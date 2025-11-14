namespace Jellyfin.Plugin.JellyNext.Models.Common;

/// <summary>
/// Result of a download request operation.
/// </summary>
public class DownloadResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the download request was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the user-facing message to display.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional request ID (if applicable).
    /// </summary>
    public string? RequestId { get; set; }
}
