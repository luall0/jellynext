using System;

namespace Jellyfin.Plugin.JellyNext.Models;

/// <summary>
/// Represents a content item (movie or show) from an external source.
/// </summary>
public class ContentItem
{
    /// <summary>
    /// Gets or sets the content type.
    /// </summary>
    public ContentType Type { get; set; }

    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the year.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets the TMDB ID.
    /// </summary>
    public int? TmdbId { get; set; }

    /// <summary>
    /// Gets or sets the IMDB ID.
    /// </summary>
    public string? ImdbId { get; set; }

    /// <summary>
    /// Gets or sets the TVDB ID (for shows).
    /// </summary>
    public int? TvdbId { get; set; }

    /// <summary>
    /// Gets or sets the provider-specific data.
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when this item was added.
    /// </summary>
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the Trakt ID.
    /// </summary>
    public int TraktId { get; set; }

    /// <summary>
    /// Gets or sets the season number (for next seasons provider).
    /// </summary>
    public int? SeasonNumber { get; set; }
}
