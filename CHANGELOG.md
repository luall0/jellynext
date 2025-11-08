# Changelog

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
