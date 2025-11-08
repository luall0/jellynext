<div align="center">
  <img src="images/jellynext_logo_full.png" alt="JellyNext Logo" width="600"/>

  <h3>Trakt-Powered Discovery for Jellyfin</h3>

  <p>
    <a href="#features">Features</a> •
    <a href="#installation">Installation</a> •
    <a href="#setup">Setup</a> •
    <a href="#usage">Usage</a> •
    <a href="#development">Development</a>
  </p>
</div>

---

## Overview

JellyNext brings Trakt-powered content discovery directly into your Jellyfin library. Each user can link their own Trakt account to get personalized recommendations and next season notifications through dedicated virtual libraries, with automatic one-click downloads via Radarr/Sonarr integration.

### Compatibility with Official Trakt Plugin

JellyNext is designed to work alongside the [official Jellyfin Trakt plugin](https://github.com/jellyfin/jellyfin-plugin-trakt). However, you **must exclude JellyNext virtual libraries** from the official Trakt plugin to prevent unwanted scrobbling and updates to your Trakt account.

**Important Configuration:**
- In the official Trakt plugin settings, exclude these libraries:
  - `Trakt Movie Recommendations`
  - `Trakt Show Recommendations`
  - `Trakt Next Seasons`
  - `Trending Movies` (if enabled)
- This prevents playback attempts on virtual items from being marked as "watched" on Trakt
- Your real media libraries can still sync normally with the official plugin

## Features

### 🎯 Per-User Trakt Integration
- **OAuth 2.0 Device Flow**: Each Jellyfin user links their own Trakt account securely
- **Automatic Token Refresh**: Tokens refresh automatically before expiration (75% safety buffer)
- **Per-User Settings**: Granular control over what content to sync (movie recommendations, show recommendations, next seasons)
- **Privacy-Focused**: Each user's recommendations are based on their own Trakt watch history

### 📚 Virtual Libraries
**Per-User Libraries:**
- **Trakt Movie Recommendations**: Personalized movie suggestions based on your Trakt history
- **Trakt Show Recommendations**: TV show suggestions (configurable to show only season 1 or all seasons)
- **Trakt Next Seasons**: Smart notifications for the immediate next unwatched season of shows you're following

**Global Libraries (shared across all users):**
- **Trending Movies**: Current trending movies on Trakt (non-personalized)

Features:
- Automatic sync on configurable interval (default: 6 hours)
- Smart filtering: exclude already collected items, optionally exclude watchlisted items
- Configurable limits: 1-100 items per recommendation type
- Ended/canceled shows cache (reduces API calls for shows that won't get new seasons)
- iOS/tvOS compatible (uses FFprobe-compatible dummy video files)

### ⬇️ Intelligent Download System
- **One-Click Downloads**: Click "Play" on any virtual library item to trigger download via Radarr/Sonarr
- **Automatic Detection**: Plugin detects playback attempts on virtual items and routes to appropriate service
- **Per-Season TV Downloads**: Downloads only the specific season you want, not the entire series
- **Anime Support**: Automatically detects anime (via Trakt genres) and routes to separate Sonarr anime folder
- **Playback Prevention**: Clears playback state to prevent marking virtual items as "watched"
- **Works Everywhere**: All Jellyfin clients (Web, iOS, Android, TV apps)

### 🎨 Native Jellyfin Integration
- **Standard Metadata**: Uses Jellyfin's built-in TMDB/TVDB metadata providers (no separate API key needed)
- **Native Resolution**: Virtual libraries use standard .strm file naming conventions (`[tmdbid-X]`, `[tvdbid-X]`)
- **Seamless UI**: Virtual content appears alongside your real library with full metadata and artwork

## Installation

### Prerequisites
- **Jellyfin 10.11.0 or higher** (required for API compatibility)
- **.NET 9.0 Runtime** (usually included with Jellyfin)
- **Trakt Account** (free at [trakt.tv](https://trakt.tv))
- **(Optional) Radarr/Sonarr** for automatic download functionality

### Install from Repository

1. **Add Plugin Repository to Jellyfin**
   - Go to: **Dashboard → Plugins → Repositories**
   - Click **"+"** to add a new repository
   - Enter repository URL: `https://raw.githubusercontent.com/luall0/jellyfin-luall0-plugins/refs/heads/main/manifest.json`
   - Click **Save**

2. **Install JellyNext Plugin**
   - Go to: **Dashboard → Plugins → Catalog**
   - Find **"JellyNext"** in the list
   - Click **Install**
   - Restart Jellyfin when prompted

3. **Verify Installation**
   - After restart, go to: **Dashboard → Plugins**
   - **JellyNext** should appear in your installed plugins list

### Manual Installation

1. **Download the Plugin**
   - Download the latest `jellynext-vX.X.X.zip` from [Releases](../../releases)

2. **Extract to Jellyfin Plugins Directory**
   - **Linux**: `/var/lib/jellyfin/plugins/JellyNext_vX.X.X/`
   - **Windows**: `%AppData%\Jellyfin\Server\plugins\JellyNext_vX.X.X\`
   - **Docker**: `/config/plugins/JellyNext_vX.X.X/`

   Note: The version folder name (e.g., `JellyNext_v1.0.0`) is important for Jellyfin's plugin system.

3. **Restart Jellyfin**
   - Restart your Jellyfin server completely
   - Verify installation: **Dashboard → Plugins → JellyNext** should appear

## Setup

### Step 1: Link Trakt Account (Per-User)

Each Jellyfin user must link their own Trakt account for personalized recommendations.

Navigate to: **Dashboard → Plugins → JellyNext**

#### Authorization Process

1. **Start Authorization**
   - Select a Jellyfin user from the dropdown
   - Click **"Authorize Trakt"** button

2. **Device Code Appears**
   - A code like `ABC12345` will be displayed
   - An activation URL will also be shown: [trakt.tv/activate](https://trakt.tv/activate)

3. **Activate on Trakt**
   - On any device, visit: [trakt.tv/activate](https://trakt.tv/activate)
   - Log into your Trakt account (or create one)
   - Enter the device code shown in Jellyfin
   - Authorize the application

4. **Verification**
   - The Jellyfin page will automatically poll for authorization
   - Once successful, status changes to: **🟢 Authorized**
   - You can now configure per-user sync settings

#### Per-User Sync Settings

After authorization, configure what to sync for each user:

1. **Select authorized user** from dropdown
2. Click **"User Settings"** to expand options

**Content Sync Options:**
- ☑️ **Sync Movie Recommendations**: Enable Trakt Movie Recommendations library
- ☑️ **Sync Show Recommendations**: Enable Trakt Show Recommendations library
- ☑️ **Sync Next Seasons**: Enable Trakt Next Seasons library

**Recommendation Limits:**
- **Movie Recommendations Limit**: Number of movie recommendations to fetch (1-100, default: 50)
- **Show Recommendations Limit**: Number of show recommendations to fetch (1-100, default: 50)

**Filtering Options:**
- ☑️ **Ignore Collected Items**: Exclude movies/shows already in your Trakt collection (recommended)
- ☐ **Ignore Watchlisted Items**: Exclude items on your Trakt watchlist (if you don't want to download them yet)

**Performance Options:**
- ☑️ **Limit Shows to Season 1**: Only create stubs for season 1 of recommended shows (faster Jellyfin library scans, recommended)

**Debugging:**
- ☐ **Extra Logging**: Enable verbose logging for this user (for troubleshooting)

Click **Save** after making changes.

### Step 2: Add Virtual Libraries to Jellyfin

For each user who will use JellyNext, create three Jellyfin libraries pointing to the virtual folders.

**Go to**: Dashboard → Libraries → Add Media Library

**Create three libraries** with these exact settings:

#### Library 1: Trakt Movie Recommendations
- Content type: `Movies`
- Display name: `Trakt Movie Recommendations` (or your choice)
- Folder: `/path/to/jellyfin/data/jellynext-virtual/[user-id]/movies-recommendations/`
- Metadata language: Your preference
- Country: Your preference

#### Library 2: Trakt Show Recommendations
- Content type: `Shows`
- Display name: `Trakt Show Recommendations` (or your choice)
- Folder: `/path/to/jellyfin/data/jellynext-virtual/[user-id]/shows-recommendations/`
- Metadata language: Your preference
- Country: Your preference

#### Library 3: Trakt Next Seasons
- Content type: `Shows`
- Display name: `Trakt Next Seasons` (or your choice)
- Folder: `/path/to/jellyfin/data/jellynext-virtual/[user-id]/next-seasons/`
- Metadata language: Your preference
- Country: Your preference

**Finding your user ID:**
- Dashboard → Users → Select user → URL shows user ID (e.g., `a1b2c3d4e5f6...`)
- Or check filesystem after first sync: `/jellyfin/data/jellynext-virtual/` will contain user folders

**Important Notes:**
- Use the full absolute path to your Jellyfin data directory
- Docker users: Use the container's internal path (e.g., `/config/data/jellynext-virtual/...`)
- Library names can be customized, but folders must match exactly
- Set library permissions so the user can access only their own virtual libraries

### Step 3: Initial Sync

After linking Trakt and adding virtual libraries:

1. **Wait for automatic sync** (happens 5 seconds after Jellyfin starts, then every 6 hours)

   OR

2. **Manually trigger sync**:
   - Go to: **Dashboard → Scheduled Tasks**
   - Find: **"JellyNext Content Sync"**
   - Click **Play button** (▶️) to run immediately

3. **Monitor Progress**:
   - Check task logs for sync status
   - Virtual library folders will be populated with `.strm` files
   - Jellyfin will automatically scan and add metadata

4. **Verify Libraries**:
   - Your virtual libraries should now show content
   - Check that posters and metadata loaded correctly

### Step 4: Configure Radarr/Sonarr (Optional)

If you want to enable automatic downloads when clicking "Play" on virtual library items, configure your *Arr instances.

Navigate to: **Dashboard → Plugins → JellyNext → Settings**

#### Radarr Configuration
1. **Radarr URL**: Your Radarr instance (e.g., `http://localhost:7878` or `http://radarr:7878` for Docker)
2. **Radarr API Key**: Found in Radarr → Settings → General → API Key
3. Click **Test Radarr Connection** to verify
4. Select **Quality Profile** from dropdown (e.g., "HD-1080p")
5. Select **Root Folder** from dropdown (e.g., "/movies")

#### Sonarr Configuration
1. **Sonarr URL**: Your Sonarr instance (e.g., `http://localhost:8989` or `http://sonarr:8989` for Docker)
2. **Sonarr API Key**: Found in Sonarr → Settings → General → API Key
3. Click **Test Sonarr Connection** to verify
4. Select **Quality Profile** from dropdown (e.g., "HD-1080p")
5. Select **Root Folder** from dropdown (e.g., "/tv")
6. **(Optional) Anime Root Folder**: Separate folder for anime if you use one

#### Cache Settings (Optional)
- **Cache Expiration (hours)**: How long to cache recommendations before refreshing (default: 6 hours)
- **Ended Shows Cache (days)**: How long to cache ended/canceled shows to reduce API calls (default: 7 days, range: 1-365)

#### Playback Settings (Optional)
- **Use Short Dummy Video**: Use 2-second dummy video for auto-stop on all clients (default: enabled)
  - When enabled: Playback stops automatically after 2 seconds even on clients without API support
  - When disabled: Uses 1-hour dummy video (prevents "watched" status but requires manual stop)
- **Playback Stop Delay (seconds)**: Delay before API stop command (default: 2 seconds, range: 0-30)
  - Increase if your client needs more time before playback can be stopped
  - Set to 0 for immediate stop (may not work on all clients)

Click **Save** when done.

## Usage

### Understanding Virtual Libraries

After setup, you'll see three new libraries per user:

1. **Trakt Movie Recommendations**
   - Personalized movie suggestions based on your Trakt watch history
   - Updates every sync interval (default: 6 hours)
   - Shows only movies you haven't collected (if enabled)

2. **Trakt Show Recommendations**
   - TV show suggestions from Trakt
   - By default shows only Season 1 for better performance
   - Can be configured to show all seasons (10 seasons max)

3. **Trakt Next Seasons**
   - Smart notifications for shows you're actively watching
   - Shows only the immediate next unwatched season
   - Automatically updates as you progress through series
   - Uses smart caching to reduce API calls for ended/canceled shows

### Downloading Content

**How It Works:**

When you click "Play" on any virtual library item, the plugin automatically:

1. **Detects the playback attempt** on a virtual item
2. **Identifies the content** (movie vs TV show, season number, IDs)
3. **Routes to the correct service**:
   - Movies → Radarr
   - TV Shows → Sonarr
   - Anime (detected via Trakt genres) → Sonarr anime folder (if configured)
4. **Adds to download queue** with your configured quality profile
5. **Prevents "watched" marking** by clearing playback state

**For TV Shows:**
- Only the specific season shown in the virtual library is downloaded
- Season is set to "monitored" in Sonarr
- Series is added but other seasons remain unmonitored
- Useful for "try before you download everything" approach

**After Download:**
- Once Radarr/Sonarr downloads and imports the media
- The real file appears in your main library
- You can play it normally from your main library
- The virtual library item remains (in case you want to download again)

### What Happens on Playback

```
User clicks Play → PlaybackInterceptor detects virtual path
                 ↓
         Extracts IDs (TMDB/TVDB) and season info
                 ↓
         Looks up cached metadata
                 ↓
         Calls RadarrService or SonarrService
                 ↓
         Adds to download queue
                 ↓
         Sends notification to user
                 ↓
         Waits 2 seconds (configurable)
                 ↓
         Stops playback & clears "watched" status
```

This all happens in seconds, mostly invisible to the user.

### Playback Stop Behavior

When you click "Play" on a virtual library item, JellyNext triggers the download and then automatically stops playback. Here's what you need to know:

**What You'll See:**
1. **Notification**: A message appears confirming the download has been added to your queue
2. **Playback Stops**: After a brief delay (default: 2 seconds), playback stops automatically

**Client Compatibility:**
- **Automatic stop** uses Jellyfin's native playback control API
- **Most clients support this**: Jellyfin Web, Android, iOS, many TV apps
- **Some clients may not respond** to the automatic stop command (this is a client limitation, not a plugin issue)

**If Playback Doesn't Stop Automatically:**
- **Simply stop it manually** by clicking the stop/back button on your client
- This is normal for clients that don't fully support Jellyfin's playback control API
- The download has already been triggered - stopping playback just prevents the dummy video from playing

**Why the Delay?**
- Some clients need time to initialize playback before they can receive a stop command
- Default delay: 2 seconds (configurable: 0-30 seconds)
- Adjust in: **Dashboard → Plugins → JellyNext → Playback Settings → Playback Stop Delay**

**Client Support:**
If your client doesn't support automatic playback stop, this is a Jellyfin native feature limitation. You can:
- Ask the client developer when automatic playback control will be supported
- Continue using the plugin - just manually stop playback after the notification appears
- The download functionality works perfectly regardless of automatic stop support

**Important:** The download is triggered immediately when you click "Play" - the playback stop is just to prevent the dummy video from playing. Even if playback doesn't stop automatically, the download has already been added to Radarr/Sonarr.

## Development

### Building from Source

```bash
# Clone the repository
git clone https://github.com/luall0/jellynext.git
cd jellynext

# Restore dependencies
dotnet restore Jellyfin.Plugin.JellyNext/Jellyfin.Plugin.JellyNext.csproj

# Build the plugin (Debug)
dotnet build Jellyfin.Plugin.JellyNext/Jellyfin.Plugin.JellyNext.csproj

# Build for release
dotnet build -c Release Jellyfin.Plugin.JellyNext/Jellyfin.Plugin.JellyNext.csproj

# Output will be in: Jellyfin.Plugin.JellyNext/bin/Release/net9.0/
```

### Project Structure

```
Jellyfin.Plugin.JellyNext/
├── Api/                          # REST API Controllers
│   ├── TraktController.cs        # OAuth flow, user management, settings
│   ├── RadarrController.cs       # Radarr connection testing, profiles, downloads
│   ├── SonarrController.cs       # Sonarr connection testing, profiles, downloads
│   └── JellyNextLibraryController.cs  # Query cached content
├── Configuration/                # Plugin settings
│   ├── PluginConfiguration.cs   # Settings model (persisted)
│   └── configPage.html           # Admin web UI
├── Helpers/                      # Utility classes
│   └── UserHelper.cs             # User configuration lookups
├── Models/                       # Data models organized by service
│   ├── Common/                   # ContentItem, ContentType
│   ├── Trakt/                    # TraktUser, TraktMovie, TraktShow, OAuth models
│   ├── Radarr/                   # Movie, QualityProfile, RootFolder
│   └── Sonarr/                   # Series, Season, QualityProfile
├── Providers/                    # Pluggable content sources
│   ├── IContentProvider.cs       # Provider interface
│   ├── RecommendationsProvider.cs  # Trakt recommendations
│   └── NextSeasonsProvider.cs    # Next season notifications
├── Resources/                    # Embedded resources
│   ├── dummy.mp4                 # 1-hour FFprobe-compatible video (prevents "watched")
│   └── dummy_short.mp4           # 2-second video (auto-stops playback)
├── ScheduledTasks/               # Background tasks
│   └── ContentSyncScheduledTask.cs  # Periodic sync (6hr default)
├── Services/                     # Business logic
│   ├── TraktApi.cs               # Trakt API client (OAuth, recommendations)
│   ├── ContentSyncService.cs     # Orchestrates sync across users/providers
│   ├── ContentCacheService.cs    # In-memory content cache (6hr expiration)
│   ├── EndedShowsCacheService.cs # Cross-user cache for ended shows (7 day default)
│   ├── LocalLibraryService.cs    # Jellyfin library queries
│   ├── PlaybackInterceptor.cs    # Detects virtual playback, triggers downloads
│   ├── RadarrService.cs          # Radarr API client
│   └── SonarrService.cs          # Sonarr API client (with anime detection)
├── VirtualLibrary/               # Virtual library system
│   ├── VirtualLibraryManager.cs  # Stub file creation/management
│   ├── VirtualLibraryCreator.cs  # Initialization
│   ├── VirtualLibraryContentType.cs  # Enum of content types
│   └── VirtualLibraryContentTypeHelper.cs  # Type mappings
├── Plugin.cs                     # Plugin entry point (singleton)
└── PluginServiceRegistrator.cs  # Dependency injection setup
```

### Architecture Overview

**Key Design Patterns:**

1. **Provider System**: Pluggable content sources via `IContentProvider` interface
   - Easy to add new recommendation sources
   - Automatic integration with sync/caching
   - Error isolation per provider

2. **Per-User Architecture**: Each user has isolated configuration
   - Own Trakt OAuth tokens
   - Own sync settings (what to sync, filters)
   - Own virtual library folders

3. **Smart Caching**: Multiple cache layers
   - Content cache: 6hr expiration (recommendations, next seasons)
   - Ended shows cache: 7 day expiration (shows that won't get new seasons)
   - Reduces API calls while keeping data fresh

4. **Playback Interception**: Event-driven download triggers
   - Listens for PlaybackStart events
   - Detects virtual library paths via regex
   - Routes to appropriate download service
   - Clears playback state to prevent "watched" marking

### Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Push to the branch (`git push origin feature/amazing-feature`)
4. Open a Pull Request

### Technical Notes

**Critical Implementation Details:**

- **HTTP Client**: ALWAYS use `NamedClient.Default` for Trakt API (avoids Cloudflare blocks)
- **Trakt Headers**: Only use `trakt-api-version: 2` and `trakt-api-key` headers (no User-Agent/Accept)
- **Extended Metadata**: Use `extended=full` on Trakt endpoints to get genre data for anime detection
- **Token Refresh**: Trakt rotates refresh tokens on each refresh - always save new tokens
- **TV Downloads**: Per-season monitoring (series monitored, but only specific season enabled)
- **iOS/tvOS Fix**: Two dummy videos - short (2sec auto-stop) or long (1hr prevents "watched")
- **Config Validation**: Stub files auto-rebuild when dummy video setting changes
- **Jellyfin 10.11**: UserDataManager requires `User` entity (not Guid), use `IUserManager.GetUserById()`
- **Framework**: .NET 9.0 required

## Troubleshooting

### Installation Issues

**"Plugin not appearing after restart"**
- Verify folder name includes version: `JellyNext_v1.0.0` (not just `JellyNext`)
- Check Jellyfin logs for plugin load errors: **Dashboard → Logs**
- Ensure .NET 9.0 is installed (included with Jellyfin 10.11+)

**"Libraries showing as empty"**
- Verify you created the Jellyfin libraries pointing to the virtual folders
- Check folder paths match exactly: `/jellyfin/data/jellynext-virtual/[user-id]/[content-type]/`
- Confirm user has completed Trakt authorization
- Manually trigger sync: **Dashboard → Scheduled Tasks → JellyNext Content Sync**

### OAuth/Authorization Issues

**"Authorization stuck on 'Waiting...'"**
- Make sure you entered the code at [trakt.tv/activate](https://trakt.tv/activate) (not trakt.tv homepage)
- Verify you're logged into Trakt before entering the code
- Check Jellyfin logs for Trakt API errors
- Device codes expire after ~15 minutes - try authorizing again with a new code

**"Token refresh failing"**
- Check Jellyfin logs for specific error messages
- Verify system clock is accurate (OAuth tokens are time-sensitive)
- Try unlinking and re-linking Trakt account: **Dashboard → Plugins → JellyNext → Unlink User**

**"Cloudflare blocking requests"**
- This is fixed in the plugin by using `NamedClient.Default`
- If you're still seeing errors, ensure you're on the latest plugin version

### Content/Sync Issues

**"No recommendations appearing"**
- Verify user has watch history on Trakt
- Check per-user sync settings: **Dashboard → Plugins → JellyNext → Select User → User Settings**
- Ensure "Ignore Collected" isn't hiding all content (if all recommendations are already in your library)
- Check "Ended Shows Cache" - expired shows won't appear in Next Seasons
- View sync task logs: **Dashboard → Scheduled Tasks → JellyNext Content Sync → Last run**

**"Virtual libraries not updating"**
- Check cache expiration setting (default: 6 hours)
- Manually trigger sync to force update
- Clear ended shows cache by changing expiration days and re-syncing

**"Too many API calls to Trakt"**
- Increase "Cache Expiration (hours)" to reduce sync frequency
- Increase "Ended Shows Cache (days)" to cache completed shows longer
- Enable "Limit Shows to Season 1" to reduce stub file creation time

### Download Issues

**"Downloads not triggering"**
- Verify Radarr/Sonarr configuration: **Dashboard → Plugins → JellyNext → Test Connection**
- Check that quality profile and root folder are selected
- Ensure Radarr/Sonarr are accessible from Jellyfin server (same network/proper URLs)
- Look for errors in Radarr/Sonarr logs
- Verify PlaybackInterceptor is running: check Jellyfin logs for "JellyNext: Playback detected"

**"Downloads trigger but fail"**
- Check Radarr/Sonarr logs for specific error messages
- Verify root folder has write permissions
- Ensure quality profile exists and is active
- Check TMDB/TVDB IDs are valid (verify in .strm filename: `[tmdbid-12345]`)

**"Anime not going to anime folder"**
- Verify "Sonarr Anime Root Folder" is configured
- Check that Trakt metadata includes "anime" genre (enable "Extra Logging" for debug info)
- Ensure the anime root folder exists and is writable

**"Item marked as watched after download attempt"**
- This should be prevented automatically by PlaybackInterceptor
- If occurring, report as bug with Jellyfin logs

### Performance Issues

**"Jellyfin library scans taking too long"**
- Enable "Limit Shows to Season 1" in per-user settings (reduces stub files from 10 seasons to 1)
- Increase cache expiration to reduce sync frequency
- Consider disabling "Sync Show Recommendations" if you only care about next seasons

**"High memory usage"**
- Content cache holds all recommendations in memory
- Reduce cache expiration time to free memory more frequently
- Restart Jellyfin to clear cache

### Logs and Debugging

**Enable Extra Logging:**
1. **Dashboard → Plugins → JellyNext**
2. Select user → **User Settings**
3. Enable **"Extra Logging"**
4. Trigger sync or download
5. View logs: **Dashboard → Logs** (look for "JellyNext:" prefix)

**Useful Log Locations:**
- Plugin load errors: Jellyfin startup logs
- Sync errors: Scheduled task logs
- Download triggers: Search for "PlaybackInterceptor" or "PlaybackStart"
- API errors: Search for "TraktApi", "RadarrService", "SonarrService"

## Frequently Asked Questions

**Q: Do I need a Trakt VIP subscription?**
A: No, JellyNext works with free Trakt accounts.

**Q: Will this download content automatically?**
A: No, downloads are triggered only when you click "Play" on a virtual library item. It's a manual one-click process.

**Q: Can I use this without Radarr/Sonarr?**
A: Yes, you can still use the virtual libraries to browse recommendations. Downloads just won't work.

**Q: Does this affect my real Jellyfin libraries?**
A: No, virtual libraries are completely separate. They don't modify or interfere with your existing media.

**Q: Can multiple users use this on the same Jellyfin server?**
A: Yes! Each user links their own Trakt account and gets their own virtual libraries with personalized recommendations.

**Q: How often does it sync with Trakt?**
A: Default is every 6 hours. Configurable via "Cache Expiration (hours)" setting.

**Q: What's the difference between Show Recommendations and Next Seasons?**
A:
- **Show Recommendations**: New shows you might like (based on Trakt)
- **Next Seasons**: Next unwatched season of shows you're already watching

**Q: Why only 10 seasons for show recommendations?**
A: Performance. Jellyfin scans can be slow with thousands of stub files. Enable "Limit Shows to Season 1" for even better performance.

**Q: How does anime detection work?**
A: Plugin checks if Trakt metadata includes "anime" genre, then routes to your configured anime root folder in Sonarr.

**Q: Can I customize which recommendations appear?**
A: Yes, via per-user settings:
- Enable/disable movie recommendations, show recommendations, or next seasons
- Filter out collected or watchlisted items
- Limit show recommendations to season 1 only

**Q: Does this use my Jellyfin API key?**
A: No, it uses Trakt's OAuth system. Each user authorizes the plugin via Trakt's website.

**Q: Is this an official Jellyfin plugin?**
A: Not yet. It's currently community-maintained. Official repository submission planned for future.

## License

This project is licensed under the GNU General Public License v3.0 - see the [LICENSE](LICENSE) file for details.

Copyright (C) 2025 luall0

This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

## Acknowledgments

- **[Trakt.tv](https://trakt.tv)**: Powers all recommendations and watch history tracking
- **[TMDB](https://www.themoviedb.org)** & **[TVDB](https://thetvdb.com)**: Metadata providers used by Jellyfin
- **[Jellyfin](https://jellyfin.org)**: Open-source media server platform
- **[jellyfin-plugin-trakt](https://github.com/jellyfin/jellyfin-plugin-trakt)**: OAuth implementation reference
- **[Radarr](https://radarr.video)** & **[Sonarr](https://sonarr.tv)**: Automated media management

---

<div align="center">
  Made with ❤️ for the Jellyfin community
</div>
