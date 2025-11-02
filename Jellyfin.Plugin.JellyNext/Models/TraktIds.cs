using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models;

/// <summary>
/// Represents Trakt item IDs from various sources.
/// </summary>
public class TraktIds
{
    /// <summary>
    /// Gets or sets the Trakt ID.
    /// </summary>
    [JsonPropertyName("trakt")]
    public int Trakt { get; set; }

    /// <summary>
    /// Gets or sets the Slug.
    /// </summary>
    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    /// <summary>
    /// Gets or sets the TVDB ID.
    /// </summary>
    [JsonPropertyName("tvdb")]
    public int? Tvdb { get; set; }

    /// <summary>
    /// Gets or sets the IMDB ID.
    /// </summary>
    [JsonPropertyName("imdb")]
    public string? Imdb { get; set; }

    /// <summary>
    /// Gets or sets the TMDB ID.
    /// </summary>
    [JsonPropertyName("tmdb")]
    public int? Tmdb { get; set; }
}
