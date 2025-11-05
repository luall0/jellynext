using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models.Radarr;

/// <summary>
/// Represents a movie in Radarr.
/// </summary>
public class RadarrMovie
{
    /// <summary>
    /// Gets or sets the movie ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the movie title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the TMDB ID.
    /// </summary>
    [JsonPropertyName("tmdbId")]
    public int TmdbId { get; set; }

    /// <summary>
    /// Gets or sets the IMDB ID.
    /// </summary>
    [JsonPropertyName("imdbId")]
    public string? ImdbId { get; set; }

    /// <summary>
    /// Gets or sets the year.
    /// </summary>
    [JsonPropertyName("year")]
    public int Year { get; set; }

    /// <summary>
    /// Gets or sets the quality profile ID.
    /// </summary>
    [JsonPropertyName("qualityProfileId")]
    public int QualityProfileId { get; set; }

    /// <summary>
    /// Gets or sets the root folder path.
    /// </summary>
    [JsonPropertyName("rootFolderPath")]
    public string RootFolderPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to monitor the movie.
    /// </summary>
    [JsonPropertyName("monitored")]
    public bool Monitored { get; set; }

    /// <summary>
    /// Gets or sets the add options.
    /// </summary>
    [JsonPropertyName("addOptions")]
    public RadarrAddOptions AddOptions { get; set; } = new();
}