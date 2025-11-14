using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyNext.Models.Common;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyNext.Services.DownloadProviders;

/// <summary>
/// Webhook download provider for custom HTTP webhook integrations.
/// </summary>
public class WebhookDownloadProvider : IDownloadProvider
{
    private readonly ILogger<WebhookDownloadProvider> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookDownloadProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    public WebhookDownloadProvider(
        ILogger<WebhookDownloadProvider> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc/>
    public async Task<DownloadResult> RequestMovieAsync(ContentItem contentItem, string playerId)
    {
        var config = Plugin.Instance?.Configuration;
        if (config == null)
        {
            _logger.LogError("Plugin configuration not available");
            return new DownloadResult
            {
                Success = false,
                Message = "Configuration error: Plugin configuration not available"
            };
        }

        if (string.IsNullOrWhiteSpace(config.WebhookMovieUrl))
        {
            _logger.LogWarning("Webhook movie URL not configured");
            return new DownloadResult
            {
                Success = false,
                Message = "Webhook movie URL is not configured. Please configure webhooks in plugin settings."
            };
        }

        // Build placeholder dictionary for movies
        var placeholders = new Dictionary<string, string>
        {
            { "tmdbId", contentItem.TmdbId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty },
            { "imdbId", contentItem.ImdbId ?? string.Empty },
            { "title", contentItem.Title ?? string.Empty },
            { "year", contentItem.Year?.ToString(CultureInfo.InvariantCulture) ?? string.Empty },
            { "jellyfinUserId", playerId ?? string.Empty }
        };

        // Replace placeholders in URL
        var url = ReplacePlaceholders(config.WebhookMovieUrl, placeholders);

        // Replace placeholders in headers
        var headers = config.WebhookMovieHeaders?
            .Select(h => new KeyValuePair<string, string>(
                ReplacePlaceholders(h.Name, placeholders),
                ReplacePlaceholders(h.Value, placeholders)))
            .Where(h => !string.IsNullOrWhiteSpace(h.Key))
            .ToList() ?? new List<KeyValuePair<string, string>>();

        // Replace placeholders in payload
        string? payload = null;
        if (!string.IsNullOrWhiteSpace(config.WebhookMoviePayload))
        {
            payload = ReplacePlaceholders(config.WebhookMoviePayload, placeholders);
        }

        // Send webhook request
        var method = config.WebhookMethod?.ToUpperInvariant() ?? "POST";
        var result = await SendWebhookAsync(url, method, headers, payload, contentItem.Title ?? "Unknown", "movie");

        if (result.Success)
        {
            _logger.LogInformation(
                "Successfully triggered webhook for movie: {Title} (TMDB: {TmdbId})",
                contentItem.Title,
                contentItem.TmdbId);
        }
        else
        {
            _logger.LogError(
                "Failed to trigger webhook for movie: {Title} (TMDB: {TmdbId}) - {Message}",
                contentItem.Title,
                contentItem.TmdbId,
                result.Message);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<DownloadResult> RequestShowAsync(ContentItem contentItem, int seasonNumber, string playerId, bool isAnime)
    {
        var config = Plugin.Instance?.Configuration;
        if (config == null)
        {
            _logger.LogError("Plugin configuration not available");
            return new DownloadResult
            {
                Success = false,
                Message = "Configuration error: Plugin configuration not available"
            };
        }

        if (string.IsNullOrWhiteSpace(config.WebhookShowUrl))
        {
            _logger.LogWarning("Webhook show URL not configured");
            return new DownloadResult
            {
                Success = false,
                Message = "Webhook show URL is not configured. Please configure webhooks in plugin settings."
            };
        }

        // Build placeholder dictionary for TV shows
        var placeholders = new Dictionary<string, string>
        {
            { "tvdbId", contentItem.TvdbId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty },
            { "tmdbId", contentItem.TmdbId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty },
            { "imdbId", contentItem.ImdbId ?? string.Empty },
            { "title", contentItem.Title ?? string.Empty },
            { "year", contentItem.Year?.ToString(CultureInfo.InvariantCulture) ?? string.Empty },
            { "seasonNumber", seasonNumber.ToString(CultureInfo.InvariantCulture) },
            { "isAnime", isAnime.ToString(CultureInfo.InvariantCulture).ToLowerInvariant() },
            { "jellyfinUserId", playerId ?? string.Empty }
        };

        // Replace placeholders in URL
        var url = ReplacePlaceholders(config.WebhookShowUrl, placeholders);

        // Replace placeholders in headers
        var headers = config.WebhookShowHeaders?
            .Select(h => new KeyValuePair<string, string>(
                ReplacePlaceholders(h.Name, placeholders),
                ReplacePlaceholders(h.Value, placeholders)))
            .Where(h => !string.IsNullOrWhiteSpace(h.Key))
            .ToList() ?? new List<KeyValuePair<string, string>>();

        // Replace placeholders in payload
        string? payload = null;
        if (!string.IsNullOrWhiteSpace(config.WebhookShowPayload))
        {
            payload = ReplacePlaceholders(config.WebhookShowPayload, placeholders);
        }

        // Send webhook request
        var method = config.WebhookMethod?.ToUpperInvariant() ?? "POST";
        var result = await SendWebhookAsync(url, method, headers, payload, contentItem.Title ?? "Unknown", "show", seasonNumber);

        if (result.Success)
        {
            _logger.LogInformation(
                "Successfully triggered webhook for show: {Title} - Season {Season} (TVDB: {TvdbId})",
                contentItem.Title,
                seasonNumber,
                contentItem.TvdbId);
        }
        else
        {
            _logger.LogError(
                "Failed to trigger webhook for show: {Title} - Season {Season} (TVDB: {TvdbId}) - {Message}",
                contentItem.Title,
                seasonNumber,
                contentItem.TvdbId,
                result.Message);
        }

        return result;
    }

    /// <summary>
    /// Replaces placeholders in a template string.
    /// </summary>
    /// <param name="template">The template string containing placeholders like {tmdbId}.</param>
    /// <param name="placeholders">Dictionary of placeholder values.</param>
    /// <returns>The template with placeholders replaced.</returns>
    private static string ReplacePlaceholders(string template, Dictionary<string, string> placeholders)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return template;
        }

        var result = template;
        foreach (var placeholder in placeholders)
        {
            // Replace {key} with value, case-insensitive
            var pattern = $"{{{Regex.Escape(placeholder.Key)}}}";
            result = Regex.Replace(result, pattern, placeholder.Value, RegexOptions.IgnoreCase);
        }

        return result;
    }

    /// <summary>
    /// Sends a webhook HTTP request.
    /// </summary>
    /// <param name="url">The webhook URL.</param>
    /// <param name="method">The HTTP method (GET, POST, PUT, PATCH).</param>
    /// <param name="headers">Custom headers to include.</param>
    /// <param name="payload">Optional request body payload.</param>
    /// <param name="title">Content title for user-facing messages.</param>
    /// <param name="contentType">Content type (movie or show) for messages.</param>
    /// <param name="seasonNumber">Optional season number for shows.</param>
    /// <returns>A download result indicating success/failure.</returns>
    private async Task<DownloadResult> SendWebhookAsync(
        string url,
        string method,
        List<KeyValuePair<string, string>> headers,
        string? payload,
        string title,
        string contentType,
        int? seasonNumber = null)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
            var request = new HttpRequestMessage(new HttpMethod(method), url);

            // Add custom headers
            foreach (var header in headers)
            {
                try
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to add header {HeaderName}: {HeaderValue}", header.Key, header.Value);
                }
            }

            // Add payload for POST/PUT/PATCH
            if (!string.IsNullOrWhiteSpace(payload) &&
                (method == "POST" || method == "PUT" || method == "PATCH"))
            {
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
            }

            _logger.LogDebug("Sending webhook {Method} request to {Url}", method, url);

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var seasonInfo = seasonNumber.HasValue ? $" - Season {seasonNumber.Value}" : string.Empty;
                return new DownloadResult
                {
                    Success = true,
                    Message = $"{title}{seasonInfo} webhook triggered successfully (HTTP {(int)response.StatusCode})"
                };
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                _logger.LogError(
                    "Webhook request failed with status {StatusCode}: {ErrorBody}",
                    response.StatusCode,
                    errorBody);

                return new DownloadResult
                {
                    Success = false,
                    Message = $"Webhook request failed: HTTP {(int)response.StatusCode} - {response.ReasonPhrase}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending webhook to {Url}", url);
            return new DownloadResult
            {
                Success = false,
                Message = $"Webhook request exception: {ex.Message}"
            };
        }
    }
}
