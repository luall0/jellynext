namespace Jellyfin.Plugin.JellyNext.Models.Jellyseerr;

/// <summary>
/// Jellyseerr permission constants (bitmask values).
/// </summary>
public static class JellyseerrPermissions
{
    /// <summary>
    /// Admin permission (unrestricted access).
    /// </summary>
    public const int Admin = 2;

    /// <summary>
    /// Basic request permission for non-4K media.
    /// </summary>
    public const int Request = 32;

    /// <summary>
    /// Auto-approve permission for all non-4K media.
    /// </summary>
    public const int AutoApprove = 128;

    /// <summary>
    /// Auto-approve permission for non-4K movies.
    /// </summary>
    public const int AutoApproveMovie = 256;

    /// <summary>
    /// Auto-approve permission for non-4K TV shows.
    /// </summary>
    public const int AutoApproveTv = 512;

    /// <summary>
    /// Request permission for 4K media.
    /// </summary>
    public const int Request4K = 1024;

    /// <summary>
    /// Auto-approve permission for 4K media.
    /// </summary>
    public const int AutoApprove4K = 32768;
}
