using System;
using System.Globalization;
using System.Linq;
using Jellyfin.Plugin.JellyNext.Models;
using Jellyfin.Plugin.JellyNext.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
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

        // Handle stub files (.strm) - check if it's in our virtual library
        if (args.Path.EndsWith(".strm", StringComparison.OrdinalIgnoreCase) &&
            args.Path.Contains("jellynext-virtual", StringComparison.OrdinalIgnoreCase))
        {
            return ResolveStubFile(args);
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

    private Folder CreateMoviesFolder(Guid _)
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

    private Movie? CreateMovieItem(Guid userId, string itemId, ItemResolveArgs _)
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

    private Movie? ResolveStubFile(ItemResolveArgs args)
    {
        // Extract TMDB ID from filename (e.g., "Thor (2011) [tmdbid-10195].strm" -> 10195)
        var fileName = System.IO.Path.GetFileNameWithoutExtension(args.Path);
        var tmdbMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"\[tmdbid-(\d+)\]$");

        if (!tmdbMatch.Success || !int.TryParse(tmdbMatch.Groups[1].Value, out var tmdbId))
        {
            _logger.LogWarning("Invalid stub filename format (expected '[tmdbid-XXXXX]'): {Path}", args.Path);
            return null;
        }

        // Get admin user (first Trakt user for now)
        var config = Plugin.Instance?.Configuration;
        if (config?.TraktUsers == null || config.TraktUsers.Length == 0)
        {
            _logger.LogWarning("No Trakt users configured");
            return null;
        }

        var adminUser = config.TraktUsers[0];
        var userId = adminUser.LinkedMbUserId;

        // Get cached recommendations
        var cachedContent = _cacheService.GetCachedContent(userId, "recommendations");
        var contentItem = cachedContent.FirstOrDefault(c => c.Type == ContentType.Movie && c.TmdbId == tmdbId);

        if (contentItem == null)
        {
            _logger.LogWarning("Movie not found in cache for TMDB ID: {TmdbId}", tmdbId);
            return null;
        }

        // Create movie item with proper metadata
        var movie = new Movie
        {
            Name = contentItem.Title,
            Path = args.Path,
            ProductionYear = contentItem.Year,
            PremiereDate = contentItem.Year.HasValue ? new DateTime(contentItem.Year.Value, 1, 1) : null
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

        _logger.LogInformation("Resolved stub file to movie: {Title} (TMDB: {TmdbId})", movie.Name, tmdbId);
        return movie;
    }
}
