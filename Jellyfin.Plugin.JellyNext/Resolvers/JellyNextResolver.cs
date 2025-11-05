using System;
using System.Globalization;
using System.Linq;
using Jellyfin.Plugin.JellyNext.Models;
using Jellyfin.Plugin.JellyNext.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyNext.Resolvers;

/// <summary>
/// Resolver for JellyNext virtual libraries.
/// </summary>
public class JellyNextResolver : IItemResolver
{
    private const string VirtualPathPrefix = "jellynext://";
    private const string RecommendationsPath = "jellynext://recommendations";
    private const string RecommendationsMoviesPath = "jellynext://recommendations/movies";
    private const string VirtualLibraryFolderName = "jellynext-virtual";

    private readonly ILogger<JellyNextResolver> _logger;
    private readonly ContentCacheService _cacheService;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyNextResolver"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="cacheService">The content cache service.</param>
    public JellyNextResolver(
        ILogger<JellyNextResolver> logger,
        ContentCacheService cacheService)
    {
        _logger = logger;
        _cacheService = cacheService;
    }

    /// <inheritdoc />
    public ResolverPriority Priority => ResolverPriority.Plugin;

    /// <inheritdoc />
    public BaseItem? ResolvePath(ItemResolveArgs args)
    {
        if (string.IsNullOrEmpty(args?.Path))
        {
            return null;
        }

        // Check if path is in our virtual library
        if (args.Path.Contains("jellynext-virtual", StringComparison.OrdinalIgnoreCase))
        {
            // Handle stub files (.strm) for movies
            if (args.Path.EndsWith(".strm", StringComparison.OrdinalIgnoreCase))
            {
                return ResolveStubFile(args);
            }

            // Handle show folders (directories with tvdbid in name)
            if (args.IsDirectory &&
                args.Path.Contains("shows_recommendations", StringComparison.OrdinalIgnoreCase) &&
                System.Text.RegularExpressions.Regex.IsMatch(args.Path, @"\[tvdbid-\d+\]"))
            {
                return ResolveShowFolder(args);
            }
        }

        // Handle jellynext:// virtual paths
        if (!args.Path.StartsWith(VirtualPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        _logger.LogDebug("Resolving virtual path: {Path}", args.Path);

        // Get admin user (first Trakt user for now)
        var config = Plugin.Instance?.Configuration;
        if (config?.TraktUsers == null || config.TraktUsers.Length == 0)
        {
            _logger.LogWarning("No Trakt users configured");
            return null;
        }

        var adminUser = config.TraktUsers[0];
        var userId = adminUser.LinkedMbUserId;

        // Handle recommendations root folder
        if (args.Path.Equals(RecommendationsPath, StringComparison.OrdinalIgnoreCase))
        {
            return CreateRecommendationsFolder();
        }

        // Handle recommendations movies folder
        if (args.Path.Equals(RecommendationsMoviesPath, StringComparison.OrdinalIgnoreCase))
        {
            return CreateMoviesFolder(userId);
        }

        // Handle individual movie items
        if (args.Path.StartsWith(RecommendationsMoviesPath + "/", StringComparison.OrdinalIgnoreCase))
        {
            var itemId = args.Path.Substring((RecommendationsMoviesPath + "/").Length);
            return CreateMovieItem(userId, itemId, args);
        }

        return null;
    }

    private Folder CreateRecommendationsFolder()
    {
        return new Folder
        {
            Name = "Trakt Recommendations",
            Path = RecommendationsPath,
            Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow,
            IsVirtualItem = true
        };
    }

    private Folder CreateMoviesFolder(Guid userId)
    {
        return new Folder
        {
            Name = "Movies",
            Path = RecommendationsMoviesPath,
            Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
            ParentId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow,
            IsVirtualItem = true
        };
    }

    private Movie? CreateMovieItem(Guid userId, string itemId, ItemResolveArgs args)
    {
        // Get cached recommendations
        var cachedContent = _cacheService.GetCachedContent(userId, "recommendations");
        var movies = cachedContent.Where(c => c.Type == ContentType.Movie).ToList();

        // Find the movie by ID (using TMDB ID as identifier)
        if (!int.TryParse(itemId, out var tmdbId))
        {
            _logger.LogWarning("Invalid movie ID format: {ItemId}", itemId);
            return null;
        }

        var contentItem = movies.FirstOrDefault(m => m.TmdbId == tmdbId);
        if (contentItem == null)
        {
            _logger.LogWarning("Movie not found in cache: TMDB ID {TmdbId}", tmdbId);
            return null;
        }

        // Create virtual movie item
        var movie = new Movie
        {
            Name = contentItem.Title,
            Path = $"{RecommendationsMoviesPath}/{tmdbId}",
            Id = Guid.NewGuid(),
            ParentId = Guid.Parse("10000000-0000-0000-0000-000000000002"),
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow,
            ProductionYear = contentItem.Year,
            IsVirtualItem = true
        };

        // Add provider IDs
        if (contentItem.TmdbId.HasValue)
        {
            movie.ProviderIds[MediaBrowser.Model.Entities.MetadataProvider.Tmdb.ToString()] = contentItem.TmdbId.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrEmpty(contentItem.ImdbId))
        {
            movie.ProviderIds[MediaBrowser.Model.Entities.MetadataProvider.Imdb.ToString()] = contentItem.ImdbId;
        }

        _logger.LogDebug("Created virtual movie: {Title} (TMDB: {TmdbId})", movie.Name, tmdbId);
        return movie;
    }

    private Series? ResolveShowFolder(ItemResolveArgs args)
    {
        // Extract userId and content type from path: jellynext-virtual/[userId]/shows_recommendations/ShowName [tvdbid-XXXXX]
        var pathMatch = System.Text.RegularExpressions.Regex.Match(
            args.Path,
            @"jellynext-virtual[/\\]([a-f0-9-]+)[/\\]([^/\\]+)[/\\]");

        if (!pathMatch.Success)
        {
            _logger.LogWarning("Invalid virtual library path format: {Path}", args.Path);
            return null;
        }

        var userIdStr = pathMatch.Groups[1].Value;
        var contentTypeDir = pathMatch.Groups[2].Value;

        if (!Guid.TryParse(userIdStr, out var userId))
        {
            _logger.LogWarning("Invalid userId in path: {UserId}", userIdStr);
            return null;
        }

        // Parse content type from directory name
        if (!VirtualLibrary.VirtualLibraryContentTypeHelper.TryParseDirectoryName(
            contentTypeDir,
            out var contentType))
        {
            _logger.LogWarning("Unknown content type directory: {ContentType}", contentTypeDir);
            return null;
        }

        // Extract TVDB ID from folder name (e.g., "Breaking Bad (2008) [tvdbid-81189]" -> 81189)
        var folderName = System.IO.Path.GetFileName(args.Path);
        var tvdbMatch = System.Text.RegularExpressions.Regex.Match(folderName, @"\[tvdbid-(\d+)\]$");

        if (!tvdbMatch.Success || !int.TryParse(tvdbMatch.Groups[1].Value, out var tvdbId))
        {
            _logger.LogWarning("Invalid show folder format (expected '[tvdbid-XXXXX]'): {Path}", args.Path);
            return null;
        }

        // Get provider name from content type
        var providerName = VirtualLibrary.VirtualLibraryContentTypeHelper.GetProviderName(contentType);

        // Get cached content for this user and provider
        var cachedContent = _cacheService.GetCachedContent(userId, providerName);
        var contentItem = cachedContent.FirstOrDefault(c => c.Type == ContentType.Show && c.TvdbId == tvdbId);

        if (contentItem == null)
        {
            _logger.LogWarning("Show not found in cache for user {UserId}, TVDB ID: {TvdbId}", userId, tvdbId);
            return null;
        }

        // Create series item with proper metadata
        var series = new Series
        {
            Name = contentItem.Title,
            Path = args.Path,
            ProductionYear = contentItem.Year,
            PremiereDate = contentItem.Year.HasValue ? new DateTime(contentItem.Year.Value, 1, 1) : null
        };

        // Add provider IDs for metadata lookup
        if (contentItem.TvdbId.HasValue)
        {
            series.ProviderIds[MediaBrowser.Model.Entities.MetadataProvider.Tvdb.ToString()] =
                contentItem.TvdbId.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (contentItem.TmdbId.HasValue)
        {
            series.ProviderIds[MediaBrowser.Model.Entities.MetadataProvider.Tmdb.ToString()] =
                contentItem.TmdbId.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrEmpty(contentItem.ImdbId))
        {
            series.ProviderIds[MediaBrowser.Model.Entities.MetadataProvider.Imdb.ToString()] = contentItem.ImdbId;
        }

        _logger.LogInformation(
            "Resolved show folder to series for user {UserId}: {Title} (TVDB: {TvdbId})",
            userId,
            series.Name,
            tvdbId);
        return series;
    }

    private Movie? ResolveStubFile(ItemResolveArgs args)
    {
        // Only handle movie .strm files (shows use folders)
        // Extract userId and content type from path: jellynext-virtual/[userId]/movies_recommendations/...
        var pathMatch = System.Text.RegularExpressions.Regex.Match(
            args.Path,
            @"jellynext-virtual[/\\]([a-f0-9-]+)[/\\]([^/\\]+)[/\\]");

        if (!pathMatch.Success)
        {
            _logger.LogWarning("Invalid virtual library path format: {Path}", args.Path);
            return null;
        }

        var userIdStr = pathMatch.Groups[1].Value;
        var contentTypeDir = pathMatch.Groups[2].Value;

        if (!Guid.TryParse(userIdStr, out var userId))
        {
            _logger.LogWarning("Invalid userId in path: {UserId}", userIdStr);
            return null;
        }

        // Parse content type from directory name
        if (!VirtualLibrary.VirtualLibraryContentTypeHelper.TryParseDirectoryName(
            contentTypeDir,
            out var contentType))
        {
            _logger.LogWarning("Unknown content type directory: {ContentType}", contentTypeDir);
            return null;
        }

        // Extract TMDB ID from filename (e.g., "Thor (2011) [tmdbid-10195].strm" -> 10195)
        var fileName = System.IO.Path.GetFileNameWithoutExtension(args.Path);
        var tmdbMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"\[tmdbid-(\d+)\]$");

        if (!tmdbMatch.Success || !int.TryParse(tmdbMatch.Groups[1].Value, out var tmdbId))
        {
            _logger.LogWarning("Invalid stub filename format (expected '[tmdbid-XXXXX]'): {Path}", args.Path);
            return null;
        }

        // Get provider name from content type
        var providerName = VirtualLibrary.VirtualLibraryContentTypeHelper.GetProviderName(contentType);

        // Get cached content for this user and provider
        var cachedContent = _cacheService.GetCachedContent(userId, providerName);
        var contentItem = cachedContent.FirstOrDefault(c => c.Type == ContentType.Movie && c.TmdbId == tmdbId);

        if (contentItem == null)
        {
            _logger.LogWarning("Movie not found in cache for user {UserId}, TMDB ID: {TmdbId}", userId, tmdbId);
            return null;
        }

        // Create movie item with proper metadata
        var movie = new Movie
        {
            Name = contentItem.Title,
            Path = args.Path,
            ProductionYear = contentItem.Year,
            PremiereDate = contentItem.Year.HasValue ? new DateTime(contentItem.Year.Value, 1, 1) : null,
            RunTimeTicks = 90 * TimeSpan.TicksPerMinute, // Dummy 90 minute runtime
            Container = "strm"
        };

        // Add provider IDs for metadata lookup
        if (contentItem.TmdbId.HasValue)
        {
            movie.ProviderIds[MediaBrowser.Model.Entities.MetadataProvider.Tmdb.ToString()] =
                contentItem.TmdbId.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrEmpty(contentItem.ImdbId))
        {
            movie.ProviderIds[MediaBrowser.Model.Entities.MetadataProvider.Imdb.ToString()] = contentItem.ImdbId;
        }

        _logger.LogInformation(
            "Resolved stub file to movie for user {UserId}: {Title} (TMDB: {TmdbId})",
            userId,
            movie.Name,
            tmdbId);
        return movie;
    }
}
