using System.Threading.Tasks;
using Jellyfin.Plugin.JellyNext.Models.Common;

namespace Jellyfin.Plugin.JellyNext.Services.DownloadProviders;

/// <summary>
/// Interface for download providers (Native Radarr/Sonarr or Jellyseerr integration).
/// </summary>
public interface IDownloadProvider
{
    /// <summary>
    /// Requests download of a movie.
    /// </summary>
    /// <param name="contentItem">The movie content item.</param>
    /// <param name="playerId">The player's user ID (for Jellyseerr requests).</param>
    /// <returns>A download result indicating success/failure.</returns>
    Task<DownloadResult> RequestMovieAsync(ContentItem contentItem, string playerId);

    /// <summary>
    /// Requests download of a TV show season.
    /// </summary>
    /// <param name="contentItem">The show content item.</param>
    /// <param name="seasonNumber">The season number to download.</param>
    /// <param name="playerId">The player's user ID (for Jellyseerr requests).</param>
    /// <param name="isAnime">Whether the show is anime (for separate folder routing).</param>
    /// <returns>A download result indicating success/failure.</returns>
    Task<DownloadResult> RequestShowAsync(ContentItem contentItem, int seasonNumber, string playerId, bool isAnime);
}
