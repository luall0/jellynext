using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyNext.Helpers;
using Jellyfin.Plugin.JellyNext.Models;
using Jellyfin.Plugin.JellyNext.Services;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyNext.Providers;

/// <summary>
/// Provider for Trakt personalized recommendations.
/// </summary>
public class RecommendationsProvider : IContentProvider
{
    private readonly ILogger<RecommendationsProvider> _logger;
    private readonly TraktApi _traktApi;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecommendationsProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="traktApi">The Trakt API service.</param>
    public RecommendationsProvider(ILogger<RecommendationsProvider> logger, TraktApi traktApi)
    {
        _logger = logger;
        _traktApi = traktApi;
    }

    /// <inheritdoc />
    public string ProviderName => "recommendations";

    /// <inheritdoc />
    public string LibraryName => "Trakt Recommendations";

    /// <inheritdoc />
    public bool IsEnabledForUser(Guid userId)
    {
        var traktUser = UserHelper.GetTraktUser(userId);
        return traktUser != null && !string.IsNullOrWhiteSpace(traktUser.AccessToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContentItem>> FetchContentAsync(Guid userId)
    {
        var traktUser = UserHelper.GetTraktUser(userId);
        if (traktUser == null)
        {
            _logger.LogWarning("No Trakt user found for Jellyfin user {UserId}", userId);
            return Array.Empty<ContentItem>();
        }

        var config = Plugin.Instance?.Configuration;
        if (config == null)
        {
            _logger.LogError("Plugin configuration not available");
            return Array.Empty<ContentItem>();
        }

        var contentItems = new List<ContentItem>();

        try
        {
            // Fetch movie recommendations
            var movies = await _traktApi.GetMovieRecommendations(
                traktUser,
                config.IgnoreCollected,
                config.IgnoreWatchlisted,
                limit: 50);

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
                    ProviderName = ProviderName
                });
            }

            // Fetch show recommendations
            var shows = await _traktApi.GetShowRecommendations(
                traktUser,
                config.IgnoreCollected,
                config.IgnoreWatchlisted,
                limit: 50);

            foreach (var show in shows)
            {
                contentItems.Add(new ContentItem
                {
                    Type = ContentType.Show,
                    Title = show.Title,
                    Year = show.Year,
                    TmdbId = show.Ids.Tmdb,
                    ImdbId = show.Ids.Imdb,
                    TvdbId = show.Ids.Tvdb,
                    TraktId = show.Ids.Trakt,
                    ProviderName = ProviderName
                });
            }

            _logger.LogInformation(
                "Fetched {MovieCount} movie and {ShowCount} show recommendations for user {UserId}",
                movies.Length,
                shows.Length,
                userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch recommendations for user {UserId}", userId);
        }

        return contentItems.AsReadOnly();
    }
}
