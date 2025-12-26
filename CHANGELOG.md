# Changelog

## v1.2.1.1

### Bug Fixes

- **Trending movies downloads**: Fixed download failure when playing trending movies
  - PlaybackInterceptor now correctly handles global content paths (`jellynext-virtual/global/movies_trending/`)
  - Detects global content types and uses configured `TrendingMoviesUserId` for cache lookup
  - Previously failed because path parsing expected per-user GUID, not "global" keyword

## v1.2.1.0

### Improvements

- **Modular Configuration UI**: Refactored configuration page into maintainable tab-based architecture
  - Split 1843-line monolithic `configPage.html` into modular components (317-line main shell + 4 tab files)
  - Each tab isolated: `general.html/js`, `trakt.html/js`, `trending.html/js`, `downloads.html/js` (~40-865 lines each)
  - New `ConfigController` serves tab resources from embedded resources via REST endpoints
  - Eager loading strategy ensures all tabs loaded before population
  - Proper async load order prevents race conditions (load tabs → config → users → populate UI)

### Bug Fixes

- **Configuration save errors**: Fixed JSON deserialization errors when saving with unconfigured Radarr/Sonarr
  - Changed `RadarrQualityProfileId` and `SonarrQualityProfileId` from `int` to `int?` (nullable)
  - Added validation in `RadarrService` and `SonarrService` before using profile IDs
  - Fixed JavaScript to properly handle empty select values (avoid `parseInt("")` → `NaN`)
- **Race condition in trending settings**: Fixed issue where user selection was lost on page reload
  - `loadUsers()` now returns promise to ensure dropdown is populated before setting selected value
  - Prevents accidental overwrite of `TrendingMoviesUserId` with null

### Technical

- Updated CLAUDE.md with configuration page architecture patterns and load order requirements

## v1.2.0.0

### Features

- **Download Integration Modes**: Complete overhaul of download system with three pluggable integration modes
  - **Native Mode** (default): Direct Radarr/Sonarr API integration for granular control
  - **Jellyseerr Mode**: Centralized request management with approval workflows and multi-user tracking
  - **Webhook Mode**: Custom HTTP webhooks for complete flexibility and external system integration
  - Factory pattern (`DownloadProviderFactory`) selects provider based on configuration
  - All modes support anime detection and per-season TV downloads

- **Jellyseerr Integration**: Full integration with Jellyseerr request management system
  - **Automatic user import**: Jellyfin users auto-imported to Jellyseerr on first request with REQUEST-only permissions
  - **Per-user attribution**: All requests tracked per-user via `X-Api-User` header
  - **Approval workflows**: Respects Jellyseerr approval settings and request quotas
  - **Configuration modes**:
    - Default mode: Uses Jellyseerr's default Radarr/Sonarr server and profile settings
    - Manual mode: Explicit server and profile selection from UI dropdowns
  - **Server/profile selection**: UI dynamically loads Radarr/Sonarr servers and quality profiles from Jellyseerr
  - **Anime support**: Optional separate anime profile for TV shows detected via Trakt genres
  - **GUID normalization**: Handles UUID format differences between Jellyseerr (no hyphens) and Jellyfin (standard format)
  - **Connection testing**: Test Jellyseerr connection and validate API key from plugin settings
  - New service: `JellyseerrService` for API communication
  - New controller: `JellyseerrController` for UI configuration
  - New models: `MediaRequest`, `JellyseerrUser`, `RadarrServer`, `SonarrServer`, `QualityProfile`

- **Webhook Integration**: Custom HTTP webhook support for external integrations
  - **Flexible HTTP methods**: GET, POST, PUT, PATCH support
  - **Dynamic placeholders**: Replace variables in URLs, headers, and payloads
    - Movies: `{tmdbId}`, `{imdbId}`, `{title}`, `{year}`, `{jellyfinUserId}`
    - TV Shows: All movie placeholders + `{tvdbId}`, `{seasonNumber}`, `{isAnime}`
  - **Custom headers**: Add authentication, API keys, or any custom headers with placeholder support
  - **JSON payload templates**: Fully customizable request body with default templates included
  - **Separate configurations**: Independent settings for movies vs TV shows
  - **Use cases**: Discord/Slack notifications, custom download systems, third-party automation
  - New service: `WebhookDownloadProvider` implementing `IDownloadProvider`
  - New configuration fields: `WebhookMovieUrl`, `WebhookShowUrl`, `WebhookMethod`, `WebhookMovieHeaders`, `WebhookShowHeaders`, `WebhookMoviePayload`, `WebhookShowPayload`
  - New model: `WebhookHeader` for custom header configuration
  - Comprehensive documentation: New `WEBHOOK.md` guide with examples and troubleshooting

