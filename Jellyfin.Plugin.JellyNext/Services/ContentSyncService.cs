using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyNext.Helpers;
using Jellyfin.Plugin.JellyNext.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyNext.Services;

/// <summary>
/// Service for syncing content from all registered providers.
/// </summary>
public class ContentSyncService
{
    private readonly ILogger<ContentSyncService> _logger;
    private readonly ContentCacheService _cacheService;
    private readonly EndedShowsCacheService _endedShowsCache;
    private readonly IEnumerable<IContentProvider> _providers;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSyncService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="cacheService">The cache service.</param>
    /// <param name="endedShowsCache">The ended shows cache service.</param>
    /// <param name="providers">The collection of content providers.</param>
    public ContentSyncService(
        ILogger<ContentSyncService> logger,
        ContentCacheService cacheService,
        EndedShowsCacheService endedShowsCache,
        IEnumerable<IContentProvider> providers)
    {
        _logger = logger;
        _cacheService = cacheService;
        _endedShowsCache = endedShowsCache;
        _providers = providers;
    }

    /// <summary>
    /// Syncs content for all users and all providers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task SyncAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting content sync for all users");

        var config = Plugin.Instance?.Configuration;
        if (config == null)
        {
            _logger.LogError("Plugin configuration not available");
            return;
        }

        var traktUsers = config.GetAllTraktUsers();
        if (traktUsers.Count == 0)
        {
            _logger.LogInformation("No Trakt users configured, skipping sync");
            return;
        }

        var syncTasks = new List<Task>();

        foreach (var traktUser in traktUsers)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            syncTasks.Add(SyncUserAsync(traktUser.LinkedMbUserId, cancellationToken));
        }

        await Task.WhenAll(syncTasks);

        // Clean up expired shows from the ended shows cache
        var removedCount = _endedShowsCache.RemoveExpiredShows();

        // Log ended shows cache statistics
        var endedShowsCount = _endedShowsCache.GetCachedCount();
        _logger.LogInformation(
            "Completed content sync for all users. Ended shows cache: {Count} shows (removed {Removed} expired)",
            endedShowsCount,
            removedCount);
    }

    /// <summary>
    /// Syncs content for a specific user across all providers.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task SyncUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting content sync for user {UserId}", userId);

        var traktUser = UserHelper.GetTraktUser(userId);
        if (traktUser == null)
        {
            _logger.LogWarning("No Trakt user found for {UserId}", userId);
            return;
        }

        foreach (var provider in _providers)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await SyncProviderAsync(userId, provider, cancellationToken);
        }

        _logger.LogInformation("Completed content sync for user {UserId}", userId);
    }

    /// <summary>
    /// Syncs content for a specific user and provider.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="provider">The content provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task SyncProviderAsync(
        Guid userId,
        IContentProvider provider,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!provider.IsEnabledForUser(userId))
            {
                _logger.LogDebug(
                    "Provider {Provider} not enabled for user {UserId}",
                    provider.ProviderName,
                    userId);
                return;
            }

            _logger.LogInformation(
                "Syncing {Provider} for user {UserId}",
                provider.ProviderName,
                userId);

            var items = await provider.FetchContentAsync(userId);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            _cacheService.UpdateCache(userId, provider.ProviderName, items);

            _logger.LogInformation(
                "Successfully synced {Count} items from {Provider} for user {UserId}",
                items.Count,
                provider.ProviderName,
                userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to sync {Provider} for user {UserId}",
                provider.ProviderName,
                userId);
        }
    }

    /// <summary>
    /// Gets all registered providers.
    /// </summary>
    /// <returns>Collection of content providers.</returns>
    public IEnumerable<IContentProvider> GetProviders()
    {
        return _providers;
    }

    /// <summary>
    /// Gets a specific provider by name.
    /// </summary>
    /// <param name="providerName">The provider name.</param>
    /// <returns>The provider, or null if not found.</returns>
    public IContentProvider? GetProvider(string providerName)
    {
        return _providers.FirstOrDefault(p =>
            p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
    }
}
