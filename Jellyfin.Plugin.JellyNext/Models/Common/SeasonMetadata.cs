using System;

namespace Jellyfin.Plugin.JellyNext.Models.Common;

/// <summary>
/// Metadata for a TV show season.
/// </summary>
public class SeasonMetadata
{
    /// <summary>
    /// Gets or sets the season number.
    /// </summary>
    public int SeasonNumber { get; set; }

    /// <summary>
    /// Gets or sets the total number of episodes in this season.
    /// </summary>
    public int EpisodeCount { get; set; }

    /// <summary>
    /// Gets or sets the number of episodes that have aired.
    /// </summary>
    public int AiredEpisodes { get; set; }

    /// <summary>
    /// Gets or sets when the season first aired.
    /// </summary>
    public DateTime? FirstAired { get; set; }

    /// <summary>
    /// Gets or sets when this season metadata was cached.
    /// </summary>
    public DateTime CachedAt { get; set; }

    /// <summary>
    /// Gets a value indicating whether the season is complete (all episodes have aired).
    /// </summary>
    public bool IsComplete => EpisodeCount > 0 && EpisodeCount == AiredEpisodes;
}
