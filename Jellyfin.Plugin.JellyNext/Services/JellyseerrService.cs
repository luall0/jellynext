using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyNext.Models.Jellyseerr;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyNext.Services;

/// <summary>
/// Service for interacting with the Jellyseerr API.
/// </summary>
public class JellyseerrService
{
    private readonly ILogger<JellyseerrService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyseerrService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    public JellyseerrService(ILogger<JellyseerrService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Tests the connection to Jellyseerr.
    /// </summary>
    /// <param name="jellyseerrUrl">The Jellyseerr URL.</param>
    /// <param name="apiKey">The Jellyseerr API key.</param>
    /// <returns>A test connection response.</returns>
    public async Task<TestConnectionResponse> TestConnectionAsync(string jellyseerrUrl, string apiKey)
    {
        var response = new TestConnectionResponse();

        try
        {
            if (string.IsNullOrWhiteSpace(jellyseerrUrl) || string.IsNullOrWhiteSpace(apiKey))
            {
                response.Success = false;
                response.ErrorMessage = "Jellyseerr URL and API Key are required";
                return response;
            }

            using var httpClient = CreateJellyseerrClient(jellyseerrUrl, apiKey);

            var status = await httpClient.GetFromJsonAsync<StatusResponse>("/api/v1/status");
            if (status == null)
            {
                response.Success = false;
                response.ErrorMessage = "Failed to retrieve status from Jellyseerr";
                return response;
            }

            response.Version = status.Version;
            response.Success = true;
            _logger.LogInformation("Successfully connected to Jellyseerr v{Version}", status.Version);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error testing Jellyseerr connection");
            response.Success = false;
            response.ErrorMessage = $"Connection failed: {ex.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Jellyseerr connection");
            response.Success = false;
            response.ErrorMessage = $"Unexpected error: {ex.Message}";
        }

        return response;
    }

    /// <summary>
    /// Gets all users from Jellyseerr.
    /// </summary>
    /// <returns>List of Jellyseerr users.</returns>
    public async Task<List<JellyseerrUser>?> GetUsersAsync()
    {
        try
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null)
            {
                _logger.LogError("Plugin configuration is null");
                return null;
            }

            using var httpClient = CreateJellyseerrClient(config.JellyseerrUrl, config.JellyseerrApiKey);

            // The API returns paginated results, fetch all pages
            var allUsers = new List<JellyseerrUser>();
            var currentPage = 1;
            var totalPages = 1;

            do
            {
                var skip = (currentPage - 1) * 100;
                var response = await httpClient.GetFromJsonAsync<PaginatedResponse<JellyseerrUser>>(
                    $"/api/v1/user?take=100&skip={skip}");

                if (response?.Results != null)
                {
                    allUsers.AddRange(response.Results);
                }

                if (response?.PageInfo != null)
                {
                    totalPages = response.PageInfo.Pages;
                }

                currentPage++;
            }
            while (currentPage <= totalPages);

