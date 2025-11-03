using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyNext.Services;
using Jellyfin.Plugin.JellyNext.VirtualLibrary;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyNext.ScheduledTasks;

/// <summary>
/// Scheduled task for syncing content from Trakt.
/// </summary>
public class ContentSyncScheduledTask : IScheduledTask
{
    private readonly ILogger<ContentSyncScheduledTask> _logger;
    private readonly ContentSyncService _syncService;
    private readonly VirtualLibraryManager _virtualLibraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSyncScheduledTask"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="syncService">The content sync service.</param>
    /// <param name="virtualLibraryManager">The virtual library manager.</param>
    public ContentSyncScheduledTask(
        ILogger<ContentSyncScheduledTask> logger,
        ContentSyncService syncService,
        VirtualLibraryManager virtualLibraryManager)
    {
        _logger = logger;
        _syncService = syncService;
        _virtualLibraryManager = virtualLibraryManager;

        // Initialize virtual library on first construction
        _virtualLibraryManager.Initialize();
    }

    /// <inheritdoc />
    public string Name => "Sync Trakt Content";

    /// <inheritdoc />
    public string Key => "JellyNextContentSync";

    /// <inheritdoc />
    public string Description => "Syncs recommendations and other content from Trakt for all linked users";

    /// <inheritdoc />
    public string Category => "JellyNext";

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting scheduled content sync");

        try
        {
            progress?.Report(0);
            await _syncService.SyncAllAsync(cancellationToken);
            progress?.Report(80);

            // Refresh virtual library stub files after sync
            _virtualLibraryManager.RefreshStubFiles();
            progress?.Report(100);

            _logger.LogInformation("Scheduled content sync completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled content sync");
            throw;
        }
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        // Default to 6 hours - users can configure this in Jellyfin Dashboard
        return new[]
        {
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerInterval,
                IntervalTicks = TimeSpan.FromHours(6).Ticks
            }
        };
    }
}
