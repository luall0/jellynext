using Jellyfin.Plugin.JellyNext.Providers;
using Jellyfin.Plugin.JellyNext.Services;
using Jellyfin.Plugin.JellyNext.Services.DownloadProviders;
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
        serviceCollection.AddSingleton<ShowsCacheService>();
        serviceCollection.AddSingleton<RadarrService>();
        serviceCollection.AddSingleton<SonarrService>();
        serviceCollection.AddSingleton<JellyseerrService>();
        serviceCollection.AddSingleton<LocalLibraryService>();

        // Download providers
        serviceCollection.AddSingleton<NativeDownloadProvider>();
        serviceCollection.AddSingleton<JellyseerrDownloadProvider>();
        serviceCollection.AddSingleton<WebhookDownloadProvider>();
        serviceCollection.AddSingleton<DownloadProviderFactory>();

        // Content providers
        serviceCollection.AddSingleton<IContentProvider, RecommendationsProvider>();
        serviceCollection.AddSingleton<IContentProvider, NextSeasonsProvider>();
        serviceCollection.AddSingleton<IContentProvider, TrendingMoviesProvider>();

        // Sync service (must be registered after providers)
        serviceCollection.AddSingleton<ContentSyncService>();

        // Virtual library
        serviceCollection.AddSingleton<VirtualLibrary.VirtualLibraryManager>();
        serviceCollection.AddSingleton<VirtualLibrary.VirtualLibraryCreator>();

        // Hosted services
        serviceCollection.AddHostedService<PlaybackInterceptor>();
    }
}
