using Jellyfin.Plugin.JellyNext.Providers;
using Jellyfin.Plugin.JellyNext.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.JellyNext;

/// <summary>
/// Service registrator for JellyNext plugin services.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        // Core services
        serviceCollection.AddHttpClient();
        serviceCollection.AddSingleton<TraktApi>();
        serviceCollection.AddSingleton<TmdbService>();
        serviceCollection.AddSingleton<ContentCacheService>();

        // Content providers
        serviceCollection.AddSingleton<IContentProvider, RecommendationsProvider>();

        // Sync service (must be registered after providers)
        serviceCollection.AddSingleton<ContentSyncService>();
    }
}
