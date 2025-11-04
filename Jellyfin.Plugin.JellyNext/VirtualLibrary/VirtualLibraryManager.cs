using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.Plugin.JellyNext.Models;
using Jellyfin.Plugin.JellyNext.Services;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyNext.VirtualLibrary;

/// <summary>
/// Manages the virtual library by creating stub files for recommendations.
/// </summary>
public class VirtualLibraryManager
{
    private const string VirtualLibraryFolderName = "jellynext-virtual";
    private const string StubFileExtension = ".strm";
    private const string DummyVideoFileName = "dummy.mp4";
    private const string KeepFileName = ".keep";

    private readonly ContentCacheService _cacheService;
    private readonly ILogger<VirtualLibraryManager> _logger;
    private string? _virtualLibraryPath;
    private string? _dummyVideoPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualLibraryManager"/> class.
    /// </summary>
    /// <param name="cacheService">The content cache service.</param>
    /// <param name="logger">The logger.</param>
    public VirtualLibraryManager(
        ContentCacheService cacheService,
        ILogger<VirtualLibraryManager> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the virtual library directory.
    /// </summary>
    public void Initialize()
    {
        try
        {
            var pluginDataPath = Plugin.Instance?.DataFolderPath;
            if (string.IsNullOrEmpty(pluginDataPath))
            {
                _logger.LogWarning("Plugin data path not available");
                return;
            }

            _virtualLibraryPath = Path.Combine(pluginDataPath, VirtualLibraryFolderName);
            if (!Directory.Exists(_virtualLibraryPath))
            {
                Directory.CreateDirectory(_virtualLibraryPath);
                _logger.LogInformation("Created virtual library directory: {Path}", _virtualLibraryPath);
            }

            // Migrate old structure (clean up old .strm files in root)
            MigrateOldStructure();

            // Create dummy video file for FFprobe compatibility
            CreateDummyVideo();

            // Log setup instructions for each user
            LogSetupInstructions();

            // Create stub files for current recommendations
            RefreshStubFiles();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing virtual library");
        }
    }

    private void CreateDummyVideo()
    {
        if (string.IsNullOrEmpty(_virtualLibraryPath))
        {
            return;
        }

        try
        {
            _dummyVideoPath = Path.Combine(_virtualLibraryPath, DummyVideoFileName);

            // Only create if it doesn't exist
            if (File.Exists(_dummyVideoPath))
            {
                _logger.LogDebug("Dummy video already exists: {Path}", _dummyVideoPath);
                return;
            }

            // Extract embedded dummy video from resources
            var assembly = typeof(VirtualLibraryManager).Assembly;
            var resourceName = $"{assembly.GetName().Name}.Resources.{DummyVideoFileName}";

            _logger.LogInformation("Extracting embedded dummy video from resources...");

            using (var resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                {
                    _logger.LogWarning("Embedded dummy video not found in resources: {ResourceName}", resourceName);
                    return;
                }

                using (var fileStream = File.Create(_dummyVideoPath))
                {
                    resourceStream.CopyTo(fileStream);
                }
            }

            if (File.Exists(_dummyVideoPath))
            {
                var fileSize = new FileInfo(_dummyVideoPath).Length;
                _logger.LogInformation("Extracted dummy video file: {Path} (size: {Size} bytes)", _dummyVideoPath, fileSize);
            }
            else
            {
                _logger.LogWarning("Failed to extract dummy video file");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not extract dummy video file - .strm files will use fallback URL");
        }
    }

    private void MigrateOldStructure()
    {
        if (string.IsNullOrEmpty(_virtualLibraryPath))
        {
            return;
        }

        try
        {
            // Check for old .strm files in the root of jellynext-virtual
            var oldStubFiles = Directory.GetFiles(_virtualLibraryPath, $"*{StubFileExtension}", SearchOption.TopDirectoryOnly);
            if (oldStubFiles.Length > 0)
            {
                _logger.LogInformation(
                    "Found {Count} old stub files in root directory, cleaning up for migration to per-user structure",
                    oldStubFiles.Length);

                foreach (var file in oldStubFiles)
                {
                    File.Delete(file);
                    _logger.LogDebug("Deleted old stub file: {File}", file);
                }

                _logger.LogInformation("Migration complete. Old stub files removed. New per-user structure will be created.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during migration from old structure");
        }
    }

    private void LogSetupInstructions()
    {
        if (string.IsNullOrEmpty(_virtualLibraryPath))
        {
            return;
        }

        var config = Plugin.Instance?.Configuration;
        if (config?.TraktUsers == null || config.TraktUsers.Length == 0)
        {
            _logger.LogInformation("================================================================================");
            _logger.LogInformation("JellyNext Virtual Libraries Initialized");
            _logger.LogInformation("================================================================================");
            _logger.LogInformation("No Trakt users configured yet.");
            _logger.LogInformation("Link a Trakt account in the plugin settings to get started.");
            _logger.LogInformation("================================================================================");
            return;
        }

        _logger.LogInformation("================================================================================");
        _logger.LogInformation("JellyNext Virtual Libraries Initialized");
        _logger.LogInformation("================================================================================");
        _logger.LogInformation("IMPORTANT: Each content type is a SEPARATE library in Jellyfin");
        _logger.LogInformation("(e.g., \"admin's Movies Recommendations\", \"admin's Shows Recommendations\")");
        _logger.LogInformation("================================================================================");

        foreach (var traktUser in config.TraktUsers)
        {
            var userId = traktUser.LinkedMbUserId;

            _logger.LogInformation("User ID: {UserId}", userId);

            // Movies Recommendations
            var moviesRecoPath = GetUserLibraryPath(userId, VirtualLibraryContentType.MoviesRecommendations);
            _logger.LogInformation("  [1] Movie Recommendations:");
            _logger.LogInformation("      Path: {Path}", moviesRecoPath);
            _logger.LogInformation("      Library Type: Movies");
            _logger.LogInformation("      Suggested Name: \"[Your Username]'s Movies Recommendations\"");

            // Shows Recommendations
            var showsRecoPath = GetUserLibraryPath(userId, VirtualLibraryContentType.ShowsRecommendations);
            _logger.LogInformation("  [2] Show Recommendations:");
            _logger.LogInformation("      Path: {Path}", showsRecoPath);
            _logger.LogInformation("      Library Type: Shows");
            _logger.LogInformation("      Suggested Name: \"[Your Username]'s Shows Recommendations\"");

            _logger.LogInformation("  Setup Instructions:");
            _logger.LogInformation("    1. Go to Jellyfin Dashboard → Libraries → Add Media Library");
            _logger.LogInformation("    2. For EACH content type above, create a SEPARATE library:");
            _logger.LogInformation("       - Select content type (Movies or Shows)");
            _logger.LogInformation("       - Add the folder path shown above");
            _logger.LogInformation("       - Use the suggested library name");
            _logger.LogInformation("    3. This allows you to have separate libraries per recommendation type");
            _logger.LogInformation("--------------------------------------------------------------------------------");
        }

        _logger.LogInformation("================================================================================");
    }

    /// <summary>
    /// Refreshes stub files based on current cached recommendations for all users.
    /// </summary>
    public void RefreshStubFiles()
    {
        if (string.IsNullOrEmpty(_virtualLibraryPath))
        {
            _logger.LogWarning("Virtual library path not initialized");
            return;
        }

        try
        {
            var config = Plugin.Instance?.Configuration;
            if (config?.TraktUsers == null || config.TraktUsers.Length == 0)
            {
                _logger.LogDebug("No Trakt users configured, skipping stub file refresh");
                return;
            }

            // Refresh for all users
            foreach (var traktUser in config.TraktUsers)
            {
                var userId = traktUser.LinkedMbUserId;
                RefreshStubFilesForUser(userId, VirtualLibraryContentType.MoviesRecommendations);
                RefreshStubFilesForUser(userId, VirtualLibraryContentType.ShowsRecommendations);
                // Future: Add watchlist content types here
            }

            _logger.LogInformation("Stub file refresh complete for all users");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing stub files");
        }
    }

    /// <summary>
    /// Refreshes stub files for a specific user and content type.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="contentType">The content type.</param>
    public void RefreshStubFilesForUser(Guid userId, VirtualLibraryContentType contentType)
    {
        try
        {
            var userPath = GetUserLibraryPath(userId, contentType);

            // Ensure directory exists
            if (!Directory.Exists(userPath))
            {
                Directory.CreateDirectory(userPath);
                _logger.LogInformation("Created directory: {Path}", userPath);
            }

            // Create/maintain .keep file to prevent Jellyfin from ignoring empty directories
            var keepFile = Path.Combine(userPath, KeepFileName);
            if (!File.Exists(keepFile))
            {
                File.WriteAllText(keepFile, "This file ensures Jellyfin recognizes this directory even when empty.");
                _logger.LogDebug("Created .keep file: {Path}", keepFile);
            }

            var providerName = VirtualLibraryContentTypeHelper.GetProviderName(contentType);
            var cachedContent = _cacheService.GetCachedContent(userId, providerName);

            // Determine media type from content type
            var mediaType = VirtualLibraryContentTypeHelper.GetMediaType(contentType);
            var isMovies = mediaType == "Movies";

            // Filter content based on media type
            var items = isMovies
                ? cachedContent.Where(c => c.Type == ContentType.Movie && c.TmdbId.HasValue).ToList()
                : cachedContent.Where(c => c.Type == ContentType.Show && c.TvdbId.HasValue).ToList();

            _logger.LogInformation(
                "Refreshing {Count} {MediaType} recommendations for user {UserId}",
                items.Count,
                mediaType.ToLowerInvariant(),
                userId);

            if (isMovies)
            {
                // For movies: flat .strm files in root directory
                var existingFiles = Directory.GetFiles(userPath, $"*{StubFileExtension}");
                var currentTmdbIds = items.Select(m => m.TmdbId!.Value).ToHashSet();

                // Clean old stub files
                foreach (var file in existingFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var tmdbMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"\[tmdbid-(\d+)\]$");
                    if (tmdbMatch.Success && int.TryParse(tmdbMatch.Groups[1].Value, out var tmdbId))
                    {
                        if (!currentTmdbIds.Contains(tmdbId))
                        {
                            File.Delete(file);
                            _logger.LogDebug("Removed old stub file: {File}", file);
                        }
                    }
                }

                // Create new stub files
                foreach (var item in items)
                {
                    var title = SanitizeFilename(item.Title);
                    var year = item.Year?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "Unknown";
                    var tmdbId = item.TmdbId!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    var fileName = $"{title} ({year}) [tmdbid-{tmdbId}]{StubFileExtension}";
                    var stubFile = Path.Combine(userPath, fileName);

                    if (!File.Exists(stubFile))
                    {
                        // Point to dummy video file for FFprobe compatibility
                        // Playback interceptor detects virtual items by path, not file content
                        var content = !string.IsNullOrEmpty(_dummyVideoPath) && File.Exists(_dummyVideoPath)
                            ? _dummyVideoPath
                            : "http://jellynext-placeholder/movie"; // Fallback if dummy video creation failed
                        File.WriteAllText(stubFile, content);
                        _logger.LogDebug("Created stub file: {Title} ({Year})", item.Title, year);
                    }
                }
            }
            else
            {
                // For shows: create folder structure with per-season .strm files
                var existingDirs = Directory.GetDirectories(userPath);
                var currentTvdbIds = items.Select(s => s.TvdbId!.Value).ToHashSet();

                // Clean old show folders
                foreach (var dir in existingDirs)
                {
                    var dirName = Path.GetFileName(dir);
                    var tvdbMatch = System.Text.RegularExpressions.Regex.Match(dirName, @"\[tvdbid-(\d+)\]$");
                    if (tvdbMatch.Success && int.TryParse(tvdbMatch.Groups[1].Value, out var tvdbId))
                    {
                        if (!currentTvdbIds.Contains(tvdbId))
                        {
                            Directory.Delete(dir, recursive: true);
                            _logger.LogDebug("Removed old show folder: {Dir}", dir);
                        }
                    }
                }

                // Create new show folders with per-season .strm files
                foreach (var item in items)
                {
                    var title = SanitizeFilename(item.Title);
                    var year = item.Year?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "Unknown";
                    var tvdbId = item.TvdbId!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    var showFolderName = $"{title} ({year}) [tvdbid-{tvdbId}]";
                    var showFolder = Path.Combine(userPath, showFolderName);

                    if (!Directory.Exists(showFolder))
                    {
                        Directory.CreateDirectory(showFolder);
                    }

                    // Create per-season .strm files as fake episodes (seasons 1-10)
                    // Format: S01E01 - Download Season 1.strm (Jellyfin treats these as episodes)
                    // Users can trigger downloads for higher seasons manually
                    for (int seasonNumber = 1; seasonNumber <= 10; seasonNumber++)
                    {
                        var seasonFileName = $"S{seasonNumber:D2}E01 - Download Season {seasonNumber}{StubFileExtension}";
                        var stubFile = Path.Combine(showFolder, seasonFileName);

                        if (!File.Exists(stubFile))
                        {
                            // Point to dummy video file for FFprobe compatibility
                            // Playback interceptor will extract season number from filename pattern S##E##
                            var content = !string.IsNullOrEmpty(_dummyVideoPath) && File.Exists(_dummyVideoPath)
                                ? _dummyVideoPath
                                : "http://jellynext-placeholder/show"; // Fallback if dummy video creation failed
                            File.WriteAllText(stubFile, content);
                        }
                    }

                    _logger.LogDebug("Created show folder with per-season stubs: {Title} ({Year})", item.Title, year);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing stub files for user {UserId}", userId);
        }
    }

    /// <summary>
    /// Gets the virtual library path.
    /// </summary>
    /// <returns>The path to the virtual library folder.</returns>
    public string? GetVirtualLibraryPath()
    {
        return _virtualLibraryPath;
    }

    /// <summary>
    /// Gets the library path for a specific user and content type.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="contentType">The content type.</param>
    /// <returns>The full path to the user's content type directory.</returns>
    public string GetUserLibraryPath(Guid userId, VirtualLibraryContentType contentType)
    {
        if (string.IsNullOrEmpty(_virtualLibraryPath))
        {
            throw new InvalidOperationException("Virtual library path not initialized");
        }

        var directoryName = VirtualLibraryContentTypeHelper.GetDirectoryName(contentType);
        return Path.Combine(_virtualLibraryPath, userId.ToString(), directoryName);
    }

    /// <summary>
    /// Gets setup instructions for all users.
    /// </summary>
    /// <returns>Dictionary mapping user IDs to library paths and instructions.</returns>
    public Dictionary<Guid, Dictionary<VirtualLibraryContentType, string>> GetLibrarySetupInstructions()
    {
        var result = new Dictionary<Guid, Dictionary<VirtualLibraryContentType, string>>();
        var config = Plugin.Instance?.Configuration;

        if (config?.TraktUsers == null || config.TraktUsers.Length == 0)
        {
            return result;
        }

        foreach (var traktUser in config.TraktUsers)
        {
            var userId = traktUser.LinkedMbUserId;
            var userPaths = new Dictionary<VirtualLibraryContentType, string>
            {
                [VirtualLibraryContentType.MoviesRecommendations] =
                    GetUserLibraryPath(userId, VirtualLibraryContentType.MoviesRecommendations),
                [VirtualLibraryContentType.ShowsRecommendations] =
                    GetUserLibraryPath(userId, VirtualLibraryContentType.ShowsRecommendations)
            };

            result[userId] = userPaths;
        }

        return result;
    }

    private static string SanitizeFilename(string filename)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", filename.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Trim();
    }
}
