using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyNext.Models;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyNext.Services;

/// <summary>
/// Service for interacting with the Trakt API.
/// </summary>
public class TraktApi
{
    private const string TraktApiBaseUrl = "https://api.trakt.tv";
    private const string DeviceCodeEndpoint = "/oauth/device/code";
    private const string DeviceTokenEndpoint = "/oauth/device/token";
    private const string RefreshTokenEndpoint = "/oauth/token";

    private const string TraktClientId = "2c2621eef7a2c82a221f7a03c65bfa0088555699ebeb4cefe1ee2490c8245864";
    private const string TraktClientSecret = "d2fa3baeafd861a7e024cbb53b0b9bf2451db8f56e8ca8ee650324963c2c967c";

    private readonly ILogger<TraktApi> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="TraktApi"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    public TraktApi(ILogger<TraktApi> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }

    /// <summary>
    /// Initiates the OAuth device authorization flow.
    /// </summary>
    /// <param name="traktUser">The Trakt user configuration.</param>
    /// <returns>The user code to display to the user.</returns>
    public async Task<string> AuthorizeDevice(TraktUser traktUser)
    {
        var request = new { client_id = TraktClientId };

        using var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
        httpClient.BaseAddress = new Uri(TraktApiBaseUrl);
        httpClient.DefaultRequestHeaders.Add("trakt-api-version", "2");
        httpClient.DefaultRequestHeaders.Add("trakt-api-key", TraktClientId);

        var response = await httpClient.PostAsJsonAsync(DeviceCodeEndpoint, request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Trakt API error: Status={Status}, Content={Content}", response.StatusCode, errorContent);
        }

        response.EnsureSuccessStatusCode();

        var deviceCode = await response.Content.ReadFromJsonAsync<TraktDeviceCode>(_jsonOptions);
        if (deviceCode == null)
        {
            throw new InvalidOperationException("Failed to obtain device code from Trakt");
        }

        // Start background polling task and track it
        var pollingTask = Task.Run(() => PollForAccessToken(deviceCode, traktUser));
        Plugin.Instance?.PollingTasks.TryAdd(traktUser.LinkedMbUserId, pollingTask);

        return deviceCode.UserCode;
    }

    /// <summary>
    /// Polls Trakt for access token completion.
    /// </summary>
    /// <param name="deviceCode">The device code from the initial authorization.</param>
    /// <param name="traktUser">The Trakt user configuration.</param>
    /// <returns>True if authorization succeeded, false otherwise.</returns>
    public async Task<bool> PollForAccessToken(TraktDeviceCode deviceCode, TraktUser traktUser)
    {
        var request = new
        {
            code = deviceCode.DeviceCode,
            client_id = TraktClientId,
            client_secret = TraktClientSecret
        };

        var pollingInterval = deviceCode.Interval;
        var expiresAt = DateTime.UtcNow.AddSeconds(deviceCode.ExpiresIn);

        using var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
        httpClient.BaseAddress = new Uri(TraktApiBaseUrl);
        httpClient.DefaultRequestHeaders.Add("trakt-api-version", "2");
        httpClient.DefaultRequestHeaders.Add("trakt-api-key", TraktClientId);

        while (DateTime.UtcNow < expiresAt)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync(DeviceTokenEndpoint, request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var accessToken = await response.Content.ReadFromJsonAsync<TraktUserAccessToken>(_jsonOptions);
                    if (accessToken != null)
                    {
                        traktUser.AccessToken = accessToken.AccessToken;
                        traktUser.RefreshToken = accessToken.RefreshToken;
                        traktUser.AccessTokenExpiration = DateTime.Now.AddSeconds(accessToken.ExpirationWithBuffer);

                        Plugin.Instance?.SaveConfiguration();
                        _logger.LogInformation("Successfully authorized Trakt user {UserId}", traktUser.LinkedMbUserId);
                        return true;
                    }
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Still pending user authorization
                    _logger.LogDebug("Waiting for user to authorize device...");
                }
                else if (response.StatusCode == (HttpStatusCode)418)
                {
                    // User denied authorization
                    _logger.LogWarning("User denied Trakt authorization");
                    return false;
                }
                else if (response.StatusCode == HttpStatusCode.Gone)
                {
                    // Device code expired
                    _logger.LogWarning("Trakt device code expired");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling for Trakt access token");
            }

            await Task.Delay(pollingInterval * 1000);
        }

