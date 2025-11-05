using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models.Radarr;

/// <summary>
/// Represents a Radarr root folder.
/// </summary>
public class RadarrRootFolder
{
    /// <summary>
    /// Gets or sets the root folder ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the root folder path.
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the free space in bytes.
    /// </summary>
    [JsonPropertyName("freeSpace")]
    public long FreeSpace { get; set; }
}
