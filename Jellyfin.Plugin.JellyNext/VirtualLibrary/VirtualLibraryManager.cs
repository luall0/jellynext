using System;
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

    private readonly ContentCacheService _cacheService;
    private readonly ILogger<VirtualLibraryManager> _logger;
    private string? _virtualLibraryPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualLibraryManager"/> class.
    /// </summary>
    /// <param name="cacheService">The content cache service.</param>
    /// <param name="logger">The logger.</param>
    public VirtualLibraryManager(ContentCacheService cacheService, ILogger<VirtualLibraryManager> logger)
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

            _logger.LogInformation("================================================================================");
            _logger.LogInformation("JellyNext Virtual Library Initialized");
            _logger.LogInformation("================================================================================");
            _logger.LogInformation("Virtual Library Path: {Path}", _virtualLibraryPath);
            _logger.LogInformation("To use this library:");
            _logger.LogInformation("  1. Go to Jellyfin Dashboard → Libraries");
            _logger.LogInformation("  2. Click 'Add Media Library'");
            _logger.LogInformation("  3. Select 'Movies' as content type");
            _logger.LogInformation("  4. Add folder: {Path}", _virtualLibraryPath);
            _logger.LogInformation("  5. Name it 'Trakt Recommendations'");
            _logger.LogInformation("================================================================================");

            // Create stub files for current recommendations
            RefreshStubFiles();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing virtual library");
        }
    }

    /// <summary>
    /// Refreshes stub files based on current cached recommendations.
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
            // Get admin user (first Trakt user)
            var config = Plugin.Instance?.Configuration;
            if (config?.TraktUsers == null || config.TraktUsers.Length == 0)
            {
                _logger.LogDebug("No Trakt users configured, skipping stub file refresh");
                return;
            }

            var adminUser = config.TraktUsers[0];
            var userId = adminUser.LinkedMbUserId;

            // Get cached movie recommendations
            var cachedContent = _cacheService.GetCachedContent(userId, "recommendations");
            var movies = cachedContent.Where(c => c.Type == ContentType.Movie && c.TmdbId.HasValue).ToList();

            _logger.LogInformation("Creating stub files for {Count} movie recommendations", movies.Count);

            // Clean old stub files (remove files that are no longer in recommendations)
            var existingFiles = Directory.GetFiles(_virtualLibraryPath, $"*{StubFileExtension}");
            var currentTmdbIds = movies.Select(m => m.TmdbId!.Value).ToHashSet();

            foreach (var file in existingFiles)
            {
                // Extract TMDB ID from filename (format: "Title (Year) [tmdbid-12345].strm")
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

            // Create new stub files with proper naming: "Title (Year) [tmdbid-12345].strm"
            foreach (var movie in movies)
            {
                // Sanitize title for filename
                var title = SanitizeFilename(movie.Title);
                var year = movie.Year?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "Unknown";
                var tmdbId = movie.TmdbId!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var fileName = $"{title} ({year}) [tmdbid-{tmdbId}]{StubFileExtension}";
                var stubFile = Path.Combine(_virtualLibraryPath, fileName);

                if (!File.Exists(stubFile))
                {
                    // Write TMDB ID as content - this helps if we need to debug
                    File.WriteAllText(stubFile, $"plugin://jellynext/movie/{movie.TmdbId}");
                    _logger.LogDebug("Created stub file: {Title} ({Year})", movie.Title, year);
                }
            }

            _logger.LogInformation("Stub file refresh complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing stub files");
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

    private static string SanitizeFilename(string filename)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", filename.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Trim();
    }
}
