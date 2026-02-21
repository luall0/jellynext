using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyNext.Services;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyNext.ScheduledTasks;

/// <summary>
/// Scheduled task for syncing Trakt watchlists and automatically adding items to download systems.
/// </summary>
public class WatchlistSyncScheduledTask : IScheduledTask
{
    private readonly ILogger<WatchlistSyncScheduledTask> _logger;
    private readonly WatchlistSyncService _syncService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WatchlistSyncScheduledTask"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="syncService">The watchlist sync service.</param>
    public WatchlistSyncScheduledTask(
        ILogger<WatchlistSyncScheduledTask> logger,
        WatchlistSyncService syncService)
    {
        _logger = logger;
        _syncService = syncService;
    }

    /// <inheritdoc />
    public string Name => "Sync Trakt Watchlists";

    /// <inheritdoc />
    public string Key => "JellyNextWatchlistSync";

    /// <inheritdoc />
    public string Description => "Automatically adds watchlisted movies and shows to Radarr/Sonarr/Jellyseerr";

    /// <inheritdoc />
    public string Category => "JellyNext";

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting scheduled watchlist sync");

        try
        {
            progress?.Report(0);
            await _syncService.SyncAllAsync(cancellationToken);
            progress?.Report(100);

            _logger.LogInformation("Scheduled watchlist sync completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled watchlist sync");
            throw;
        }
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        // Run every 1 hour by default (watchlists change more frequently than recommendations)
        return new[]
        {
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.IntervalTrigger,
                IntervalTicks = TimeSpan.FromHours(1).Ticks
            }
        };
    }
}
