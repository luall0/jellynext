using System;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models.Trakt;

/// <summary>
/// Represents a Trakt TV show.
/// </summary>
public class TraktShow
{
    /// <summary>
    /// Gets or sets the show title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the show year.
    /// </summary>
    [JsonPropertyName("year")]
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets the show IDs.
    /// </summary>
    [JsonPropertyName("ids")]
    public TraktIds Ids { get; set; } = new TraktIds();

    /// <summary>
    /// Gets or sets the genres.
    /// </summary>
    [JsonPropertyName("genres")]
    public string[] Genres { get; set; } = Array.Empty<string>();
}