            _logger.LogDebug("Retrieved {Count} users from Jellyseerr", allUsers.Count);
            return allUsers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users from Jellyseerr");
            return null;
        }
    }

    /// <summary>
    /// Gets a user by their Jellyfin user ID.
    /// </summary>
    /// <param name="jellyfinUserId">The Jellyfin user ID.</param>
    /// <returns>The Jellyseerr user if found, null otherwise.</returns>
    public async Task<JellyseerrUser?> GetUserByJellyfinIdAsync(string jellyfinUserId)
    {
        try
        {
            var users = await GetUsersAsync();
            if (users == null)
            {
                return null;
            }

            // Normalize both IDs by removing hyphens for comparison
            // Jellyseerr stores UUIDs without hyphens (e.g., "73f7d5b94d03434bb16d52370923eda5")
            // Jellyfin uses standard GUID format with hyphens (e.g., "73f7d5b9-4d03-434b-b16d-52370923eda5")
            var normalizedSearchId = jellyfinUserId.Replace("-", string.Empty, StringComparison.Ordinal);

            return users.FirstOrDefault(u =>
                u.JellyfinUserId != null &&
                u.JellyfinUserId.Replace("-", string.Empty, StringComparison.Ordinal)
                    .Equals(normalizedSearchId, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding user by Jellyfin ID {JellyfinUserId}", jellyfinUserId);
            return null;
        }
    }

    /// <summary>
    /// Imports a user from Jellyfin with minimal permissions (REQUEST only).
    /// </summary>
    /// <param name="jellyfinUserId">The Jellyfin user ID to import.</param>
    /// <returns>The imported Jellyseerr user if successful, null otherwise.</returns>
    public async Task<JellyseerrUser?> ImportUserFromJellyfinAsync(string jellyfinUserId)
    {
        try
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null)
            {
                _logger.LogError("Plugin configuration is null");
                return null;
            }

            using var httpClient = CreateJellyseerrClient(config.JellyseerrUrl, config.JellyseerrApiKey);

            var normalizedUserId = jellyfinUserId.Replace("-", string.Empty, StringComparison.Ordinal);

            var importRequest = new ImportUserRequest
            {
                JellyfinUserIds = new[] { normalizedUserId }
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(importRequest),
                Encoding.UTF8,
                "application/json");

            var httpResponse = await httpClient.PostAsync("/api/v1/user/import-from-jellyfin", jsonContent);
            httpResponse.EnsureSuccessStatusCode();

            var importedUsers = await httpResponse.Content.ReadFromJsonAsync<List<JellyseerrUser>>();
            var importedUser = importedUsers?.FirstOrDefault();

            if (importedUser != null)
            {
                _logger.LogInformation(
                    "Successfully imported user {Username} (Jellyfin ID: {JellyfinId}) to Jellyseerr with ID {JellyseerrId}",
                    importedUser.DisplayName ?? importedUser.Username,
                    jellyfinUserId,
                    importedUser.Id);
            }

            return importedUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing user {JellyfinUserId} from Jellyfin", jellyfinUserId);
            return null;
        }
    }

    /// <summary>
    /// Ensures a user exists in Jellyseerr, importing them if necessary.
    /// </summary>
    /// <param name="jellyfinUserId">The Jellyfin user ID.</param>
    /// <returns>The Jellyseerr user ID if successful, null otherwise.</returns>
    public async Task<int?> EnsureUserExistsAsync(string jellyfinUserId)
    {
        try
        {
            // Check if user already exists
            var existingUser = await GetUserByJellyfinIdAsync(jellyfinUserId);
            if (existingUser != null)
            {
                _logger.LogDebug("User already exists in Jellyseerr: {Username} (ID: {Id})", existingUser.DisplayName ?? existingUser.Username, existingUser.Id);
                return existingUser.Id;
            }

            // Import user with minimal permissions
            _logger.LogInformation("User not found in Jellyseerr, importing user with Jellyfin ID {JellyfinId}", jellyfinUserId);
            var importedUser = await ImportUserFromJellyfinAsync(jellyfinUserId);

            return importedUser?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring user exists in Jellyseerr");
            return null;
        }
    }

    /// <summary>
    /// Requests a movie via Jellyseerr.
    /// </summary>
    /// <param name="tmdbId">The TMDB ID of the movie.</param>
    /// <param name="jellyfinUserId">The Jellyfin user ID making the request.</param>
    /// <param name="is4k">Whether to request 4K quality.</param>
    /// <returns>The media request response.</returns>
    public async Task<MediaRequestResponse?> RequestMovieAsync(int tmdbId, string jellyfinUserId, bool is4k = false)
    {
        try
        {
            // Ensure user exists in Jellyseerr
            var jellyseerrUserId = await EnsureUserExistsAsync(jellyfinUserId);
            if (jellyseerrUserId == null)
            {
                _logger.LogError("Failed to ensure user exists in Jellyseerr for Jellyfin user {JellyfinUserId}", jellyfinUserId);
                return null;
            }

            var config = Plugin.Instance?.Configuration;
            if (config == null)
            {
                _logger.LogError("Plugin configuration is null");
                return null;
            }

            using var httpClient = CreateJellyseerrClient(config.JellyseerrUrl, config.JellyseerrApiKey);

            // Determine if we should use Jellyseerr defaults or manual configuration
            int? serverId = null;
            int? profileId = null;
            string? rootFolder = null;

            if (!config.UseJellyseerrRadarrDefaults)
            {
                // Use manual configuration
                serverId = config.JellyseerrRadarrServerId;
                profileId = config.JellyseerrRadarrProfileId;

                // Get root folder from the selected Radarr server
                if (serverId.HasValue)
                {
                    var servers = await httpClient.GetFromJsonAsync<List<RadarrServer>>("/api/v1/settings/radarr");
                    var selectedServer = servers?.FirstOrDefault(s => s.Id == serverId.Value);
                    if (selectedServer != null)
                    {
                        rootFolder = selectedServer.ActiveDirectory;
                        _logger.LogInformation(
                            "Using manual Radarr config: server {ServerId}, profile {ProfileId}, folder '{RootFolder}'",
                            serverId,
                            profileId,
                            rootFolder);
                    }
                }
            }
            else
            {
                _logger.LogInformation("Using Jellyseerr default Radarr configuration");
            }

            httpClient.DefaultRequestHeaders.Add("X-Api-User", jellyseerrUserId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));

            var request = new MediaRequest
            {
                MediaType = "movie",
                MediaId = tmdbId,
                Is4k = is4k,
                ServerId = serverId,
                ProfileId = profileId,
                RootFolder = rootFolder
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            _logger.LogInformation("JSON request: {JsonContent}", JsonSerializer.Serialize(request));
            var httpResponse = await httpClient.PostAsync("/api/v1/request", jsonContent);
            httpResponse.EnsureSuccessStatusCode();

            var result = await httpResponse.Content.ReadFromJsonAsync<MediaRequestResponse>();
            _logger.LogInformation("Successfully requested movie {TmdbId} via Jellyseerr", tmdbId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting movie {TmdbId} via Jellyseerr", tmdbId);
            return null;
        }
    }

    /// <summary>
    /// Requests a TV show via Jellyseerr.
    /// </summary>
    /// <param name="tmdbId">The TMDB ID of the TV show.</param>
    /// <param name="jellyfinUserId">The Jellyfin user ID making the request.</param>
    /// <param name="seasonNumber">The season number to request (null for all seasons).</param>
    /// <param name="is4k">Whether to request 4K quality.</param>
    /// <param name="isAnime">Whether this is an anime show (uses separate profile if configured).</param>
    /// <returns>The media request response.</returns>
    public async Task<MediaRequestResponse?> RequestTvShowAsync(int tmdbId, string jellyfinUserId, int? seasonNumber = null, bool is4k = false, bool isAnime = false)
    {
        try
        {
            // Ensure user exists in Jellyseerr
            var jellyseerrUserId = await EnsureUserExistsAsync(jellyfinUserId);
            if (jellyseerrUserId == null)
            {
                _logger.LogError("Failed to ensure user exists in Jellyseerr for Jellyfin user {JellyfinUserId}", jellyfinUserId);
                return null;
            }

            var config = Plugin.Instance?.Configuration;
            if (config == null)
            {
                _logger.LogError("Plugin configuration is null");
                return null;
            }

            using var httpClient = CreateJellyseerrClient(config.JellyseerrUrl, config.JellyseerrApiKey);

            // Determine if we should use Jellyseerr defaults or manual configuration
            int? serverId = null;
            int? profileId = null;
            string? rootFolder = null;

            if (!config.UseJellyseerrSonarrDefaults)
            {
                // Use manual configuration
                serverId = config.JellyseerrSonarrServerId;

                // Use anime profile if specified and configured, otherwise use regular profile
                profileId = isAnime && config.JellyseerrSonarrAnimeProfileId.HasValue
                    ? config.JellyseerrSonarrAnimeProfileId
                    : config.JellyseerrSonarrProfileId;

                // Get root folder from the selected Sonarr server
                if (serverId.HasValue)
                {
                    var servers = await httpClient.GetFromJsonAsync<List<SonarrServer>>("/api/v1/settings/sonarr");
                    var selectedServer = servers?.FirstOrDefault(s => s.Id == serverId.Value);
                    if (selectedServer != null)
                    {
                        // Use anime directory if it's an anime and anime directory is configured
                        rootFolder = isAnime && !string.IsNullOrEmpty(selectedServer.ActiveAnimeDirectory)
                            ? selectedServer.ActiveAnimeDirectory
                            : selectedServer.ActiveDirectory;
                        _logger.LogInformation(
                            "Using manual Sonarr config: server {ServerId}, profile {ProfileId}, folder '{RootFolder}' (Anime: {IsAnime})",
                            serverId,
                            profileId,
                            rootFolder,
                            isAnime);
                    }
                }
            }
            else
            {
                _logger.LogInformation("Using Jellyseerr default Sonarr configuration (Anime: {IsAnime})", isAnime);
            }

            // Add user id to request
            httpClient.DefaultRequestHeaders.Add("X-Api-User", jellyseerrUserId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));

            var request = new MediaRequest
            {
                MediaType = "tv",
                MediaId = tmdbId,
                Seasons = seasonNumber.HasValue ? new[] { seasonNumber.Value } : "all",
                Is4k = is4k,
                ServerId = serverId,
                ProfileId = profileId,
                RootFolder = rootFolder
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            _logger.LogInformation("Request json {Request}", JsonSerializer.Serialize(request));
            var httpResponse = await httpClient.PostAsync("/api/v1/request", jsonContent);
            httpResponse.EnsureSuccessStatusCode();

            var result = await httpResponse.Content.ReadFromJsonAsync<MediaRequestResponse>();
            _logger.LogInformation(
                "Successfully requested TV show {TmdbId} season {Season} via Jellyseerr",
                tmdbId,
                seasonNumber?.ToString(CultureInfo.InvariantCulture) ?? "all");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error requesting TV show {TmdbId} season {Season} via Jellyseerr",
                tmdbId,
                seasonNumber);
            return null;
        }
    }

    /// <summary>
    /// Gets all Radarr servers configured in Jellyseerr.
    /// </summary>
    /// <param name="jellyseerrUrl">The Jellyseerr URL.</param>
    /// <param name="apiKey">The Jellyseerr API key.</param>
    /// <returns>List of Radarr servers.</returns>
    public async Task<List<RadarrServer>?> GetRadarrServersAsync(string jellyseerrUrl, string apiKey)
    {
        try
        {
            using var httpClient = CreateJellyseerrClient(jellyseerrUrl, apiKey);
            var servers = await httpClient.GetFromJsonAsync<List<RadarrServer>>("/api/v1/settings/radarr");
            _logger.LogInformation("Retrieved {Count} Radarr servers from Jellyseerr", servers?.Count ?? 0);
            return servers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Radarr servers from Jellyseerr");
            return null;
        }
    }

    /// <summary>
    /// Gets all Sonarr servers configured in Jellyseerr.
    /// </summary>
    /// <param name="jellyseerrUrl">The Jellyseerr URL.</param>
    /// <param name="apiKey">The Jellyseerr API key.</param>
    /// <returns>List of Sonarr servers.</returns>
    public async Task<List<SonarrServer>?> GetSonarrServersAsync(string jellyseerrUrl, string apiKey)
    {
        try
        {
            using var httpClient = CreateJellyseerrClient(jellyseerrUrl, apiKey);
            var servers = await httpClient.GetFromJsonAsync<List<SonarrServer>>("/api/v1/settings/sonarr");
            _logger.LogInformation("Retrieved {Count} Sonarr servers from Jellyseerr", servers?.Count ?? 0);
            return servers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Sonarr servers from Jellyseerr");
            return null;
        }
    }

    /// <summary>
    /// Gets quality profiles for a specific Radarr server.
    /// </summary>
    /// <param name="jellyseerrUrl">The Jellyseerr URL.</param>
    /// <param name="apiKey">The Jellyseerr API key.</param>
    /// <param name="radarrServerId">The Radarr server ID.</param>
    /// <returns>List of quality profiles.</returns>
    public async Task<List<QualityProfile>?> GetRadarrProfilesAsync(string jellyseerrUrl, string apiKey, int radarrServerId)
    {
        try
        {
            using var httpClient = CreateJellyseerrClient(jellyseerrUrl, apiKey);
            var response = await httpClient.GetFromJsonAsync<RadarrServiceResponse>(
                $"/api/v1/service/radarr/{radarrServerId}");
            _logger.LogInformation(
                "Retrieved {Count} quality profiles for Radarr server {ServerId}",
                response?.Profiles?.Count ?? 0,
                radarrServerId);
            return response?.Profiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Radarr profiles for server {ServerId}", radarrServerId);
            return null;
        }
    }

    /// <summary>
    /// Gets quality profiles for a specific Sonarr server.
    /// </summary>
    /// <param name="jellyseerrUrl">The Jellyseerr URL.</param>
    /// <param name="apiKey">The Jellyseerr API key.</param>
    /// <param name="sonarrServerId">The Sonarr server ID.</param>
    /// <returns>List of quality profiles.</returns>
    public async Task<List<QualityProfile>?> GetSonarrProfilesAsync(string jellyseerrUrl, string apiKey, int sonarrServerId)
    {
        try
        {
            using var httpClient = CreateJellyseerrClient(jellyseerrUrl, apiKey);
            var response = await httpClient.GetFromJsonAsync<SonarrServiceResponse>(
                $"/api/v1/service/sonarr/{sonarrServerId}");
            _logger.LogInformation(
                "Retrieved {Count} quality profiles for Sonarr server {ServerId}",
                response?.Profiles?.Count ?? 0,
                sonarrServerId);
            return response?.Profiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Sonarr profiles for server {ServerId}", sonarrServerId);
            return null;
        }
    }

    /// <summary>
    /// Creates an HTTP client configured for Jellyseerr.
    /// </summary>
    /// <param name="baseUrl">The Jellyseerr base URL.</param>
    /// <param name="apiKey">The Jellyseerr API key.</param>
    /// <returns>Configured HTTP client.</returns>
    private HttpClient CreateJellyseerrClient(string baseUrl, string apiKey)
    {
        var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
        httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/'));
        httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        return httpClient;
    }
}
