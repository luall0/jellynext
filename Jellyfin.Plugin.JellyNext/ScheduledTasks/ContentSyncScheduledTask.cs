using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyNext.Services;
using Jellyfin.Plugin.JellyNext.VirtualLibrary;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
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
    private readonly ILibraryManager _libraryManager;
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSyncScheduledTask"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="syncService">The content sync service.</param>
    /// <param name="virtualLibraryManager">The virtual library manager.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="fileSystem">The file system.</param>
    public ContentSyncScheduledTask(
        ILogger<ContentSyncScheduledTask> logger,
        ContentSyncService syncService,
        VirtualLibraryManager virtualLibraryManager,
        ILibraryManager libraryManager,
        IFileSystem fileSystem)
    {
        _logger = logger;
        _syncService = syncService;
        _virtualLibraryManager = virtualLibraryManager;
        _libraryManager = libraryManager;
        _fileSystem = fileSystem;

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
            progress?.Report(60);

            // Refresh virtual library stub files after sync
            _virtualLibraryManager.RefreshStubFiles();
            progress?.Report(80);

            // Trigger library scan for all virtual libraries
            await ScanVirtualLibrariesAsync(cancellationToken);
            progress?.Report(100);

            _logger.LogInformation("Scheduled content sync completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled content sync");
            throw;
        }
    }

    private async Task ScanVirtualLibrariesAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Get all virtual folders (libraries) that contain "jellynext-virtual" in their path
            var virtualFolders = _libraryManager.GetVirtualFolders()
                .Where(vf => vf.Locations.Any(loc => loc.Contains("jellynext-virtual", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (virtualFolders.Count == 0)
            {
                _logger.LogDebug("No virtual libraries configured to scan");
                return;
            }

            _logger.LogInformation(
                "Triggering scan for {Count} virtual libraries: {Names}",
                virtualFolders.Count,
                string.Join(", ", virtualFolders.Select(vf => vf.Name)));

            // Scan each virtual library specifically (more efficient than scanning all libraries)
            var scannedCount = 0;
            foreach (var virtualFolder in virtualFolders)
            {
                // Get the library item by path
                var libraryPath = virtualFolder.Locations.FirstOrDefault(loc =>
                    loc.Contains("jellynext-virtual", StringComparison.OrdinalIgnoreCase));

                if (string.IsNullOrEmpty(libraryPath))
                {
                    continue;
                }

                var libraryItem = _libraryManager.FindByPath(libraryPath, isFolder: true);
                if (libraryItem is MediaBrowser.Controller.Entities.Folder folder)
                {
                    _logger.LogDebug("Scanning library: {Name} at {Path}", virtualFolder.Name, libraryPath);

                    // Trigger metadata refresh for this specific library
                    var directoryService = new DirectoryService(_fileSystem);
                    var refreshOptions = new MetadataRefreshOptions(directoryService)
                    {
                        // Only scan for new/removed items, don't refresh metadata
                        ReplaceAllMetadata = false,
                        ReplaceAllImages = false
                    };

                    await folder.ValidateChildren(
                        new Progress<double>(),
                        refreshOptions,
                        recursive: true,
                        allowRemoveRoot: false,
                        cancellationToken);

                    scannedCount++;
                }
                else
                {
                    _logger.LogWarning("Could not find library folder for: {Name}", virtualFolder.Name);
                }
            }

            _logger.LogInformation(
                "Successfully triggered scan for {ScannedCount}/{TotalCount} virtual libraries",
                scannedCount,
                virtualFolders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error triggering library scan");
            // Don't throw - library scanning is best-effort
        }
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        // Default to 6 hours - users can configure this in Jellyfin Dashboard
        // Note: Startup sync is handled by StartupSyncService (not via triggers)
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
