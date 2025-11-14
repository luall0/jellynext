using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models.Jellyseerr;

/// <summary>
/// Root folder information from Jellyseerr.
/// </summary>
public class RootFolder
{
    /// <summary>
    /// Gets or sets the folder ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the folder path.
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the free space in bytes.
    /// </summary>
    [JsonPropertyName("freeSpace")]
    public long FreeSpace { get; set; }
}
