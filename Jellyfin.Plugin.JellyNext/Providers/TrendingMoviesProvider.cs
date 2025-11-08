using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyNext.Helpers;
using Jellyfin.Plugin.JellyNext.Models.Common;
using Jellyfin.Plugin.JellyNext.Services;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyNext.Providers;

/// <summary>
/// Provider for Trakt trending movies (global, not per-user).
/// </summary>
public class TrendingMoviesProvider : IContentProvider
{
    private readonly ILogger<TrendingMoviesProvider> _logger;
    private readonly TraktApi _traktApi;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrendingMoviesProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="traktApi">The Trakt API service.</param>
    public TrendingMoviesProvider(
        ILogger<TrendingMoviesProvider> logger,
        TraktApi traktApi)
    {
        _logger = logger;
        _traktApi = traktApi;
    }

    /// <inheritdoc />
    public string ProviderName => "trending";

    /// <inheritdoc />
    public string LibraryName => "Trending Movies";

    /// <inheritdoc />
    public bool IsEnabledForUser(Guid userId)
    {
        // Trending is global, not per-user
        // Check global configuration instead
        var config = Plugin.Instance?.Configuration;
        if (config == null)
        {
            return false;
        }

        return config.TrendingMoviesEnabled;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContentItem>> FetchContentAsync(Guid userId)
    {
        var config = Plugin.Instance?.Configuration;
        if (config == null || !config.TrendingMoviesEnabled)
        {
            return Array.Empty<ContentItem>();
        }

        // Get the user ID from config to fetch trending with
        var trendingUserId = config.TrendingMoviesUserId;
        if (trendingUserId == Guid.Empty)
        {
            _logger.LogWarning("Trending movies enabled but no user ID configured");
            return Array.Empty<ContentItem>();
        }

        var traktUser = UserHelper.GetTraktUser(trendingUserId);
        if (traktUser == null || string.IsNullOrWhiteSpace(traktUser.AccessToken))
        {
            _logger.LogWarning(
                "Trending movies enabled but user {UserId} has no valid Trakt authentication",
                trendingUserId);
            return Array.Empty<ContentItem>();
        }

        var contentItems = new List<ContentItem>();

        try
        {
            var limit = Math.Clamp(config.TrendingMoviesLimit, 1, 100);
            var movies = await _traktApi.GetTrendingMovies(traktUser, limit);

            foreach (var movie in movies)
            {
                contentItems.Add(new ContentItem
                {
                    Type = ContentType.Movie,
                    Title = movie.Title,
                    Year = movie.Year,
                    TmdbId = movie.Ids.Tmdb,
                    ImdbId = movie.Ids.Imdb,
                    TraktId = movie.Ids.Trakt,
                    ProviderName = ProviderName,
                    Genres = movie.Genres
                });
            }

            _logger.LogInformation(
                "Fetched {Count} trending movies",
                contentItems.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch trending movies");
        }

        return contentItems.AsReadOnly();
    }
}
