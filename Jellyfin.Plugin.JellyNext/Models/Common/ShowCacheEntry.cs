using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.JellyNext.Models.Common;

/// <summary>
/// Cache entry for a TV show with season-level metadata.
/// </summary>
public class ShowCacheEntry
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
    /// Gets or sets the show status (e.g., "ended", "canceled", "returning series").
    /// </summary>
    public string Status { get; set; } = "unknown";

    /// <summary>
    /// Gets or sets the genres.
    /// </summary>
    public string[] Genres { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the cached season metadata (season number → metadata).
    /// </summary>
    public Dictionary<int, SeasonMetadata> Seasons { get; set; } = new Dictionary<int, SeasonMetadata>();

    /// <summary>
    /// Gets or sets when this show was first cached.
    /// </summary>
    public DateTime CachedAt { get; set; }

    /// <summary>
    /// Gets or sets the highest season number the user has watched at least one episode of.
    /// Updated during sync based on watch history.
    /// </summary>
    public int? HighestWatchedSeason { get; set; }

    /// <summary>
    /// Gets a value indicating whether the show is ended or canceled.
    /// </summary>
    public bool IsEnded => Status.Equals("ended", StringComparison.OrdinalIgnoreCase) ||
                           Status.Equals("canceled", StringComparison.OrdinalIgnoreCase);
}
