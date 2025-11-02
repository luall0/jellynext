# JellyNext

A Jellyfin plugin that delivers personalized Trakt-powered discovery directly inside Jellyfin.

## Features

- **Per-user Trakt Integration**: Each Jellyfin user can link their own Trakt account
- **Virtual Recommendation Library**: Dynamically generated library showing personalized Trakt recommendations
- **Virtual New Seasons Library**: Shows new seasons for shows you follow on Trakt
- **One-Click Downloads**: Trigger downloads to Radarr/Sonarr directly from Jellyfin
- **Smart Play/Download Button**: Automatically switches between Play and Download based on library availability
- **Metadata Enrichment**: Fetches metadata and images from TMDB

## Installation

1. Add the plugin repository to Jellyfin:
   - Go to Dashboard → Plugins → Repositories
   - Add repository URL (to be provided)

2. Install JellyNext from the plugin catalog

3. Configure the plugin:
   - Go to Dashboard → Plugins → JellyNext
   - Add your Trakt API credentials
   - Add your TMDB API key
   - Configure Radarr/Sonarr URLs and API keys
   - Set sync interval

## Configuration

### Trakt Setup
1. Create a Trakt API application at https://trakt.tv/oauth/applications
2. Copy the Client ID and Client Secret to the plugin configuration

### TMDB Setup
1. Get a TMDB API key from https://www.themoviedb.org/settings/api
2. Add it to the plugin configuration

### Arr Stack Setup
Configure your Radarr and Sonarr instances with their URLs and API keys.

## Development

### Building

```bash
dotnet build Jellyfin.Plugin.JellyNext/Jellyfin.Plugin.JellyNext.csproj
```

### Project Structure

```
Jellyfin.Plugin.JellyNext/
├── Configuration/       # Plugin configuration and UI
├── Api/                # API endpoints
├── Services/           # Core services (Trakt, Radarr, Sonarr, etc.)
├── Plugin.cs           # Main plugin entry point
└── Jellyfin.Plugin.JellyNext.csproj
```

## License

TBD
