using System;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models.Trakt;

/// <summary>
/// Represents a watched show from Trakt with season progress.
/// </summary>
public class TraktWatchedShow
{
    /// <summary>
    /// Gets or sets the show information.
    /// </summary>
    [JsonPropertyName("show")]
    public TraktShow Show { get; set; } = new TraktShow();

    /// <summary>
    /// Gets or sets the season progress information.
    /// </summary>
    [JsonPropertyName("seasons")]
    public TraktShowSeasonProgress[] Seasons { get; set; } = Array.Empty<TraktShowSeasonProgress>();

    /// <summary>
    /// Gets or sets the last watched date.
    /// </summary>
    [JsonPropertyName("last_watched_at")]
    public DateTime? LastWatchedAt { get; set; }
}
