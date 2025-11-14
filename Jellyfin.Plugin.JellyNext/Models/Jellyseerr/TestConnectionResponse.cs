namespace Jellyfin.Plugin.JellyNext.Models.Jellyseerr;

/// <summary>
/// Jellyseerr test connection response.
/// </summary>
public class TestConnectionResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the connection was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the Jellyseerr version.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the error message if connection failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