### Improvements

- **Download Provider Architecture**: Pluggable provider system for extensibility
  - `IDownloadProvider` interface defines contract for all download providers
  - `NativeDownloadProvider`: Implements direct Radarr/Sonarr integration
  - `JellyseerrDownloadProvider`: Implements Jellyseerr API integration
  - `WebhookDownloadProvider`: Implements custom HTTP webhook integration
  - `DownloadProviderFactory`: Factory pattern selects provider based on `DownloadIntegration` enum
  - `PlaybackInterceptor` uses factory to route download requests

- **Configuration UI Enhancements**: Redesigned download integration section
  - **Visual integration selector**: Card-based UI with icons and descriptions for each mode
  - **Dynamic sections**: UI shows/hides relevant settings based on selected integration mode
  - **Webhook UI**: Interactive payload editors with placeholder insertion buttons
  - **Header management**: Add/remove custom headers dynamically for webhook requests
  - **Payload reset**: Quick reset to default payload templates
  - **Real-time validation**: Client-side validation for URLs, headers, and JSON payloads

- **Documentation**: Comprehensive guides for all integration modes
  - Updated README.md with three integration modes in setup guide
  - New WEBHOOK.md with complete webhook integration guide including:
    - Configuration examples (GET, POST, authentication, Discord/Slack)
    - Building custom webhook endpoints (Python Flask, Node.js Express)
    - Troubleshooting guide
    - Security considerations
    - Advanced usage patterns
  - Updated CLAUDE.md with download provider architecture
  - FAQ updates covering all three integration modes

### Technical Changes

- **New enum**: `DownloadIntegrationType` with values: `Native = 0`, `Jellyseerr = 1`, `Webhook = 2`
- **Configuration migration**: Changed from `UseJellyseerr` boolean to `DownloadIntegration` enum (backward compatible)
- **Service registration**: All download providers registered in `PluginServiceRegistrator`
- **API Controllers**:
  - `JellyseerrController`: Connection testing, server/profile retrieval
  - Enhanced error handling and logging across all providers
- **Models**:
  - `MediaRequest`: Jellyseerr request model with media IDs and user tracking
  - `JellyseerrUser`: User model for auto-import functionality
  - `RadarrServer` / `SonarrServer`: Server configuration models
  - `WebhookHeader`: Custom header model for webhook requests
- **HTTP Client**: All providers use `NamedClient.Default` for Cloudflare bypass compatibility

## v1.1.2.0

### Improvements

- **Configuration UI Redesign**: Complete overhaul of plugin settings interface with native Jellyfin tab styling
  - **Tab-based layout**: Organized settings into 4 tabs (General, Trakt, Trending, Downloads)
  - **Native Jellyfin styling**: Uses `controlgroup` and `localnav` classes matching Jellyfin's UI patterns
  - **Unified save button**: Single save button now handles all settings including per-user Trakt configurations
  - **Improved UX**: Removed redundant "Save User Settings" button, cleaner tab navigation

- **Virtual Library Management**: Enhanced global content directory handling
  - **Automatic cleanup**: Trending movies directory now automatically flushed when feature is disabled
  - **Consistent state**: Prevents stale content from appearing in global libraries after configuration changes

## v1.1.1.0

### Improvements

