using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyNext.Services;

/// <summary>
/// Service for querying the local Jellyfin library.
/// </summary>
public class LocalLibraryService
{
    private readonly ILogger<LocalLibraryService> _logger;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalLibraryService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="libraryManager">The library manager.</param>
    public LocalLibraryService(ILogger<LocalLibraryService> logger, ILibraryManager libraryManager)
    {
        _logger = logger;
        _libraryManager = libraryManager;
    }

    /// <summary>
    /// Finds a TV series in the local library by TVDB ID.
    /// </summary>
    /// <param name="tvdbId">The TVDB ID.</param>
    /// <returns>The series if found, null otherwise.</returns>
    public Series? FindSeriesByTvdbId(int tvdbId)
    {
        var allItems = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Series },
            HasTvdbId = true,
            Recursive = true
        });

        return allItems
            .OfType<Series>()
            .Where(s => !s.Path?.Contains("jellynext-virtual", StringComparison.OrdinalIgnoreCase) ?? true)
            .FirstOrDefault(s => s.GetProviderId(MetadataProvider.Tvdb) == tvdbId.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Gets the season numbers that exist locally for a series.
    /// </summary>
    /// <param name="series">The series.</param>
    /// <returns>Set of season numbers that exist locally.</returns>
    public HashSet<int> GetLocalSeasons(Series series)
    {
        var seasons = new HashSet<int>();

        var seasonItems = _libraryManager.GetItemList(new InternalItemsQuery
        {
            ParentId = series.Id,
            IncludeItemTypes = new[] { BaseItemKind.Season },
            Recursive = false
        });

        foreach (var item in seasonItems.OfType<Season>())
        {
            // Skip virtual items created by this plugin
            if (item.Path?.Contains("jellynext-virtual", StringComparison.OrdinalIgnoreCase) == true)
            {
                continue;
            }

            if (item.IndexNumber.HasValue)
            {
                seasons.Add(item.IndexNumber.Value);
            }
        }

        return seasons;
    }

    /// <summary>
    /// Checks if a specific season exists locally for a series.
    /// </summary>
    /// <param name="tvdbId">The TVDB ID of the series.</param>
    /// <param name="seasonNumber">The season number to check.</param>
    /// <returns>True if the season exists locally, false otherwise.</returns>
    public bool DoesSeasonExist(int tvdbId, int seasonNumber)
    {
        var series = FindSeriesByTvdbId(tvdbId);
        if (series == null)
        {
            return false;
        }

        var localSeasons = GetLocalSeasons(series);
        return localSeasons.Contains(seasonNumber);
    }
}
