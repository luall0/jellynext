using System;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models.Trakt;

/// <summary>
/// Represents a watchlist show item from Trakt API.
/// </summary>
public class TraktWatchlistShowItem
{
    /// <summary>
    /// Gets or sets when the item was added to the watchlist.
    /// </summary>
    [JsonPropertyName("listed_at")]
    public DateTime ListedAt { get; set; }

    /// <summary>
    /// Gets or sets the show.
    /// </summary>
    [JsonPropertyName("show")]
    public TraktShow Show { get; set; } = new TraktShow();
}
