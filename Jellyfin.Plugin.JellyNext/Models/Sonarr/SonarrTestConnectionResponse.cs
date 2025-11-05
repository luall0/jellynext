using System.Collections.Generic;

namespace Jellyfin.Plugin.JellyNext.Models.Sonarr;

/// <summary>
/// Represents the response from testing Sonarr connection.
/// </summary>
public class SonarrTestConnectionResponse
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
    /// Gets or sets the Sonarr version.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the available quality profiles.
    /// </summary>
    public List<SonarrQualityProfile> QualityProfiles { get; set; } = new List<SonarrQualityProfile>();

    /// <summary>
    /// Gets or sets the available root folders.
    /// </summary>
    public List<SonarrRootFolder> RootFolders { get; set; } = new List<SonarrRootFolder>();
}
