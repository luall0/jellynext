using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyNext.Models.Common;
using Jellyfin.Plugin.JellyNext.Models.Radarr;
using Jellyfin.Plugin.JellyNext.Models.Sonarr;
using Jellyfin.Plugin.JellyNext.Models.Trakt;
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
    /// Adds a series to Sonarr with specific season monitoring.
    /// </summary>
    /// <param name="tvdbId">The TVDB ID of the series.</param>
    /// <param name="title">The series title.</param>
    /// <param name="year">The series year.</param>
    /// <param name="seasonNumber">The season number to monitor (downloads only this season).</param>
    /// <param name="isAnime">Whether the series is anime (uses anime root folder).</param>
    /// <returns>The added series if successful, null otherwise.</returns>
    public async Task<SonarrSeries?> AddSeriesAsync(int tvdbId, string title, int? year, int seasonNumber, bool isAnime = false)
    {
        try
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null)
            {
                _logger.LogError("Plugin configuration not available");
                return null;
            }

            if (string.IsNullOrWhiteSpace(config.SonarrUrl) || string.IsNullOrWhiteSpace(config.SonarrApiKey))
            {
                _logger.LogError("Sonarr URL or API key not configured");
                return null;
            }

            using var httpClient = CreateSonarrClient(config.SonarrUrl, config.SonarrApiKey);

            // Check if series already exists
            var existingSeries = await httpClient.GetFromJsonAsync<List<SonarrSeries>>($"/api/v3/series?tvdbId={tvdbId}");
            if (existingSeries != null && existingSeries.Count > 0)
            {
                var existing = existingSeries[0];
                _logger.LogInformation("Series {Title} already exists in Sonarr with ID {SeriesId}", title, existing.Id);

                // Check if the requested season is already monitored
                var existingSeason = existing.Seasons?.Find(s => s.SeasonNumber == seasonNumber);
                if (existingSeason != null && existingSeason.Monitored)
                {
                    _logger.LogInformation("Season {SeasonNumber} of {Title} is already monitored", seasonNumber, title);
                    return existing;
                }

                // Update the series to monitor the requested season
                existingSeason = existing.Seasons?.Find(s => s.SeasonNumber == seasonNumber);
                if (existingSeason != null)
                {
                    existingSeason.Monitored = true;
                    var updateResponse = await httpClient.PutAsJsonAsync($"/api/v3/series/{existing.Id}", existing);
                    if (updateResponse.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Updated {Title} to monitor season {SeasonNumber}", title, seasonNumber);

                        // Trigger search for the newly monitored season
                        var commandBody = new
                        {
                            name = "SeriesSearch",
                            seriesId = existing.Id
                        };
                        await httpClient.PostAsJsonAsync("/api/v3/command", commandBody);

                        return existing;
                    }
                    else
                    {
                        _logger.LogError("Failed to update series monitoring for {Title}", title);
                    }
                }

                return existing;
            }

            // Determine root folder based on anime flag
            var rootFolderPath = isAnime && !string.IsNullOrWhiteSpace(config.SonarrAnimeRootFolderPath)
                ? config.SonarrAnimeRootFolderPath
                : config.SonarrRootFolderPath;

            if (string.IsNullOrWhiteSpace(rootFolderPath))
            {
                _logger.LogError("Sonarr root folder path not configured");
                return null;
            }

            // Create season monitoring list (only monitor the requested season)
            var seasons = new List<SonarrSeason>();
            for (int i = 0; i <= 20; i++) // Sonarr needs all seasons defined
            {
                seasons.Add(new SonarrSeason
                {
                    SeasonNumber = i,
                    Monitored = i == seasonNumber // Only monitor the requested season
                });
            }

            // Create series object
            var newSeries = new SonarrSeries
            {
                TvdbId = tvdbId,
                Title = title,
                QualityProfileId = config.SonarrQualityProfileId,
                RootFolderPath = rootFolderPath,
                Monitored = true, // Series monitored, but only specific season is monitored at season level
                SeasonFolder = true,
                SeriesType = isAnime ? "anime" : "standard",
                Seasons = seasons,
                AddOptions = new SonarrAddOptions
                {
                    SearchForMissingEpisodes = true // Trigger immediate search
                }
            };

            // Add series to Sonarr
            var response = await httpClient.PostAsJsonAsync("/api/v3/series", newSeries);
            if (response.IsSuccessStatusCode)
            {
                var addedSeries = await response.Content.ReadFromJsonAsync<SonarrSeries>();
                _logger.LogInformation(
                    "Successfully added {Title} ({Year}) to Sonarr - Season {SeasonNumber} monitored - Type: {SeriesType}",
                    title,
                    year,
                    seasonNumber,
                    isAnime ? "Anime" : "Standard");
                return addedSeries;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to add series {Title} to Sonarr: {StatusCode} - {Error}", title, response.StatusCode, errorContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding series {Title} to Sonarr", title);
            return null;
        }
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
