using System;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models.Trakt;

/// <summary>
/// Represents a watchlist movie item from Trakt API.
/// </summary>
public class TraktWatchlistMovieItem
{
    /// <summary>
    /// Gets or sets when the item was added to the watchlist.
    /// </summary>
    [JsonPropertyName("listed_at")]
    public DateTime ListedAt { get; set; }

    /// <summary>
    /// Gets or sets the movie.
    /// </summary>
    [JsonPropertyName("movie")]
    public TraktMovie Movie { get; set; } = new TraktMovie();
}
