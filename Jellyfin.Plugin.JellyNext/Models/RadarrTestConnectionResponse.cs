using System.Collections.Generic;

namespace Jellyfin.Plugin.JellyNext.Models;

/// <summary>
/// Represents the response from testing Radarr connection.
/// </summary>
public class RadarrTestConnectionResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the connection is successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if connection failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the Radarr version.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the available quality profiles.
    /// </summary>
    public List<RadarrQualityProfile> QualityProfiles { get; set; } = new List<RadarrQualityProfile>();

    /// <summary>
    /// Gets or sets the available root folders.
    /// </summary>
    public List<RadarrRootFolder> RootFolders { get; set; } = new List<RadarrRootFolder>();
}
