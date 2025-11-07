using System;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models.Trakt;

/// <summary>
/// Represents a Trakt movie.
/// </summary>
public class TraktMovie
{
    /// <summary>
    /// Gets or sets the movie title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the movie year.
    /// </summary>
    [JsonPropertyName("year")]
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets the movie IDs.
    /// </summary>
    [JsonPropertyName("ids")]
    public TraktIds Ids { get; set; } = new TraktIds();

    /// <summary>
    /// Gets or sets the genres.
    /// </summary>
    [JsonPropertyName("genres")]
    public string[] Genres { get; set; } = Array.Empty<string>();
}