- **Shows Cache Refactoring**: Complete overhaul of season-level caching system
  - **Hybrid architecture**: Global show/season metadata cache + per-user watch progress tracking
  - **Incremental sync**: History-based delta syncing via `/sync/history/shows` endpoint reduces API load
  - **Smart caching**: Ended shows cache all seasons immediately, ongoing shows only cache complete seasons
  - **Automatic sync mode**: `PerformIncrementalSync()` automatically detects first run and falls back to full sync
  - **In-memory timestamps**: Last sync timestamp no longer persisted to config (triggers full sync on restart for data freshness)
  - **Zero duplicate API calls**: Both RecommendationsProvider and NextSeasonsProvider read from same cache
  - **Progressive discovery**: As users watch episodes, incremental sync detects progression and triggers next season recommendations

- **Next Seasons Provider Enhancement**: Improved reliability and efficiency
  - **Sync-first approach**: Calls `ShowsCacheService.PerformIncrementalSync()` before fetching content
  - **Cache-only reads**: Retrieves watched progress + season metadata entirely from cache (no duplicate Trakt API calls)
  - **Dynamic fetching**: If next season not in cache for ongoing shows, fetches latest from Trakt API and checks season count
  - **Better library deduplication**: Uses LocalLibraryService to exclude shows already in Jellyfin library

- **Recommendations Provider Optimization**: Uses ShowsCacheService for season counts to avoid duplicate API calls

- **Configuration Simplification**: Removed `EndedShowsCacheExpirationDays` setting (no longer needed with new cache architecture)

### Technical Changes

- **New models**:
  - `ShowCacheEntry`: Global show/season metadata (Title, Year, IDs, Status, Genres, Seasons dictionary)
  - `SeasonMetadata`: Season-level data (SeasonNumber, EpisodeCount, AiredEpisodes, FirstAired, IsComplete property)
  - `TraktHistoryItem`: For parsing `/sync/history/shows` endpoint
  - `TraktEpisode`: Episode metadata for history items

- **Deleted files**:
  - `EndedShowsCacheService.cs` (replaced by `ShowsCacheService.cs`)
  - `EndedShowMetadata.cs` (replaced by `ShowCacheEntry.cs` + `SeasonMetadata.cs`)

- **API additions**:
  - `TraktApi.GetShowWatchHistory()`: Fetches watch history with automatic pagination support (100 items/page)
  - `TraktApi.GetWatchedShows()`: Enhanced with `extended=full` parameter for genre data

- **Service registration**: Updated `PluginServiceRegistrator` to use `ShowsCacheService` instead of `EndedShowsCacheService`

## v1.1.0.3

### Bug Fixes

- **Jellyfin 10.11.0 Compatibility**: Pin SDK to exact version 10.11.0 to ensure compatibility across all 10.11.x releases
  - Changed `Jellyfin.Controller` and `Jellyfin.Model` dependencies from `10.11.*` to `10.11.0`
  - Fixes `ReflectionTypeLoadException` on Jellyfin servers running 10.11.0 and 10.11.1
  - Plugin now works on Jellyfin 10.11.0+

## v1.1.0.2

### Documentation

- **Enhanced Setup Instructions**: Added detailed virtual library path discovery instructions
  - Included example log output showing exact paths for each content type
  - Clarified Docker path usage (`/config/data/plugins/Jellyfin.Plugin.JellyNext/jellynext-virtual/...`)
  - Added step-by-step guide for finding jellynext-virtual directory via Jellyfin logs after Trakt user configuration
  - Improved user experience for first-time setup

## v1.1.0.1

### Bug Fixes

- **Configuration Save Error**: Fix `System.FormatException` when saving configuration with trending movies disabled
  - `TrendingMoviesUserId` is now only included in configuration POST when trending is enabled and a valid user is selected
  - Prevents empty string from being parsed as GUID when trending movies is not configured

## v1.1.0.0

### Features

- **Trending Movies (Global)**: Added global trending movies feature visible to all users
  - New global content type: `MoviesTrending`
  - Non-personalized trending movies from Trakt
  - Configurable via Dashboard → Plugins → JellyNext → Trending Movies (Global)
  - Settings:
    - Enable/disable toggle
    - Source user selection (which Trakt account to use for API access)
    - Limit: 1-100 movies (default: 50)
  - Virtual library path: `jellynext-virtual/global/movies_trending`
  - Directory automatically created on plugin startup when enabled
  - Supports same one-click Radarr download functionality as per-user recommendations

