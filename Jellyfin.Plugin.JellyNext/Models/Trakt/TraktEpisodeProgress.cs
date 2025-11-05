using System;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models.Trakt;

/// <summary>
/// Represents episode progress information.
/// </summary>
public class TraktEpisodeProgress
{
    /// <summary>
    /// Gets or sets the episode number.
    /// </summary>
    [JsonPropertyName("number")]
    public int Number { get; set; }

    /// <summary>
    /// Gets or sets when the episode was last watched.
    /// </summary>
    [JsonPropertyName("last_watched_at")]
    public DateTime? LastWatchedAt { get; set; }
}
