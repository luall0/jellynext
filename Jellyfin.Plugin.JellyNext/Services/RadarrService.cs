using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyNext.Models;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyNext.Services;

/// <summary>
/// Service for interacting with the Radarr API.
/// </summary>
public class RadarrService
{
    private readonly ILogger<RadarrService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="RadarrService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    public RadarrService(ILogger<RadarrService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Tests the connection to Radarr and retrieves available profiles and folders.
    /// </summary>
    /// <param name="radarrUrl">The Radarr URL.</param>
    /// <param name="apiKey">The Radarr API key.</param>
    /// <returns>A test connection response with profiles and folders if successful.</returns>
    public async Task<RadarrTestConnectionResponse> TestConnectionAsync(string radarrUrl, string apiKey)
    {
        var response = new RadarrTestConnectionResponse();

        try
        {
            if (string.IsNullOrWhiteSpace(radarrUrl) || string.IsNullOrWhiteSpace(apiKey))
            {
                response.Success = false;
                response.ErrorMessage = "Radarr URL and API Key are required";
                return response;
            }

            using var httpClient = CreateRadarrClient(radarrUrl, apiKey);

            var systemStatus = await httpClient.GetFromJsonAsync<RadarrSystemStatus>("/api/v3/system/status");
            if (systemStatus == null)
            {
                response.Success = false;
                response.ErrorMessage = "Failed to retrieve system status from Radarr";
                return response;
            }

            response.Version = systemStatus.Version;

            var qualityProfiles = await httpClient.GetFromJsonAsync<List<RadarrQualityProfile>>("/api/v3/qualityprofile");
            if (qualityProfiles != null)
            {
                response.QualityProfiles = qualityProfiles;
            }

            var rootFolders = await httpClient.GetFromJsonAsync<List<RadarrRootFolder>>("/api/v3/rootfolder");
            if (rootFolders != null)
            {
                response.RootFolders = rootFolders;
            }

            response.Success = true;
            _logger.LogInformation("Successfully connected to Radarr v{Version}", systemStatus.Version);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error testing Radarr connection");
            response.Success = false;
            response.ErrorMessage = $"Connection failed: {ex.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Radarr connection");
            response.Success = false;
            response.ErrorMessage = $"Unexpected error: {ex.Message}";
        }

        return response;
    }

    /// <summary>
    /// Creates an HTTP client configured for Radarr API.
    /// </summary>
    /// <param name="radarrUrl">The Radarr URL.</param>
    /// <param name="apiKey">The Radarr API key.</param>
    /// <returns>A configured HTTP client.</returns>
    private HttpClient CreateRadarrClient(string radarrUrl, string apiKey)
    {
        var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
        httpClient.BaseAddress = new Uri(radarrUrl.TrimEnd('/'));
        httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        return httpClient;
    }
}
