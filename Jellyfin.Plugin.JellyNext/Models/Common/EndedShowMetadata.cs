using System;

namespace Jellyfin.Plugin.JellyNext.Models.Common;

/// <summary>
/// Metadata for an ended/canceled show that never expires.
/// </summary>
public class EndedShowMetadata
{
    /// <summary>
    /// Gets or sets the show title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the show year.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets the TMDB ID.
    /// </summary>
    public int? TmdbId { get; set; }

    /// <summary>
    /// Gets or sets the IMDB ID.
    /// </summary>
    public string? ImdbId { get; set; }

    /// <summary>
    /// Gets or sets the TVDB ID.
    /// </summary>
    public int? TvdbId { get; set; }

    /// <summary>
    /// Gets or sets the Trakt ID.
    /// </summary>
    public int TraktId { get; set; }

    /// <summary>
    /// Gets or sets the show status (e.g., "ended", "canceled").
    /// </summary>
    public string Status { get; set; } = "ended";

    /// <summary>
    /// Gets or sets the genres.
    /// </summary>
    public string[] Genres { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the last season watched by any user.
    /// </summary>
    public int LastSeasonWatched { get; set; }

    /// <summary>
    /// Gets or sets when this show was cached.
    /// </summary>
    public DateTime CachedAt { get; set; }

    /// <summary>
    /// Checks if the cached metadata has expired based on the configured expiration days.
    /// </summary>
    /// <param name="expirationDays">Number of days before expiration.</param>
    /// <returns>True if expired, false otherwise.</returns>
    public bool IsExpired(int expirationDays)
    {
        return DateTime.UtcNow - CachedAt > TimeSpan.FromDays(expirationDays);
    }
}
