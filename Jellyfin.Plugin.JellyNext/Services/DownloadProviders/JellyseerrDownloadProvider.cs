using System;
using System.Globalization;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyNext.Models.Common;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyNext.Services.DownloadProviders;

/// <summary>
/// Jellyseerr download provider using Jellyseerr API for requests.
/// </summary>
public class JellyseerrDownloadProvider : IDownloadProvider
{
    private readonly ILogger<JellyseerrDownloadProvider> _logger;
    private readonly JellyseerrService _jellyseerrService;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyseerrDownloadProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="jellyseerrService">The Jellyseerr service.</param>
    public JellyseerrDownloadProvider(
        ILogger<JellyseerrDownloadProvider> logger,
        JellyseerrService jellyseerrService)
    {
        _logger = logger;
        _jellyseerrService = jellyseerrService;
    }

    /// <inheritdoc/>
    public async Task<DownloadResult> RequestMovieAsync(ContentItem contentItem, string playerId)
    {
        if (contentItem.TmdbId == null)
        {
            _logger.LogWarning("Cannot request movie via Jellyseerr: TMDB ID not available for {Title}", contentItem.Title);
            return new DownloadResult
            {
                Success = false,
                Message = $"Failed to request {contentItem.Title} ({contentItem.Year}) via Jellyseerr. TMDB ID not available."
            };
        }

        var jellyseerrResult = await _jellyseerrService.RequestMovieAsync(contentItem.TmdbId.Value, playerId);

        if (jellyseerrResult != null)
        {
            _logger.LogInformation(
                "Successfully requested movie via Jellyseerr: {Title} (TMDB: {TmdbId}) - Request ID: {RequestId}",
                contentItem.Title,
                contentItem.TmdbId,
                jellyseerrResult.Id);

            return new DownloadResult
            {
                Success = true,
                Message = $"{contentItem.Title} ({contentItem.Year}) has been requested via Jellyseerr and is pending approval.",
                RequestId = jellyseerrResult.Id.ToString(CultureInfo.InvariantCulture)
            };
        }
        else
        {
            _logger.LogError(
                "Failed to request movie via Jellyseerr: {Title} (TMDB: {TmdbId})",
                contentItem.Title,
                contentItem.TmdbId);

            return new DownloadResult
            {
                Success = false,
                Message = $"Failed to request {contentItem.Title} ({contentItem.Year}) via Jellyseerr. Please check your Jellyseerr configuration."
            };
        }
    }

    /// <inheritdoc/>
    public async Task<DownloadResult> RequestShowAsync(ContentItem contentItem, int seasonNumber, string playerId, bool isAnime)
    {
        if (contentItem.TmdbId == null)
        {
            _logger.LogWarning("Cannot request TV show via Jellyseerr: TMDB ID not available for {Title}", contentItem.Title);
            return new DownloadResult
            {
                Success = false,
                Message = $"Failed to request {contentItem.Title} - Season {seasonNumber} via Jellyseerr. TMDB ID not available."
            };
        }

        var jellyseerrResult = await _jellyseerrService.RequestTvShowAsync(
            contentItem.TmdbId.Value,
            playerId,
            seasonNumber,
            isAnime: isAnime);

        if (jellyseerrResult != null)
        {
            _logger.LogInformation(
                "Successfully requested show via Jellyseerr: {Title} - Season {Season} (TMDB: {TmdbId}) - Request ID: {RequestId}",
                contentItem.Title,
                seasonNumber,
                contentItem.TmdbId,
                jellyseerrResult.Id);

            return new DownloadResult
            {
                Success = true,
                Message = $"{contentItem.Title} ({contentItem.Year}) - Season {seasonNumber} has been requested via Jellyseerr and is pending approval.",
                RequestId = jellyseerrResult.Id.ToString(CultureInfo.InvariantCulture)
            };
        }
        else
        {
            _logger.LogError(
                "Failed to request show via Jellyseerr: {Title} - Season {Season} (TMDB: {TmdbId})",
                contentItem.Title,
                seasonNumber,
                contentItem.TmdbId);

            return new DownloadResult
            {
                Success = false,
                Message = $"Failed to request {contentItem.Title} ({contentItem.Year}) - Season {seasonNumber} via Jellyseerr. Please check your Jellyseerr configuration."
            };
        }
    }
}
