using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.JellyNext.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyNext.Services;

/// <summary>
/// Service for caching synced content per user and provider.
/// </summary>
public class ContentCacheService
{
    private readonly ILogger<ContentCacheService> _logger;
    private readonly ConcurrentDictionary<string, CachedContent> _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCacheService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public ContentCacheService(ILogger<ContentCacheService> logger)
    {
        _logger = logger;
        _cache = new ConcurrentDictionary<string, CachedContent>();
    }

    /// <summary>
    /// Gets cached content for a user and provider.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="providerName">The provider name.</param>
    /// <returns>Cached content items, or empty if not cached or expired.</returns>
    public IReadOnlyList<ContentItem> GetCachedContent(Guid userId, string providerName)
    {
        var key = GetCacheKey(userId, providerName);
        if (_cache.TryGetValue(key, out var cached))
        {
            if (cached.IsExpired())
            {
                _logger.LogDebug("Cache expired for user {UserId}, provider {Provider}", userId, providerName);
                _cache.TryRemove(key, out _);
                return Array.Empty<ContentItem>();
            }

            _logger.LogDebug(
                "Cache hit for user {UserId}, provider {Provider}: {Count} items",
                userId,
                providerName,
                cached.Items.Count);
            return cached.Items;
        }

        _logger.LogDebug("Cache miss for user {UserId}, provider {Provider}", userId, providerName);
        return Array.Empty<ContentItem>();
    }

    /// <summary>
    /// Updates the cache for a user and provider.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="providerName">The provider name.</param>
    /// <param name="items">The content items to cache.</param>
    public void UpdateCache(Guid userId, string providerName, IReadOnlyList<ContentItem> items)
    {
        var key = GetCacheKey(userId, providerName);
        var cached = new CachedContent
        {
            Items = items,
            CachedAt = DateTime.UtcNow
        };

        _cache.AddOrUpdate(key, cached, (_, _) => cached);
        _logger.LogInformation(
            "Updated cache for user {UserId}, provider {Provider}: {Count} items",
            userId,
            providerName,
            items.Count);
    }

    /// <summary>
    /// Clears the cache for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    public void ClearUserCache(Guid userId)
    {
        var keysToRemove = _cache.Keys.Where(k => k.StartsWith($"{userId}:", StringComparison.Ordinal)).ToList();
        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }

        _logger.LogInformation("Cleared cache for user {UserId}", userId);
    }

    /// <summary>
    /// Clears all cached content.
    /// </summary>
    public void ClearAllCache()
    {
        _cache.Clear();
        _logger.LogInformation("Cleared all cached content");
    }

    /// <summary>
    /// Gets all cached content for a user across all providers.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>Dictionary of provider name to content items.</returns>
    public IReadOnlyDictionary<string, IReadOnlyList<ContentItem>> GetAllUserContent(Guid userId)
    {
        var result = new Dictionary<string, IReadOnlyList<ContentItem>>();
        var userPrefix = $"{userId}:";

        foreach (var kvp in _cache.Where(x => x.Key.StartsWith(userPrefix, StringComparison.Ordinal)))
        {
            if (!kvp.Value.IsExpired())
            {
                var providerName = kvp.Key.Substring(userPrefix.Length);
                result[providerName] = kvp.Value.Items;
            }
        }

        return result;
    }

    private static string GetCacheKey(Guid userId, string providerName)
    {
        return $"{userId}:{providerName}";
    }

    private class CachedContent
    {
        public IReadOnlyList<ContentItem> Items { get; set; } = Array.Empty<ContentItem>();

        public DateTime CachedAt { get; set; }

        public bool IsExpired()
        {
            var config = Plugin.Instance?.Configuration;
            var expirationHours = config?.CacheExpirationHours ?? 6;
            return DateTime.UtcNow - CachedAt > TimeSpan.FromHours(expirationHours);
        }
    }
}
