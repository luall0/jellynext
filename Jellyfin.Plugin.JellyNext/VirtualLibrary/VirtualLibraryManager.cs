using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.Plugin.JellyNext.Models.Common;
using Jellyfin.Plugin.JellyNext.Models.Radarr;
using Jellyfin.Plugin.JellyNext.Models.Sonarr;
using Jellyfin.Plugin.JellyNext.Models.Trakt;
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
    private const string DummyVideoShortFileName = "dummy_short.mp4";
    private const string KeepFileName = ".keep";

    private readonly ContentCacheService _cacheService;
    private readonly ILogger<VirtualLibraryManager> _logger;
    private string? _virtualLibraryPath;
    private string? _dummyVideoPath;
    private string? _dummyVideoShortPath;

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

            // Create dummy video files for FFprobe compatibility
            CreateDummyVideo();
            CreateDummyVideoShort();

            // Initialize global directories if enabled
            InitializeGlobalDirectories();

            // Log setup instructions for each user
            LogSetupInstructions();
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

    private void CreateDummyVideoShort()
    {
        if (string.IsNullOrEmpty(_virtualLibraryPath))
        {
            return;
        }

        try
        {
            _dummyVideoShortPath = Path.Combine(_virtualLibraryPath, DummyVideoShortFileName);

            // Only create if it doesn't exist
            if (File.Exists(_dummyVideoShortPath))
            {
                _logger.LogDebug("Short dummy video already exists: {Path}", _dummyVideoShortPath);
                return;
            }

            // Extract embedded short dummy video from resources
            var assembly = typeof(VirtualLibraryManager).Assembly;
            var resourceName = $"{assembly.GetName().Name}.Resources.{DummyVideoShortFileName}";

            _logger.LogInformation("Extracting embedded short dummy video from resources...");

            using (var resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                {
                    _logger.LogWarning("Embedded short dummy video not found in resources: {ResourceName}", resourceName);
                    return;
                }

                using (var fileStream = File.Create(_dummyVideoShortPath))
                {
                    resourceStream.CopyTo(fileStream);
                }
            }

            if (File.Exists(_dummyVideoShortPath))
            {
                var fileSize = new FileInfo(_dummyVideoShortPath).Length;
                _logger.LogInformation("Extracted short dummy video file: {Path} (size: {Size} bytes)", _dummyVideoShortPath, fileSize);
            }
            else
            {
                _logger.LogWarning("Failed to extract short dummy video file");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not extract short dummy video file - will use regular dummy video as fallback");
        }
    }

    private void InitializeGlobalDirectories()
    {
        if (string.IsNullOrEmpty(_virtualLibraryPath))
        {
            _logger.LogWarning("Virtual library path not initialized, cannot create global directories");
            return;
        }

        try
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null)
            {
                return;
            }

            // Create trending movies directory if enabled
            if (config.TrendingMoviesEnabled)
            {
                var trendingPath = GetGlobalLibraryPath(VirtualLibraryContentType.MoviesTrending);
                EnsureDirectoryExists(trendingPath);
                _logger.LogInformation("Initialized global trending movies directory: {Path}", trendingPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing global directories");
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

            // Next Seasons
            var nextSeasonsPath = GetUserLibraryPath(userId, VirtualLibraryContentType.ShowsNextSeasons);
            _logger.LogInformation("  [3] Next Seasons:");
            _logger.LogInformation("      Path: {Path}", nextSeasonsPath);
            _logger.LogInformation("      Library Type: Shows");
            _logger.LogInformation("      Suggested Name: \"[Your Username]'s Next Seasons\"");

            _logger.LogInformation("  Setup Instructions:");
            _logger.LogInformation("    1. Go to Jellyfin Dashboard → Libraries → Add Media Library");
            _logger.LogInformation("    2. For EACH content type above, create a SEPARATE library:");
            _logger.LogInformation("       - Select content type (Movies or Shows)");
            _logger.LogInformation("       - Add the folder path shown above");
            _logger.LogInformation("       - Use the suggested library name");
            _logger.LogInformation("    3. This allows you to have separate libraries per recommendation type");
            _logger.LogInformation("--------------------------------------------------------------------------------");
        }

        // Show global content types if enabled
        if (config.TrendingMoviesEnabled)
        {
            _logger.LogInformation(" ");
            _logger.LogInformation("GLOBAL CONTENT (visible to all users):");
            _logger.LogInformation("--------------------------------------------------------------------------------");

            var trendingPath = GetGlobalLibraryPath(VirtualLibraryContentType.MoviesTrending);
            _logger.LogInformation("  [Global] Trending Movies:");
            _logger.LogInformation("      Path: {Path}", trendingPath);
            _logger.LogInformation("      Library Type: Movies");
            _logger.LogInformation("      Suggested Name: \"Trending Movies\"");
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
                RefreshStubFilesForUser(userId, VirtualLibraryContentType.ShowsNextSeasons);
                // Future: Add watchlist content types here
            }

            // Refresh global content types
            if (config.TrendingMoviesEnabled && config.TrendingMoviesUserId != Guid.Empty)
            {
                RefreshGlobalStubFiles(VirtualLibraryContentType.MoviesTrending);
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
            EnsureDirectoryExists(userPath);

            var providerName = VirtualLibraryContentTypeHelper.GetProviderName(contentType);
            var cachedContent = _cacheService.GetCachedContent(userId, providerName);

            var mediaType = VirtualLibraryContentTypeHelper.GetMediaType(contentType);
            var isMovies = mediaType == "Movies";

            var items = FilterContentByMediaType(cachedContent, isMovies);

            _logger.LogInformation(
                "Refreshing {Count} {MediaType} recommendations for user {UserId}",
                items.Count,
                mediaType.ToLowerInvariant(),
                userId);

            if (isMovies)
            {
                RefreshMovieStubFiles(userPath, items);
            }
            else
            {
                RefreshShowStubFiles(userPath, items, contentType, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing stub files for user {UserId}", userId);
        }
    }

    /// <summary>
    /// Refreshes stub files for a global content type.
    /// </summary>
    /// <param name="contentType">The content type.</param>
    public void RefreshGlobalStubFiles(VirtualLibraryContentType contentType)
    {
        try
        {
            if (!VirtualLibraryContentTypeHelper.IsGlobal(contentType))
            {
                _logger.LogWarning("Content type {ContentType} is not global, skipping", contentType);
                return;
            }

            var config = Plugin.Instance?.Configuration;
            if (config == null)
            {
                return;
            }

            // For trending movies, use the configured user ID
            Guid userId;
            if (contentType == VirtualLibraryContentType.MoviesTrending)
            {
                userId = config.TrendingMoviesUserId;
                if (userId == Guid.Empty)
                {
                    _logger.LogWarning("Trending movies enabled but no user ID configured");
                    return;
                }
            }
            else
            {
                _logger.LogWarning("Unknown global content type: {ContentType}", contentType);
                return;
            }

            var globalPath = GetGlobalLibraryPath(contentType);
            EnsureDirectoryExists(globalPath);

            var providerName = VirtualLibraryContentTypeHelper.GetProviderName(contentType);
            var cachedContent = _cacheService.GetCachedContent(userId, providerName);

            var mediaType = VirtualLibraryContentTypeHelper.GetMediaType(contentType);
            var isMovies = mediaType == "Movies";

            var items = FilterContentByMediaType(cachedContent, isMovies);

            _logger.LogInformation(
                "Refreshing {Count} {MediaType} for global content type {ContentType}",
                items.Count,
                mediaType.ToLowerInvariant(),
                contentType);

            if (isMovies)
            {
                RefreshMovieStubFiles(globalPath, items);
            }
            else
            {
                // Global shows not yet supported, but structure is here for future
                RefreshShowStubFiles(globalPath, items, contentType, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing global stub files for content type {ContentType}", contentType);
        }
    }

    private void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            _logger.LogInformation("Created directory: {Path}", path);
        }

        var keepFile = Path.Combine(path, KeepFileName);
        if (!File.Exists(keepFile))
        {
            File.WriteAllText(keepFile, "This file ensures Jellyfin recognizes this directory even when empty.");
            _logger.LogDebug("Created .keep file: {Path}", keepFile);
        }
    }

    private static List<ContentItem> FilterContentByMediaType(IReadOnlyList<ContentItem> content, bool isMovies)
    {
        return isMovies
            ? content.Where(c => c.Type == ContentType.Movie && c.TmdbId.HasValue).ToList()
            : content.Where(c => c.Type == ContentType.Show && c.TvdbId.HasValue).ToList();
    }

    private void RefreshMovieStubFiles(string userPath, List<ContentItem> items)
    {
        var existingFiles = Directory.GetFiles(userPath, $"*{StubFileExtension}");

        // Check if stub file content matches current configuration
        if (existingFiles.Length > 0 && !DoesStubContentMatch(existingFiles[0]))
        {
            _logger.LogInformation("Stub file content doesn't match current configuration, flushing directory: {Path}", userPath);
            FlushDirectory(userPath);
            existingFiles = Array.Empty<string>();
        }

        var currentTmdbIds = items.Select(m => m.TmdbId!.Value).ToHashSet();

        CleanOldMovieStubFiles(existingFiles, currentTmdbIds);
        CreateMovieStubFiles(userPath, items);
    }

    private void CleanOldMovieStubFiles(string[] existingFiles, HashSet<int> currentTmdbIds)
    {
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
    }

    private void CreateMovieStubFiles(string userPath, List<ContentItem> items)
    {
        foreach (var item in items)
        {
            var title = SanitizeFilename(item.Title);
            var year = item.Year?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "Unknown";
            var tmdbId = item.TmdbId!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var fileName = $"{title} ({year}) [tmdbid-{tmdbId}]{StubFileExtension}";
            var stubFile = Path.Combine(userPath, fileName);

            if (!File.Exists(stubFile))
            {
                var content = GetStubFileContent("movie");
                File.WriteAllText(stubFile, content);
                _logger.LogDebug("Created stub file: {Title} ({Year})", item.Title, year);
            }
        }
    }

    private void RefreshShowStubFiles(string userPath, List<ContentItem> items, VirtualLibraryContentType contentType, Guid userId)
    {
        var existingDirs = Directory.GetDirectories(userPath);

        // Check if stub file content matches current configuration
        if (existingDirs.Length > 0)
        {
            var firstShowStubs = Directory.GetFiles(existingDirs[0], $"*{StubFileExtension}");
            if (firstShowStubs.Length > 0 && !DoesStubContentMatch(firstShowStubs[0]))
            {
                _logger.LogInformation("Stub file content doesn't match current configuration, flushing directory: {Path}", userPath);
                FlushDirectory(userPath);
                existingDirs = Array.Empty<string>();
            }
        }

        var currentTvdbIds = items.Select(s => s.TvdbId!.Value).ToHashSet();

        CleanOldShowFolders(existingDirs, currentTvdbIds);

        var isNextSeasons = contentType == VirtualLibraryContentType.ShowsNextSeasons;

        foreach (var item in items)
        {
            var showFolder = CreateShowFolder(userPath, item);

            if (isNextSeasons && item.SeasonNumber.HasValue)
            {
                CreateNextSeasonStub(showFolder, item);
            }
            else
            {
                CreateRegularShowStubs(showFolder, item, userId);
            }
        }
    }

    private void CleanOldShowFolders(string[] existingDirs, HashSet<int> currentTvdbIds)
    {
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
    }

    private string CreateShowFolder(string userPath, ContentItem item)
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

        return showFolder;
    }

    private void CreateNextSeasonStub(string showFolder, ContentItem item)
    {
        var seasonNumber = item.SeasonNumber!.Value;
        var seasonFileName = $"S{seasonNumber:D2}E01 - Download Season {seasonNumber}{StubFileExtension}";
        var stubFile = Path.Combine(showFolder, seasonFileName);

        if (!File.Exists(stubFile))
        {
            var content = GetStubFileContent("show");
            File.WriteAllText(stubFile, content);
        }

        var year = item.Year?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "Unknown";
        _logger.LogDebug(
            "Created next season stub: {Title} ({Year}) - Season {Season}",
            item.Title,
            year,
            seasonNumber);
    }

    private void CreateRegularShowStubs(string showFolder, ContentItem item, Guid userId)
    {
        var maxSeason = DetermineMaxSeason(item, userId);

        for (int seasonNumber = 1; seasonNumber <= maxSeason; seasonNumber++)
        {
            CreateSeasonStubFile(showFolder, seasonNumber);
        }

        CleanupExcessSeasonStubs(showFolder, maxSeason);

        var year = item.Year?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "Unknown";
        _logger.LogDebug("Created show folder with per-season stubs (S1-S{MaxSeason}): {Title} ({Year})", maxSeason, item.Title, year);
    }

    private int DetermineMaxSeason(ContentItem item, Guid userId)
    {
        var traktUser = Helpers.UserHelper.GetTraktUser(userId);

        if (traktUser?.LimitShowsToSeasonOne == true)
        {
            return 1;
        }

        if (item.AiredSeasonCount.HasValue && item.AiredSeasonCount.Value > 0)
        {
            return item.AiredSeasonCount.Value;
        }

        return 10;
    }

    private void CreateSeasonStubFile(string showFolder, int seasonNumber)
    {
        var seasonFileName = $"S{seasonNumber:D2}E01 - Download Season {seasonNumber}{StubFileExtension}";
        var stubFile = Path.Combine(showFolder, seasonFileName);

        if (!File.Exists(stubFile))
        {
            var content = GetStubFileContent("show");
            File.WriteAllText(stubFile, content);
        }
    }

    private void CleanupExcessSeasonStubs(string showFolder, int maxSeason)
    {
        var existingStubFiles = Directory.GetFiles(showFolder, $"S*{StubFileExtension}");
        foreach (var stubFile in existingStubFiles)
        {
            var fileName = Path.GetFileName(stubFile);
            var seasonMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"S(\d+)E\d+");
            if (seasonMatch.Success && int.TryParse(seasonMatch.Groups[1].Value, out var existingSeasonNum))
            {
                if (existingSeasonNum > maxSeason)
                {
                    File.Delete(stubFile);
                    _logger.LogDebug("Removed stub file for season {Season} (max is {MaxSeason}): {File}", existingSeasonNum, maxSeason, fileName);
                }
            }
        }
    }

    private string GetStubFileContent(string placeholderType)
    {
        var config = Plugin.Instance?.Configuration;
        var useShortDummy = config?.UseShortDummyVideo ?? true;

        // Use short dummy if enabled and available, otherwise fall back to regular dummy
        if (useShortDummy && !string.IsNullOrEmpty(_dummyVideoShortPath) && File.Exists(_dummyVideoShortPath))
        {
            return _dummyVideoShortPath;
        }

        if (!string.IsNullOrEmpty(_dummyVideoPath) && File.Exists(_dummyVideoPath))
        {
            return _dummyVideoPath;
        }

        return $"http://jellynext-placeholder/{placeholderType}";
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

        // Global content types go in jellynext-virtual/global/[content-type]
        if (VirtualLibraryContentTypeHelper.IsGlobal(contentType))
        {
            return Path.Combine(_virtualLibraryPath, "global", directoryName);
        }

        // Per-user content types go in jellynext-virtual/[userId]/[content-type]
        return Path.Combine(_virtualLibraryPath, userId.ToString(), directoryName);
    }

    /// <summary>
    /// Gets the library path for global content type.
    /// </summary>
    /// <param name="contentType">The content type.</param>
    /// <returns>The full path to the global content type directory.</returns>
    public string GetGlobalLibraryPath(VirtualLibraryContentType contentType)
    {
        if (string.IsNullOrEmpty(_virtualLibraryPath))
        {
            throw new InvalidOperationException("Virtual library path not initialized");
        }

        if (!VirtualLibraryContentTypeHelper.IsGlobal(contentType))
        {
            throw new ArgumentException($"Content type {contentType} is not global", nameof(contentType));
        }

        var directoryName = VirtualLibraryContentTypeHelper.GetDirectoryName(contentType);
        return Path.Combine(_virtualLibraryPath, "global", directoryName);
    }

    /// <summary>
    /// Initializes directories for a new user immediately.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    public void InitializeUserDirectories(Guid userId)
    {
        if (string.IsNullOrEmpty(_virtualLibraryPath))
        {
            _logger.LogWarning("Virtual library path not initialized, cannot create user directories");
            return;
        }

        try
        {
            // Create all content type directories for the user
            EnsureDirectoryExists(GetUserLibraryPath(userId, VirtualLibraryContentType.MoviesRecommendations));
            EnsureDirectoryExists(GetUserLibraryPath(userId, VirtualLibraryContentType.ShowsRecommendations));
            EnsureDirectoryExists(GetUserLibraryPath(userId, VirtualLibraryContentType.ShowsNextSeasons));

            _logger.LogInformation("Initialized virtual library directories for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing directories for user {UserId}", userId);
        }
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

    /// <summary>
    /// Checks if stub file content matches the current configuration.
    /// </summary>
    /// <param name="stubFilePath">Path to a stub file to check.</param>
    /// <returns>True if the content matches current configuration, false otherwise.</returns>
    private bool DoesStubContentMatch(string stubFilePath)
    {
        try
        {
            if (!File.Exists(stubFilePath))
            {
                return false;
            }

            var currentContent = File.ReadAllText(stubFilePath).Trim();
            var expectedContent = GetStubFileContent("check");

            return currentContent == expectedContent;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking stub file content: {Path}", stubFilePath);
            return true; // Assume match on error to avoid unnecessary rebuilds
        }
    }

    /// <summary>
    /// Flushes all files and subdirectories in a directory, keeping only the .keep file.
    /// </summary>
    /// <param name="directoryPath">The directory to flush.</param>
    private void FlushDirectory(string directoryPath)
    {
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                return;
            }

            // Delete all subdirectories
            foreach (var dir in Directory.GetDirectories(directoryPath))
            {
                Directory.Delete(dir, recursive: true);
                _logger.LogDebug("Deleted directory: {Dir}", dir);
            }

            // Delete all files except .keep
            foreach (var file in Directory.GetFiles(directoryPath))
            {
                var fileName = Path.GetFileName(file);
                if (fileName != KeepFileName)
                {
                    File.Delete(file);
                    _logger.LogDebug("Deleted file: {File}", file);
                }
            }

            _logger.LogInformation("Flushed directory: {Path}", directoryPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flushing directory: {Path}", directoryPath);
        }
    }

    private static string SanitizeFilename(string filename)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", filename.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Trim();
    }
}
