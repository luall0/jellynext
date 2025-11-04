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
/// Service for interacting with the Sonarr API.
/// </summary>
public class SonarrService
{
    private readonly ILogger<SonarrService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="SonarrService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    public SonarrService(ILogger<SonarrService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Tests the connection to Sonarr and retrieves available profiles and folders.
    /// </summary>
    /// <param name="sonarrUrl">The Sonarr URL.</param>
    /// <param name="apiKey">The Sonarr API key.</param>
    /// <returns>A test connection response with profiles and folders if successful.</returns>
    public async Task<SonarrTestConnectionResponse> TestConnectionAsync(string sonarrUrl, string apiKey)
    {
        var response = new SonarrTestConnectionResponse();

        try
        {
            if (string.IsNullOrWhiteSpace(sonarrUrl) || string.IsNullOrWhiteSpace(apiKey))
            {
                response.Success = false;
                response.ErrorMessage = "Sonarr URL and API Key are required";
                return response;
            }

            using var httpClient = CreateSonarrClient(sonarrUrl, apiKey);

            var systemStatus = await httpClient.GetFromJsonAsync<SonarrSystemStatus>("/api/v3/system/status");
            if (systemStatus == null)
            {
                response.Success = false;
                response.ErrorMessage = "Failed to retrieve system status from Sonarr";
                return response;
            }

            response.Version = systemStatus.Version;

            var qualityProfiles = await httpClient.GetFromJsonAsync<List<SonarrQualityProfile>>("/api/v3/qualityprofile");
            if (qualityProfiles != null)
            {
                response.QualityProfiles = qualityProfiles;
            }

            var rootFolders = await httpClient.GetFromJsonAsync<List<SonarrRootFolder>>("/api/v3/rootfolder");
            if (rootFolders != null)
            {
                response.RootFolders = rootFolders;
            }

            response.Success = true;
            _logger.LogInformation("Successfully connected to Sonarr v{Version}", systemStatus.Version);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error testing Sonarr connection");
            response.Success = false;
            response.ErrorMessage = $"Connection failed: {ex.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Sonarr connection");
            response.Success = false;
            response.ErrorMessage = $"Unexpected error: {ex.Message}";
        }

        return response;
    }

    /// <summary>
    /// Creates an HTTP client configured for Sonarr API.
    /// </summary>
    /// <param name="sonarrUrl">The Sonarr URL.</param>
    /// <param name="apiKey">The Sonarr API key.</param>
    /// <returns>A configured HTTP client.</returns>
    private HttpClient CreateSonarrClient(string sonarrUrl, string apiKey)
    {
        var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
        httpClient.BaseAddress = new Uri(sonarrUrl.TrimEnd('/'));
        httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        return httpClient;
    }
}
