using System;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models.Trakt;

/// <summary>
/// Represents a history item from Trakt watch history.
/// </summary>
public class TraktHistoryItem
{
    /// <summary>
    /// Gets or sets the unique ID for this history item.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets when this item was watched.
    /// </summary>
    [JsonPropertyName("watched_at")]
    public DateTime WatchedAt { get; set; }

    /// <summary>
    /// Gets or sets the action (usually "watch").
    /// </summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = "watch";

    /// <summary>
    /// Gets or sets the type (e.g., "episode").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "episode";

    /// <summary>
    /// Gets or sets the show information.
    /// </summary>
    [JsonPropertyName("show")]
    public TraktShow Show { get; set; } = new TraktShow();

    /// <summary>
    /// Gets or sets the episode information.
    /// </summary>
    [JsonPropertyName("episode")]
    public TraktEpisode? Episode { get; set; }
}
