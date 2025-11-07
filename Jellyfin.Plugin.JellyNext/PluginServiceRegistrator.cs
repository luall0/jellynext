using Jellyfin.Plugin.JellyNext.Providers;
using Jellyfin.Plugin.JellyNext.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Resolvers;
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
        serviceCollection.AddSingleton<ContentCacheService>();
        serviceCollection.AddSingleton<RadarrService>();
        serviceCollection.AddSingleton<SonarrService>();
        serviceCollection.AddSingleton<LocalLibraryService>();

        // Content providers
        serviceCollection.AddSingleton<IContentProvider, RecommendationsProvider>();
        serviceCollection.AddSingleton<IContentProvider, NextSeasonsProvider>();

        // Sync service (must be registered after providers)
        serviceCollection.AddSingleton<ContentSyncService>();

        // Virtual library
        serviceCollection.AddSingleton<VirtualLibrary.VirtualLibraryManager>();
        serviceCollection.AddSingleton<VirtualLibrary.VirtualLibraryCreator>();

        // Hosted services
        serviceCollection.AddHostedService<PlaybackInterceptor>();
        serviceCollection.AddHostedService<StartupSyncService>();
    }
}
