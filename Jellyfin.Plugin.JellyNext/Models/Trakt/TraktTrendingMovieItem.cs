using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models.Trakt;

/// <summary>
/// Represents a trending movie item from Trakt API.
/// </summary>
public class TraktTrendingMovieItem
{
    /// <summary>
    /// Gets or sets the number of watchers.
    /// </summary>
    [JsonPropertyName("watchers")]
    public int Watchers { get; set; }

    /// <summary>
    /// Gets or sets the movie.
    /// </summary>
    [JsonPropertyName("movie")]
    public TraktMovie Movie { get; set; } = new TraktMovie();
}
