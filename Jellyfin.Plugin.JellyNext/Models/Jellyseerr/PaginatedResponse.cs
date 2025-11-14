using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models.Jellyseerr;

/// <summary>
/// Paginated response from Jellyseerr API.
/// </summary>
/// <typeparam name="T">The type of items in the results.</typeparam>
public class PaginatedResponse<T>
{
    /// <summary>
    /// Gets or sets the pagination information.
    /// </summary>
    [JsonPropertyName("pageInfo")]
    public PageInfo? PageInfo { get; set; }

    /// <summary>
    /// Gets or sets the results.
    /// </summary>
    [JsonPropertyName("results")]
    public List<T> Results { get; set; } = new List<T>();
}