        _logger.LogWarning("Trakt authorization timed out");
        return false;
    }

    /// <summary>
    /// Refreshes the user's access token using the refresh token.
    /// </summary>
    /// <param name="traktUser">The Trakt user configuration.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RefreshUserAccessToken(TraktUser traktUser)
    {
        if (string.IsNullOrWhiteSpace(traktUser.RefreshToken))
        {
            _logger.LogError("Attempted to refresh Trakt token but no refresh token was available");
            return;
        }

        var request = new TraktUserRefreshTokenRequest
        {
            RefreshToken = traktUser.RefreshToken,
            ClientId = TraktClientId,
            ClientSecret = TraktClientSecret
        };

        try
        {
            using var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
            httpClient.BaseAddress = new Uri(TraktApiBaseUrl);
            httpClient.DefaultRequestHeaders.Add("trakt-api-version", "2");
            httpClient.DefaultRequestHeaders.Add("trakt-api-key", TraktClientId);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "JellyNext/1.0");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var response = await httpClient.PostAsJsonAsync(RefreshTokenEndpoint, request);
            response.EnsureSuccessStatusCode();

            var accessToken = await response.Content.ReadFromJsonAsync<TraktUserAccessToken>(_jsonOptions);
            if (accessToken != null)
            {
                traktUser.AccessToken = accessToken.AccessToken;
                traktUser.RefreshToken = accessToken.RefreshToken;
                traktUser.AccessTokenExpiration = DateTime.Now.AddSeconds(accessToken.ExpirationWithBuffer);

                Plugin.Instance?.SaveConfiguration();
                _logger.LogInformation("Successfully refreshed Trakt access token for user {UserId}", traktUser.LinkedMbUserId);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to refresh Trakt access token");
        }
    }

    /// <summary>
    /// Ensures the user's access token is valid, refreshing if necessary.
    /// </summary>
    /// <param name="traktUser">The Trakt user configuration.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task EnsureValidAccessToken(TraktUser traktUser)
    {
        if (DateTime.Now >= traktUser.AccessTokenExpiration)
        {
            traktUser.AccessToken = string.Empty;
            await RefreshUserAccessToken(traktUser);
        }
    }

    /// <summary>
    /// Creates an HTTP client with proper Trakt API headers.
    /// </summary>
    /// <param name="traktUser">The Trakt user configuration (optional, for authenticated requests).</param>
    /// <returns>Configured HTTP client.</returns>
    public async Task<HttpClient> CreateTraktClient(TraktUser? traktUser = null)
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(TraktApiBaseUrl);
        httpClient.DefaultRequestHeaders.Add("trakt-api-version", "2");
        httpClient.DefaultRequestHeaders.Add("trakt-api-key", TraktClientId);
        httpClient.DefaultRequestHeaders.Add("User-Agent", "JellyNext/1.0");
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        if (traktUser != null)
        {
            await EnsureValidAccessToken(traktUser);

            if (!string.IsNullOrEmpty(traktUser.AccessToken))
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", traktUser.AccessToken);
            }
        }

        return httpClient;
    }

    /// <summary>
    /// Gets personalized movie recommendations for a user.
    /// </summary>
    /// <param name="traktUser">The Trakt user configuration.</param>
    /// <param name="ignoreCollected">Whether to ignore collected movies.</param>
    /// <param name="ignoreWatchlisted">Whether to ignore watchlisted movies.</param>
    /// <param name="limit">Maximum number of recommendations to return (default: 10, max: 100).</param>
    /// <returns>List of recommended movies.</returns>
    public async Task<TraktMovie[]> GetMovieRecommendations(
        TraktUser traktUser,
        bool ignoreCollected = true,
        bool ignoreWatchlisted = false,
        int limit = 10)
    {
        var queryParams = $"?limit={limit}";
        if (ignoreCollected)
        {
            queryParams += "&ignore_collected=true";
        }

        if (ignoreWatchlisted)
        {
            queryParams += "&ignore_watchlisted=true";
        }

        using var httpClient = await CreateTraktClient(traktUser);
        var response = await httpClient.GetAsync($"/recommendations/movies{queryParams}");

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError(
                "Failed to get movie recommendations: Status={Status}, Content={Content}",
                response.StatusCode,
                errorContent);
            return Array.Empty<TraktMovie>();
        }

        var movies = await response.Content.ReadFromJsonAsync<TraktMovie[]>(_jsonOptions);
        return movies ?? Array.Empty<TraktMovie>();
    }

    /// <summary>
    /// Gets personalized show recommendations for a user.
    /// </summary>
    /// <param name="traktUser">The Trakt user configuration.</param>
    /// <param name="ignoreCollected">Whether to ignore collected shows.</param>
    /// <param name="ignoreWatchlisted">Whether to ignore watchlisted shows.</param>
    /// <param name="limit">Maximum number of recommendations to return (default: 10, max: 100).</param>
    /// <returns>List of recommended shows.</returns>
    public async Task<TraktShow[]> GetShowRecommendations(
        TraktUser traktUser,
        bool ignoreCollected = true,
        bool ignoreWatchlisted = false,
        int limit = 10)
    {
        var queryParams = $"?limit={limit}";
        if (ignoreCollected)
        {
            queryParams += "&ignore_collected=true";
        }

        if (ignoreWatchlisted)
        {
            queryParams += "&ignore_watchlisted=true";
        }

        using var httpClient = await CreateTraktClient(traktUser);
        var response = await httpClient.GetAsync($"/recommendations/shows{queryParams}");

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError(
                "Failed to get show recommendations: Status={Status}, Content={Content}",
                response.StatusCode,
                errorContent);
            return Array.Empty<TraktShow>();
        }

        var shows = await response.Content.ReadFromJsonAsync<TraktShow[]>(_jsonOptions);
        return shows ?? Array.Empty<TraktShow>();
    }

    /// <summary>
    /// Gets the user's watched shows with season/episode progress.
    /// </summary>
    /// <param name="traktUser">The Trakt user configuration.</param>
    /// <returns>List of watched shows with progress information.</returns>
    public async Task<TraktWatchedShow[]> GetWatchedShows(TraktUser traktUser)
    {
        using var httpClient = await CreateTraktClient(traktUser);
        var response = await httpClient.GetAsync("/sync/watched/shows");

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError(
                "Failed to get watched shows: Status={Status}, Content={Content}",
                response.StatusCode,
                errorContent);
            return Array.Empty<TraktWatchedShow>();
        }

        var watchedShows = await response.Content.ReadFromJsonAsync<TraktWatchedShow[]>(_jsonOptions);
        return watchedShows ?? Array.Empty<TraktWatchedShow>();
    }

    /// <summary>
    /// Gets all seasons for a show by Trakt ID.
    /// </summary>
    /// <param name="traktUser">The Trakt user configuration.</param>
    /// <param name="traktId">The Trakt show ID.</param>
    /// <returns>List of seasons for the show.</returns>
    public async Task<TraktSeason[]> GetShowSeasons(TraktUser traktUser, int traktId)
    {
        using var httpClient = await CreateTraktClient(traktUser);
        var response = await httpClient.GetAsync($"/shows/{traktId}/seasons?extended=full");

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError(
                "Failed to get show seasons for Trakt ID {TraktId}: Status={Status}, Content={Content}",
                traktId,
                response.StatusCode,
                errorContent);
            return Array.Empty<TraktSeason>();
        }

        var responseContent = await response.Content.ReadAsStringAsync();

        TraktSeason[]? seasons = null;
        try
        {
            seasons = System.Text.Json.JsonSerializer.Deserialize<TraktSeason[]>(responseContent, _jsonOptions);
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Trakt seasons for show {TraktId}", traktId);
            return Array.Empty<TraktSeason>();
        }

        if (seasons != null)
        {
            _logger.LogInformation(
                "Trakt ID {TraktId} - Retrieved {Count} seasons from API",
                traktId,
                seasons.Length);

            foreach (var season in seasons)
            {
                _logger.LogInformation(
                    "  Season {Number}: {EpisodeCount} episodes, {AiredCount} aired (first aired: {FirstAired})",
                    season.Number,
                    season.EpisodeCount,
                    season.AiredEpisodes,
                    season.FirstAired?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? "unknown");
            }
        }
        else
        {
            _logger.LogWarning("Trakt ID {TraktId} - Seasons deserialized to null", traktId);
        }

        return seasons ?? Array.Empty<TraktSeason>();
    }
}
