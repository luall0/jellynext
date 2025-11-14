using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models.Jellyseerr;

/// <summary>
/// Radarr server configuration from Jellyseerr.
/// </summary>
public class RadarrServer
{
    /// <summary>
    /// Gets or sets the server ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the server name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the hostname.
    /// </summary>
    [JsonPropertyName("hostname")]
    public string Hostname { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the port.
    /// </summary>
    [JsonPropertyName("port")]
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the API key.
    /// </summary>
    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to use SSL.
    /// </summary>
    [JsonPropertyName("useSsl")]
    public bool UseSsl { get; set; }

    /// <summary>
    /// Gets or sets the active profile ID.
    /// </summary>
    [JsonPropertyName("activeProfileId")]
    public int ActiveProfileId { get; set; }

    /// <summary>
    /// Gets or sets the active profile name.
    /// </summary>
    [JsonPropertyName("activeProfileName")]
    public string ActiveProfileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the active directory.
    /// </summary>
    [JsonPropertyName("activeDirectory")]
    public string ActiveDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this is a 4K server.
    /// </summary>
    [JsonPropertyName("is4k")]
    public bool Is4k { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the default server.
    /// </summary>
    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }
}
