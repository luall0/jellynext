using System;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyNext.Models.Common;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyNext.Services.DownloadProviders;

/// <summary>
/// Native download provider using Radarr/Sonarr directly.
/// </summary>
public class NativeDownloadProvider : IDownloadProvider
{
    private readonly ILogger<NativeDownloadProvider> _logger;
    private readonly RadarrService _radarrService;
    private readonly SonarrService _sonarrService;

    /// <summary>
    /// Initializes a new instance of the <see cref="NativeDownloadProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="radarrService">The Radarr service.</param>
    /// <param name="sonarrService">The Sonarr service.</param>
    public NativeDownloadProvider(
        ILogger<NativeDownloadProvider> logger,
        RadarrService radarrService,
        SonarrService sonarrService)
    {
        _logger = logger;
        _radarrService = radarrService;
        _sonarrService = sonarrService;
    }

    /// <inheritdoc/>
    public async Task<DownloadResult> RequestMovieAsync(ContentItem contentItem, string playerId)
    {
        if (contentItem.TmdbId == null)
        {
            _logger.LogWarning("Cannot add movie to Radarr: TMDB ID not available for {Title}", contentItem.Title);
            return new DownloadResult
            {
                Success = false,
                Message = $"Failed to add {contentItem.Title} ({contentItem.Year}) to download queue. TMDB ID not available."
            };
        }

        var radarrResult = await _radarrService.AddMovieAsync(
            contentItem.TmdbId.Value,
            contentItem.Title,
            contentItem.Year ?? DateTime.UtcNow.Year);

        if (radarrResult != null)
        {
            _logger.LogInformation(
                "Successfully added movie to Radarr: {Title} (TMDB: {TmdbId})",
                contentItem.Title,
                contentItem.TmdbId);

            return new DownloadResult
            {
                Success = true,
                Message = $"{contentItem.Title} ({contentItem.Year}) has been added to your download queue and will appear in your library shortly."
            };
        }
        else
        {
            _logger.LogError(
                "Failed to add movie to Radarr: {Title} (TMDB: {TmdbId})",
                contentItem.Title,
                contentItem.TmdbId);

            return new DownloadResult
            {
                Success = false,
                Message = $"Failed to add {contentItem.Title} ({contentItem.Year}) to download queue. Please check your Radarr configuration."
            };
        }
    }

    /// <inheritdoc/>
    public async Task<DownloadResult> RequestShowAsync(ContentItem contentItem, int seasonNumber, string playerId, bool isAnime)
    {
        if (contentItem.TvdbId == null)
        {
            _logger.LogWarning("Cannot add show to Sonarr: TVDB ID not available for {Title}", contentItem.Title);
            return new DownloadResult
            {
                Success = false,
                Message = $"Failed to add {contentItem.Title} - Season {seasonNumber} to download queue. TVDB ID not available."
            };
        }

        var sonarrResult = await _sonarrService.AddSeriesAsync(
            contentItem.TvdbId.Value,
            contentItem.Title,
            contentItem.Year,
            seasonNumber,
            isAnime);

        if (sonarrResult != null)
        {
            _logger.LogInformation(
                "Successfully added show to Sonarr: {Title} - Season {Season} (TVDB: {TvdbId})",
                contentItem.Title,
                seasonNumber,
                contentItem.TvdbId);

            return new DownloadResult
            {
                Success = true,
                Message = $"{contentItem.Title} ({contentItem.Year}) - Season {seasonNumber} has been added to your download queue and will appear in your library shortly."
            };
        }
        else
        {
            _logger.LogError(
                "Failed to add show to Sonarr: {Title} - Season {Season} (TVDB: {TvdbId})",
                contentItem.Title,
                seasonNumber,
                contentItem.TvdbId);

            return new DownloadResult
            {
                Success = false,
                Message = $"Failed to add {contentItem.Title} ({contentItem.Year}) - Season {seasonNumber} to download queue. Please check your Sonarr configuration."
            };
        }
    }
}
