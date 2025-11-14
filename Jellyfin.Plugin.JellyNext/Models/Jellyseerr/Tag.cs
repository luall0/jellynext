using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models.Jellyseerr;

/// <summary>
/// Tag information from Jellyseerr.
/// </summary>
public class Tag
{
    /// <summary>
    /// Gets or sets the tag ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the tag label.
    /// </summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;
}
