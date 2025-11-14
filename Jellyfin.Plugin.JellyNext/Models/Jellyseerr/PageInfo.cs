using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models.Jellyseerr;

/// <summary>
/// Pagination information from Jellyseerr API.
/// </summary>
public class PageInfo
{
    /// <summary>
    /// Gets or sets the total number of pages.
    /// </summary>
    [JsonPropertyName("pages")]
    public int Pages { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the total number of results.
    /// </summary>
    [JsonPropertyName("results")]
    public int Results { get; set; }

    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    [JsonPropertyName("page")]
    public int Page { get; set; }
}
