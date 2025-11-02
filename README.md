<div align="center">
  <img src="images/jellynext_logo_full.png" alt="JellyNext Logo" width="600"/>

  <h3>Trakt-Powered Discovery for Jellyfin</h3>

  <p>
    <a href="#features">Features</a> •
    <a href="#installation">Installation</a> •
    <a href="#configuration">Configuration</a> •
    <a href="#development">Development</a>
  </p>
</div>

---

## Overview

JellyNext is a Jellyfin plugin that integrates Trakt-powered discovery directly into Jellyfin. Each user can link their own Trakt account to get personalized recommendations and new season notifications, with seamless one-click downloads to Radarr/Sonarr.

## Features

### 🎯 Per-User Trakt Integration
- Each Jellyfin user links their own Trakt account via OAuth
- Fully personalized recommendations based on individual watch history
- Secure token storage with automatic refresh

### 📚 Virtual Libraries
- **Recommendations Library**: Personalized movie and TV show recommendations
- **New Seasons Library**: Notifications when new seasons drop for shows you follow
- Automatically refreshes based on your configured sync interval

### ⬇️ Smart Download Integration
- One-click download triggers to Radarr/Sonarr
- **Smart Button Switching**: Shows "Play" if media exists in your library, "Download" if not
- Works across all Jellyfin clients (Web, iOS, Android, TV, Kodi)

### 🎨 Rich Metadata
- Automatic metadata and poster fetching from TMDB
- Seamless integration with Jellyfin's native UI

## Installation

### Prerequisites
- Jellyfin 10.9.0 or higher
- .NET 8.0 Runtime
- (Optional) Radarr/Sonarr for download functionality

### Install from Repository

1. **Add Plugin Repository** (Coming Soon)
   ```
   Dashboard → Plugins → Repositories → Add Repository
   ```

2. **Install JellyNext**
   ```
   Dashboard → Plugins → Catalog → JellyNext → Install
   ```

3. **Restart Jellyfin**

### Manual Installation

1. Download the latest release from [Releases](../../releases)
2. Extract to your Jellyfin plugins directory:
   - Linux: `/var/lib/jellyfin/plugins/JellyNext/`
   - Windows: `%AppData%\Jellyfin\Server\plugins\JellyNext\`
   - Docker: `/config/plugins/JellyNext/`
3. Restart Jellyfin

## Configuration

### 1. Initial Setup

Navigate to: **Dashboard → Plugins → JellyNext**

#### TMDB API Key
1. Get a free API key from [TMDB](https://www.themoviedb.org/settings/api)
2. Enter it in the plugin configuration
3. Required for metadata and images

#### Radarr/Sonarr (Optional)
Configure your *Arr instances:
- **Radarr URL**: `http://localhost:7878` (or your Radarr address)
- **Radarr API Key**: Found in Radarr Settings → General
- **Sonarr URL**: `http://localhost:8989` (or your Sonarr address)
- **Sonarr API Key**: Found in Sonarr Settings → General

#### Sync Interval
Set how often to refresh recommendations from Trakt (default: 6 hours)

### 2. Link Trakt Account (Per-User)

Each Jellyfin user needs to link their own Trakt account:

1. Admin opens **Dashboard → Plugins → JellyNext**
2. Select a Jellyfin user from the dropdown
3. Click **"Link Trakt Account"**
4. A user code will appear (e.g., `ABC12345`)
5. Visit [trakt.tv/activate](https://trakt.tv/activate)
6. Enter the code
7. The page will auto-detect authorization and show success

**Status Indicators:**
- 🔴 Not Authorized: User hasn't linked Trakt yet
- 🟢 Authorized: User's Trakt account is linked and active

## Usage

### Virtual Libraries

Once a user has linked their Trakt account:

1. Virtual libraries will appear in Jellyfin:
   - **Trakt Recommendations**
   - **Trakt New Seasons**

2. Browse personalized content from Trakt

3. Click on any item:
   - If media exists in your library → **Play button** (normal playback)
   - If media doesn't exist → **Download button** (triggers Radarr/Sonarr)

### Download Workflow

When you click download on a virtual item:
1. Plugin checks if it's a movie or TV show
2. Sends request to Radarr (movies) or Sonarr (TV shows)
3. Your *Arr stack handles the download automatically
4. Once downloaded and imported, item shows "Play" instead of "Download"

## Development

### Building from Source

```bash
# Clone the repository
git clone https://github.com/yourusername/jellynext.git
cd jellynext

# Build the plugin
dotnet build Jellyfin.Plugin.JellyNext/Jellyfin.Plugin.JellyNext.csproj

# Build for release
dotnet build -c Release Jellyfin.Plugin.JellyNext/Jellyfin.Plugin.JellyNext.csproj
```

### Project Structure

```
Jellyfin.Plugin.JellyNext/
├── Api/                          # REST API endpoints
│   └── TraktController.cs        # OAuth & user management
├── Configuration/                # Plugin configuration
│   ├── PluginConfiguration.cs   # Settings model
│   └── configPage.html           # Admin web UI
├── Helpers/                      # Utility classes
│   └── UserHelper.cs             # User lookup helpers
├── Models/                       # Data models
│   ├── TraktUser.cs              # Per-user OAuth storage
│   ├── TraktDeviceCode.cs        # OAuth device flow
│   └── TraktUserAccessToken.cs   # OAuth tokens
├── Services/                     # Business logic
│   └── TraktApi.cs               # Trakt API integration
├── Plugin.cs                     # Plugin entry point
└── PluginServiceRegistrator.cs  # Dependency injection
```

### Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Architecture Notes

- **Per-User OAuth**: Each Jellyfin user has their own Trakt OAuth tokens stored securely
- **Automatic Token Refresh**: Tokens refresh automatically with a 75% safety buffer
- **Device Flow**: Uses OAuth 2.0 Device Authorization Flow (no redirect URLs needed)
- **HTTP Client**: Uses Jellyfin's `NamedClient.Default` for proper Cloudflare handling

## Troubleshooting

### OAuth Issues

**"Cloudflare blocking requests"**
- Ensure you're using the latest version of the plugin

**"Authorization failed"**
- Make sure the user code was entered correctly at trakt.tv/activate
- Check that the code hasn't expired (codes expire after a few minutes)
- Try unlinking and re-linking the Trakt account

### Virtual Libraries Not Appearing

- Verify the user has linked their Trakt account
- Check the sync interval hasn't been set too high
- Restart Jellyfin after initial setup

### Download Not Working

- Verify Radarr/Sonarr URLs and API keys are correct
- Ensure Radarr/Sonarr are accessible from Jellyfin server
- Check Radarr/Sonarr logs for errors

## Roadmap

- [ ] Virtual library implementation
- [ ] Radarr/Sonarr download integration
- [ ] TMDB metadata enrichment
- [ ] Scheduled sync tasks
- [ ] Play/Download button intelligence
- [ ] Multi-language support
- [ ] Advanced filtering options

## License

TBD

## Acknowledgments

- [Trakt.tv](https://trakt.tv) for the amazing recommendation API
- [TMDB](https://www.themoviedb.org) for metadata and images
- [Jellyfin](https://jellyfin.org) for the open-source media server platform
- [jellyfin-plugin-trakt](https://github.com/jellyfin/jellyfin-plugin-trakt) for OAuth implementation inspiration

---

<div align="center">
  Made with ❤️ for the Jellyfin community
</div>
