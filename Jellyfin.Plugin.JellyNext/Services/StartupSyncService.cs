using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyNext.ScheduledTasks;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyNext.Services;

/// <summary>
/// Service that triggers content sync at startup.
/// </summary>
public class StartupSyncService : IHostedService
{
    private readonly ILogger<StartupSyncService> _logger;
    private readonly ITaskManager _taskManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupSyncService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="taskManager">The task manager.</param>
    public StartupSyncService(
        ILogger<StartupSyncService> logger,
        ITaskManager taskManager)
    {
        _logger = logger;
        _taskManager = taskManager;
        _logger.LogInformation("StartupSyncService constructor called - service instantiated");
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StartupSyncService started - will trigger sync in 5 seconds");

        // Trigger sync task asynchronously (don't block startup)
        _ = Task.Run(
            async () =>
            {
                try
                {
                    // Wait a bit for Jellyfin to fully initialize
                    _logger.LogDebug("Waiting 5 seconds for Jellyfin initialization...");
                    await Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None);

                    _logger.LogInformation("Triggering content sync on startup using ITaskManager.QueueScheduledTask");

                    // Queue the sync task using Jellyfin's task manager API
                    _taskManager.QueueScheduledTask<ContentSyncScheduledTask>();

                    _logger.LogInformation("Content sync task queued successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error queueing startup content sync");
                }
            },
            CancellationToken.None);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StartupSyncService stopping...");
        return Task.CompletedTask;
    }
}
