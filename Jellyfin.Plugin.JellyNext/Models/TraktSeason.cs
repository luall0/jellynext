using System;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models;

/// <summary>
/// Represents a season from Trakt API.
/// </summary>
public class TraktSeason
{
    /// <summary>
    /// Gets or sets the season number.
    /// </summary>
    [JsonPropertyName("number")]
    public int Number { get; set; }

    /// <summary>
    /// Gets or sets the season IDs.
    /// </summary>
    [JsonPropertyName("ids")]
    public TraktIds Ids { get; set; } = new TraktIds();

    /// <summary>
    /// Gets or sets the total number of episodes in this season.
    /// </summary>
    [JsonPropertyName("episode_count")]
    public int EpisodeCount { get; set; }

    /// <summary>
    /// Gets or sets the number of episodes that have aired.
    /// </summary>
    [JsonPropertyName("aired_episodes")]
    public int AiredEpisodes { get; set; }

    /// <summary>
    /// Gets or sets when the season first aired.
    /// </summary>
    [JsonPropertyName("first_aired")]
    public DateTime? FirstAired { get; set; }
}

/// <summary>
/// Represents a basic episode in a season response.
/// </summary>
public class TraktSeasonEpisode
{
    /// <summary>
    /// Gets or sets the season number.
    /// </summary>
    [JsonPropertyName("season")]
    public int Season { get; set; }

    /// <summary>
    /// Gets or sets the episode number.
    /// </summary>
    [JsonPropertyName("number")]
    public int Number { get; set; }

    /// <summary>
    /// Gets or sets the episode title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the episode IDs.
    /// </summary>
    [JsonPropertyName("ids")]
    public TraktIds Ids { get; set; } = new TraktIds();

    /// <summary>
    /// Gets or sets when the episode first aired.
    /// </summary>
    [JsonPropertyName("first_aired")]
    public DateTime? FirstAired { get; set; }
}
