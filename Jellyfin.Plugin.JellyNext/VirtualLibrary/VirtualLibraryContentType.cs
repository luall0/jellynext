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
    ShowsNextSeasons,

    /// <summary>
    /// Trending movies (global, not per-user).
    /// </summary>
    MoviesTrending
}
