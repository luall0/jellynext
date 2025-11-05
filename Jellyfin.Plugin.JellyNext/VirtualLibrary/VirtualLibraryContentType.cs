namespace Jellyfin.Plugin.JellyNext.VirtualLibrary;

/// <summary>
/// Types of virtual library content.
/// </summary>
public enum VirtualLibraryContentType
{
    /// <summary>
    /// Trakt movie recommendations.
    /// </summary>
    MoviesRecommendations,

    /// <summary>
    /// Trakt show recommendations.
    /// </summary>
    ShowsRecommendations,

    /// <summary>
    /// Trakt movie watchlist.
    /// </summary>
    WatchlistMovies,

    /// <summary>
    /// Trakt show watchlist.
    /// </summary>
    WatchlistShows,

    /// <summary>
    /// Next seasons of watched shows.
    /// </summary>
    ShowsNextSeasons
}

/// <summary>
/// Helper methods for virtual library content types.
/// </summary>
public static class VirtualLibraryContentTypeHelper
{
    /// <summary>
    /// Gets the directory name for a content type.
    /// </summary>
    /// <param name="contentType">The content type.</param>
    /// <returns>The directory name (e.g., "movies_recommendations").</returns>
    public static string GetDirectoryName(VirtualLibraryContentType contentType)
    {
        return contentType switch
        {
            VirtualLibraryContentType.MoviesRecommendations => "movies_recommendations",
            VirtualLibraryContentType.ShowsRecommendations => "shows_recommendations",
            VirtualLibraryContentType.WatchlistMovies => "watchlist_movies",
            VirtualLibraryContentType.WatchlistShows => "watchlist_shows",
            VirtualLibraryContentType.ShowsNextSeasons => "shows_nextseasons",
            _ => throw new System.ArgumentOutOfRangeException(nameof(contentType), contentType, null)
        };
    }

    /// <summary>
    /// Gets the provider name for a content type.
    /// </summary>
    /// <param name="contentType">The content type.</param>
    /// <returns>The provider name (e.g., "recommendations").</returns>
    public static string GetProviderName(VirtualLibraryContentType contentType)
    {
        return contentType switch
        {
            VirtualLibraryContentType.MoviesRecommendations => "recommendations",
            VirtualLibraryContentType.ShowsRecommendations => "recommendations",
            VirtualLibraryContentType.WatchlistMovies => "watchlist",
            VirtualLibraryContentType.WatchlistShows => "watchlist",
            VirtualLibraryContentType.ShowsNextSeasons => "nextseasons",
            _ => throw new System.ArgumentOutOfRangeException(nameof(contentType), contentType, null)
        };
    }

    /// <summary>
    /// Gets the display name for a content type.
    /// </summary>
    /// <param name="contentType">The content type.</param>
    /// <returns>The display name (e.g., "Movie Recommendations").</returns>
    public static string GetDisplayName(VirtualLibraryContentType contentType)
    {
        return contentType switch
        {
            VirtualLibraryContentType.MoviesRecommendations => "Movie Recommendations",
            VirtualLibraryContentType.ShowsRecommendations => "Show Recommendations",
            VirtualLibraryContentType.WatchlistMovies => "Movie Watchlist",
            VirtualLibraryContentType.WatchlistShows => "Show Watchlist",
            VirtualLibraryContentType.ShowsNextSeasons => "Next Seasons",
            _ => throw new System.ArgumentOutOfRangeException(nameof(contentType), contentType, null)
        };
    }

    /// <summary>
    /// Gets the media type (Movies or Shows) for a content type.
    /// </summary>
    /// <param name="contentType">The content type.</param>
    /// <returns>"Movies" or "Shows".</returns>
    public static string GetMediaType(VirtualLibraryContentType contentType)
    {
        return contentType switch
        {
            VirtualLibraryContentType.MoviesRecommendations => "Movies",
            VirtualLibraryContentType.ShowsRecommendations => "Shows",
            VirtualLibraryContentType.WatchlistMovies => "Movies",
            VirtualLibraryContentType.WatchlistShows => "Shows",
            VirtualLibraryContentType.ShowsNextSeasons => "Shows",
            _ => throw new System.ArgumentOutOfRangeException(nameof(contentType), contentType, null)
        };
    }

    /// <summary>
    /// Tries to parse a directory name to a content type.
    /// </summary>
    /// <param name="directoryName">The directory name.</param>
    /// <param name="contentType">The parsed content type.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public static bool TryParseDirectoryName(string directoryName, out VirtualLibraryContentType contentType)
    {
        contentType = directoryName switch
        {
            "movies_recommendations" => VirtualLibraryContentType.MoviesRecommendations,
            "shows_recommendations" => VirtualLibraryContentType.ShowsRecommendations,
            "watchlist_movies" => VirtualLibraryContentType.WatchlistMovies,
            "watchlist_shows" => VirtualLibraryContentType.WatchlistShows,
            "shows_nextseasons" => VirtualLibraryContentType.ShowsNextSeasons,
            _ => default
        };

        return directoryName is "movies_recommendations" or "shows_recommendations" or "watchlist_movies" or "watchlist_shows" or "shows_nextseasons";
    }
}
