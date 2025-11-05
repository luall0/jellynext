using System;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models.Trakt;

/// <summary>
/// Represents season progress information for a watched show.
/// </summary>
public class TraktShowSeasonProgress
{
    /// <summary>
    /// Gets or sets the season number (0 for specials).
    /// </summary>
    [JsonPropertyName("number")]
    public int Number { get; set; }

    /// <summary>
    /// Gets or sets the episode progress information.
    /// </summary>
    [JsonPropertyName("episodes")]
    public TraktEpisodeProgress[] Episodes { get; set; } = Array.Empty<TraktEpisodeProgress>();
}