### Improvements

- **Global Content Architecture**: Extended virtual library system to support both per-user and global content types
  - New helper method: `VirtualLibraryContentTypeHelper.IsGlobal()` to distinguish content types
  - `VirtualLibraryManager` now handles both per-user (`jellynext-virtual/[userId]/[content-type]/`) and global (`jellynext-virtual/global/[content-type]/`) paths
  - Automatic directory initialization for global content types
  - Setup instructions now include global libraries when enabled
- **Trakt API**: Added `GetTrendingMovies()` method to fetch trending movies with configurable limits
- **New Provider**: `TrendingMoviesProvider` implements `IContentProvider` for modular trending movies support
- **Documentation**: Updated CLAUDE.md and README.md with global content architecture and trending movies feature

## v1.0.3.0

### Features

- **Per-User Recommendation Limits**: Added configurable limits for movie and show recommendations (1-100, default: 50)
  - New settings: `MovieRecommendationsLimit` and `ShowRecommendationsLimit` in per-user configuration
  - Configurable via Dashboard → Plugins → JellyNext → User Settings
  - Validated on both client and server with `Math.Clamp()` to enforce 1-100 range
  - Each user can control how many recommendations they want to fetch

## v1.0.2.0

### Features

- **Short Dummy Video Option**: Added configurable 2-second dummy video for automatic playback stop on all clients
  - New setting: `UseShortDummyVideo` (default: enabled)
  - When enabled: Uses 2-second video that auto-stops playback even on clients without API support
  - When disabled: Uses 1-hour video (prevents "watched" status but requires manual stop)
  - Configurable via Dashboard → Plugins → JellyNext → Playback Settings
  - New embedded resource: `dummy_short.mp4` (~5KB vs ~2MB for long version)

### Improvements

- **Automatic Stub Refresh on Config Change**: Virtual library stub files now automatically rebuild when dummy video setting is changed
  - Validates stub file content matches current configuration on each sync
  - Flushes and rebuilds directory if mismatch detected
  - Ensures consistent experience across all virtual items
- **Better Client Compatibility**: Short dummy video provides automatic stop on clients that don't support Jellyfin's playback control API

## v1.0.1.0

### Features

- **Configurable Playback Stop Delay**: Added setting to configure delay before stopping playback of virtual items (default: 2 seconds, range: 0-30)
  - Allows users to adjust timing for clients that need more initialization time
  - Configurable via Dashboard → Plugins → JellyNext → Playback Settings

### Improvements

- **Reduced Default Playback Delay**: Changed default playback stop delay from 5 seconds to 2 seconds for faster user experience
- **Enhanced Documentation**: Added comprehensive "Playback Stop Behavior" section to README explaining:
  - How automatic playback stop works
  - Client compatibility information
  - Instructions for clients that don't support automatic stop
  - Clarification that download triggers immediately regardless of stop behavior

## v1.0.0.1

### Bug Fixes

- **Sonarr Integration**: Fix series monitoring update failure caused by missing `path` field in API requests

## v1.0.0.0

### Features

- **Per-User Trakt Integration**: OAuth 2.0 device flow authentication with automatic token refresh
- **Virtual Libraries**: Three dedicated libraries per user (Movie Recommendations, Show Recommendations, Next Seasons)
- **One-Click Downloads**: Automatic Radarr/Sonarr integration triggered by playback attempts
- **Per-Season TV Downloads**: Granular control to download specific seasons only
- **Anime Detection**: Automatic routing to separate Sonarr anime folder based on Trakt genres
- **Smart Caching**: Configurable content cache (6hr default) and ended shows cache (7 day default)
- **Per-User Settings**: Granular sync control (movies, shows, next seasons), content filtering (collected, watchlisted), performance options (season 1 limit)
- **Automatic Sync**: Background sync task (6hr interval) with startup sync (5s after start)
- **iOS/tvOS Compatibility**: FFprobe-compatible dummy video files prevent client crashes
- **Native Jellyfin Integration**: Standard .strm file naming with TMDB/TVDB metadata providers
- **Configuration UI**: Web-based admin interface for Trakt/Radarr/Sonarr setup and user management
