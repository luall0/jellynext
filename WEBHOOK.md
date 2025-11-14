# Webhook Integration Guide

JellyNext supports custom webhook integrations for maximum flexibility in handling download requests. This allows you to integrate with any external system that accepts HTTP requests.

## Overview

Webhook mode enables you to send HTTP requests (GET, POST, PUT, or PATCH) to custom URLs when users trigger downloads from virtual libraries. You can fully customize the request format including:

- **Dynamic URLs** with placeholder substitution
- **Custom HTTP headers** with placeholder support
- **JSON payloads** with customizable templates
- **Different configurations** for movies vs TV shows

## When to Use Webhooks

Webhook mode is ideal for:

- **Custom download systems** not based on Radarr/Sonarr
- **External automation** triggered by Jellyfin playback events
- **Notification services** (Discord, Slack, custom endpoints)
- **Third-party integrations** requiring custom data formats
- **Development/testing** of custom media management solutions

If you're using Radarr/Sonarr, the **Native** integration mode is recommended for direct API communication. If you're using Jellyseerr, use the **Jellyseerr** integration mode instead.

## Configuration

Navigate to: **Dashboard → Plugins → JellyNext → Download Integration**

### Step 1: Select Webhook Mode

1. Under **Download Integration**, select **🔗 Webhooks**
2. This will reveal the webhook configuration section

### Step 2: Configure HTTP Method

Choose the HTTP method for your webhook requests:

- **GET**: Simple requests with data in URL/headers
- **POST**: Send data in request body (most common)
- **PUT**: Update existing resources
- **PATCH**: Partial updates

**Default**: POST

### Step 3: Configure Movie Webhooks

#### Webhook URL

Set the URL where movie download requests will be sent.

**Example URLs:**
```
https://example.com/api/download/movie?tmdb={tmdbId}&user={jellyfinUserId}
https://api.myserver.com/movies/request
http://localhost:8080/webhook/movie
```

#### Available Placeholders

Use these placeholders in URLs, headers, and payloads for movies:

| Placeholder | Description | Example Value |
|-------------|-------------|---------------|
| `{tmdbId}` | The Movie Database ID | `550` |
| `{imdbId}` | IMDb ID | `tt0137523` |
| `{title}` | Movie title | `Fight Club` |
| `{year}` | Release year | `1999` |
| `{jellyfinUserId}` | Jellyfin user ID who triggered download | `a1b2c3d4-e5f6-...` |

#### Custom Headers

Add custom headers for authentication, content-type, or other requirements.

**Common examples:**

```
Authorization: Bearer {your-api-token}
X-Api-Key: {your-api-key}
Content-Type: application/json
X-User-Id: {jellyfinUserId}
```

Click **+ Add Header** to add new headers. Both header names and values support placeholders.

#### Request Payload

Configure the JSON payload template for POST/PUT/PATCH requests.

**Default payload template:**
```json
{
  "tmdbId": "{tmdbId}",
  "imdbId": "{imdbId}",
  "title": "{title}",
  "year": "{year}",
  "jellyfinUserId": "{jellyfinUserId}"
}
```

**Custom examples:**

```json
{
  "type": "movie",
  "media": {
    "tmdb": "{tmdbId}",
    "imdb": "{imdbId}"
  },
  "metadata": {
    "title": "{title}",
    "year": {year}
  },
  "requestedBy": "{jellyfinUserId}",
  "timestamp": "2025-01-14T12:00:00Z"
}
```

**Note**: Placeholders are replaced with actual values at runtime. Use the clickable placeholder buttons below the payload editor to insert placeholders easily.

### Step 4: Configure TV Show Webhooks

Similar to movies, but with additional placeholders for TV-specific data.

#### Webhook URL

**Example URLs:**
```
https://example.com/api/download/show?tvdb={tvdbId}&season={seasonNumber}
https://api.myserver.com/shows/request
http://localhost:8080/webhook/show?anime={isAnime}
```

#### Available Placeholders

Use these placeholders in URLs, headers, and payloads for TV shows:

| Placeholder | Description | Example Value |
|-------------|-------------|---------------|
| `{tvdbId}` | TheTVDB ID | `73739` |
| `{tmdbId}` | The Movie Database ID | `1396` |
| `{imdbId}` | IMDb ID | `tt0903747` |
| `{title}` | Show title | `Breaking Bad` |
| `{year}` | First air year | `2008` |
| `{seasonNumber}` | Season number being requested | `2` |
| `{isAnime}` | Whether show is anime | `true` or `false` |
| `{jellyfinUserId}` | Jellyfin user ID who triggered download | `a1b2c3d4-e5f6-...` |

#### Request Payload

**Default payload template:**
```json
{
  "tvdbId": "{tvdbId}",
  "tmdbId": "{tmdbId}",
  "imdbId": "{imdbId}",
  "title": "{title}",
  "year": "{year}",
  "seasonNumber": {seasonNumber},
  "isAnime": {isAnime},
  "jellyfinUserId": "{jellyfinUserId}"
}
```

**Note**: `{seasonNumber}` and `{isAnime}` are numeric/boolean values and should not be quoted in JSON payloads.

### Step 5: Save Configuration

Click **Save** at the bottom of the page to apply webhook settings.

## How It Works

When a user clicks "Play" on a virtual library item:

1. **JellyNext intercepts the playback** and extracts content metadata
2. **Placeholder values are resolved** from the content metadata
3. **Placeholders are replaced** in URL, headers, and payload
4. **HTTP request is sent** to the configured webhook URL
5. **User receives notification** based on webhook response (success/failure)
6. **Playback is stopped** to prevent the dummy video from playing

### Request Flow

```
User clicks "Play" on virtual item
         ↓
PlaybackInterceptor detects virtual path
         ↓
Extracts content IDs and metadata
         ↓
WebhookDownloadProvider selected
         ↓
Replaces placeholders in URL/headers/payload
         ↓
Sends HTTP request to webhook endpoint
         ↓
Returns success/failure to user
         ↓
Stops playback automatically
```

### Response Handling

- **2xx status codes** (200, 201, 204, etc.) are treated as successful
- **Non-2xx status codes** are logged as errors and shown to the user
- **Network errors** (timeouts, connection refused) are caught and reported

The webhook endpoint should return a 2xx status code to indicate the request was accepted. The actual download/processing can happen asynchronously after responding.

## Example Configurations

### Example 1: Simple GET Request

**Use case**: Trigger a download via URL parameters only

**Movie URL:**
```
https://api.example.com/download?type=movie&tmdb={tmdbId}&user={jellyfinUserId}
```

**Show URL:**
```
https://api.example.com/download?type=show&tvdb={tvdbId}&season={seasonNumber}&user={jellyfinUserId}
```

**Method**: GET
**Headers**: None
**Payload**: Empty (GET requests don't use payloads)

### Example 2: POST with API Key Authentication

**Use case**: Secure API with authentication header

**Movie URL:**
```
https://api.example.com/media/movie
```

**Show URL:**
```
https://api.example.com/media/show
```

**Method**: POST
**Headers:**
```
Authorization: Bearer YOUR_API_TOKEN_HERE
Content-Type: application/json
```

**Movie Payload:**
```json
{
  "mediaType": "movie",
  "tmdbId": "{tmdbId}",
  "requestedBy": "{jellyfinUserId}"
}
```

**Show Payload:**
```json
{
  "mediaType": "show",
  "tvdbId": "{tvdbId}",
  "seasonNumber": {seasonNumber},
  "requestedBy": "{jellyfinUserId}"
}
```

### Example 3: Discord/Slack Notification Webhook

**Use case**: Send notifications to Discord/Slack when downloads are requested

**Movie URL:**
```
https://discord.com/api/webhooks/YOUR_WEBHOOK_ID/YOUR_WEBHOOK_TOKEN
```

**Method**: POST
**Headers**: None (Discord handles JSON automatically)

**Movie Payload:**
```json
{
  "content": "New movie download requested",
  "embeds": [{
    "title": "{title} ({year})",
    "description": "TMDB: {tmdbId}\nRequested by: {jellyfinUserId}",
    "color": 5814783
  }]
}
```

**Show Payload:**
```json
{
  "content": "New show download requested",
  "embeds": [{
    "title": "{title} ({year}) - Season {seasonNumber}",
    "description": "TVDB: {tvdbId}\nAnime: {isAnime}\nRequested by: {jellyfinUserId}",
    "color": 5814783
  }]
}
```

### Example 4: Custom Download System with Per-User Tracking

**Use case**: Custom media management system with user-specific quality preferences

**Movie URL:**
```
https://downloads.myserver.com/api/v1/request/movie
```

**Show URL:**
```
https://downloads.myserver.com/api/v1/request/show
```

**Method**: POST
**Headers:**
```
X-Api-Key: YOUR_API_KEY
X-User-Id: {jellyfinUserId}
X-Request-Source: JellyNext
```

**Movie Payload:**
```json
{
  "media": {
    "type": "movie",
    "ids": {
      "tmdb": "{tmdbId}",
      "imdb": "{imdbId}"
    },
    "title": "{title}",
    "year": "{year}"
  },
  "request": {
    "userId": "{jellyfinUserId}",
    "source": "jellynext",
    "timestamp": "ISO8601_TIMESTAMP_HERE"
  }
}
```

**Show Payload:**
```json
{
  "media": {
    "type": "show",
    "ids": {
      "tvdb": "{tvdbId}",
      "tmdb": "{tmdbId}",
      "imdb": "{imdbId}"
    },
    "title": "{title}",
    "year": "{year}",
    "season": {seasonNumber},
    "anime": {isAnime}
  },
  "request": {
    "userId": "{jellyfinUserId}",
    "source": "jellynext",
    "timestamp": "ISO8601_TIMESTAMP_HERE"
  }
}
```

### Example 5: Separate Endpoints for Movies and Shows

**Use case**: Different systems handle movies vs shows

**Movie URL:**
```
https://movies.myserver.com/api/request
```

**Show URL:**
```
https://shows.myserver.com/api/request
```

**Method**: POST (both)
**Headers** (both):
```
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json
```

Configure each payload template according to your specific API requirements.

## Building a Webhook Endpoint

If you're building a custom endpoint to receive JellyNext webhooks, here's what to expect:

### Expected Request Format

**Movie Requests (POST):**
```http
POST /your/webhook/path HTTP/1.1
Host: example.com
Content-Type: application/json
[Your custom headers]

{
  "tmdbId": "550",
  "imdbId": "tt0137523",
  "title": "Fight Club",
  "year": "1999",
  "jellyfinUserId": "a1b2c3d4-e5f6-7890-1234-567890abcdef"
}
```

**Show Requests (POST):**
```http
POST /your/webhook/path HTTP/1.1
Host: example.com
Content-Type: application/json
[Your custom headers]

{
  "tvdbId": "73739",
  "tmdbId": "1396",
  "imdbId": "tt0903747",
  "title": "Breaking Bad",
  "year": "2008",
  "seasonNumber": 2,
  "isAnime": false,
  "jellyfinUserId": "a1b2c3d4-e5f6-7890-1234-567890abcdef"
}
```

### Recommended Response

Your endpoint should respond quickly (< 5 seconds) with:

**Success:**
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "status": "accepted",
  "message": "Download request queued"
}
```

**Failure:**
```http
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "status": "error",
  "message": "Invalid TMDB ID"
}
```

JellyNext only checks the HTTP status code - the response body is logged but not displayed to users.

### Implementation Tips

1. **Return 2xx immediately**: Queue the actual download/processing work asynchronously
2. **Validate input**: Check that required IDs (tmdbId/tvdbId) are present and valid
3. **Log requests**: Keep audit trail of who requested what and when
4. **Handle duplicates**: Check if the same media is already being processed
5. **Implement rate limiting**: Protect against abuse or accidental spam
6. **Use authentication**: Always validate API keys/tokens in production

### Example Python Flask Endpoint

```python
from flask import Flask, request, jsonify
import logging

app = Flask(__name__)
logging.basicConfig(level=logging.INFO)

@app.route('/webhook/movie', methods=['POST'])
def movie_webhook():
    data = request.json

    # Validate required fields
    if not data.get('tmdbId'):
        return jsonify({'status': 'error', 'message': 'Missing tmdbId'}), 400

    # Log the request
    logging.info(f"Movie request: {data['title']} ({data['year']}) - TMDB: {data['tmdbId']}")
    logging.info(f"Requested by Jellyfin user: {data['jellyfinUserId']}")

    # Queue your download logic here (async)
    # queue_movie_download(data['tmdbId'], data['jellyfinUserId'])

    return jsonify({'status': 'accepted', 'message': 'Download queued'}), 200

@app.route('/webhook/show', methods=['POST'])
def show_webhook():
    data = request.json

    # Validate required fields
    if not data.get('tvdbId'):
        return jsonify({'status': 'error', 'message': 'Missing tvdbId'}), 400

    # Log the request
    logging.info(f"Show request: {data['title']} S{data['seasonNumber']:02d}")
    logging.info(f"TVDB: {data['tvdbId']}, Anime: {data['isAnime']}")
    logging.info(f"Requested by Jellyfin user: {data['jellyfinUserId']}")

    # Queue your download logic here (async)
    # queue_show_download(data['tvdbId'], data['seasonNumber'], data['jellyfinUserId'])

    return jsonify({'status': 'accepted', 'message': 'Download queued'}), 200

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=8080)
```

### Example Node.js Express Endpoint

```javascript
const express = require('express');
const app = express();

app.use(express.json());

app.post('/webhook/movie', (req, res) => {
    const { tmdbId, imdbId, title, year, jellyfinUserId } = req.body;

    // Validate required fields
    if (!tmdbId) {
        return res.status(400).json({ status: 'error', message: 'Missing tmdbId' });
    }

    // Log the request
    console.log(`Movie request: ${title} (${year}) - TMDB: ${tmdbId}`);
    console.log(`Requested by Jellyfin user: ${jellyfinUserId}`);

    // Queue your download logic here (async)
    // queueMovieDownload(tmdbId, jellyfinUserId);

    res.json({ status: 'accepted', message: 'Download queued' });
});

app.post('/webhook/show', (req, res) => {
    const { tvdbId, tmdbId, imdbId, title, year, seasonNumber, isAnime, jellyfinUserId } = req.body;

    // Validate required fields
    if (!tvdbId) {
        return res.status(400).json({ status: 'error', message: 'Missing tvdbId' });
    }

    // Log the request
    console.log(`Show request: ${title} S${seasonNumber.toString().padStart(2, '0')}`);
    console.log(`TVDB: ${tvdbId}, Anime: ${isAnime}`);
    console.log(`Requested by Jellyfin user: ${jellyfinUserId}`);

    // Queue your download logic here (async)
    // queueShowDownload(tvdbId, seasonNumber, jellyfinUserId);

    res.json({ status: 'accepted', message: 'Download queued' });
});

app.listen(8080, () => {
    console.log('Webhook server listening on port 8080');
});
```

## Troubleshooting

### Webhook Not Firing

1. **Check integration mode**: Ensure **Webhooks** is selected in plugin settings
2. **Verify URLs are configured**: Both movie and show URLs must be set
3. **Check Jellyfin logs**: Look for "WebhookDownloadProvider" entries
4. **Test URL accessibility**: Ensure webhook endpoint is reachable from Jellyfin server

### Requests Failing

1. **Check endpoint logs**: See what error your webhook endpoint is returning
2. **Verify HTTP method**: Ensure your endpoint accepts the configured method (GET/POST/PUT/PATCH)
3. **Check authentication**: Verify API keys/tokens are correct in custom headers
4. **Test placeholders**: Ensure all placeholders are being replaced correctly (check logs)
5. **Validate JSON**: If using POST/PUT/PATCH, ensure payload template is valid JSON

### Placeholders Not Replaced

1. **Check placeholder syntax**: Must use curly braces `{tmdbId}` not `$tmdbId` or `%tmdbId%`
2. **Case sensitivity**: Placeholders are case-insensitive (`{TMDBID}` works same as `{tmdbId}`)
3. **Available placeholders**: Only use placeholders listed in this guide
4. **Missing data**: If a placeholder value is empty (e.g., missing IMDb ID), it will be replaced with empty string

### Enable Debug Logging

1. Go to: **Dashboard → Plugins → JellyNext**
2. Select user and click **User Settings**
3. Enable **Extra Logging**
4. Reproduce the issue
5. Check: **Dashboard → Logs**
6. Search for: "WebhookDownloadProvider"

Look for entries showing:
- Webhook URL after placeholder replacement
- HTTP method and headers
- Request payload (if POST/PUT/PATCH)
- Response status code
- Error messages if request failed

## Security Considerations

### Authentication

Always use authentication for production webhooks:

- **API Keys**: Add `X-Api-Key` or `Authorization` header
- **Bearer Tokens**: Use `Authorization: Bearer YOUR_TOKEN`
- **HMAC Signatures**: Sign payloads for verification (implement in custom endpoint)

### HTTPS

Use HTTPS URLs for production to encrypt data in transit:

```
✅ https://api.example.com/webhook
❌ http://api.example.com/webhook
```

### Input Validation

Always validate webhook data on your endpoint:

- Check that required IDs are present and numeric
- Validate user IDs match expected format
- Sanitize string inputs (title, year) before using in SQL/shell commands
- Implement rate limiting to prevent abuse

### Network Security

- **Firewall**: Only allow webhook requests from Jellyfin server IP
- **VPN/Private Network**: Keep webhooks on internal network when possible
- **Reverse Proxy**: Use nginx/Caddy to add additional security layers

## Advanced Usage

### Dynamic User-Specific Profiles

Use `{jellyfinUserId}` to route requests to user-specific quality profiles or download paths:

**URL:**
```
https://api.example.com/download?user={jellyfinUserId}&tmdb={tmdbId}
```

Your endpoint can then look up per-user preferences and apply them.

### Multi-Step Workflows

Chain webhooks by having your endpoint trigger additional webhooks:

1. JellyNext → Your validation endpoint (checks quota, permissions)
2. Your endpoint → Download system (if validation passes)
3. Your endpoint → Notification system (notify user of status)

### Conditional Routing

Use `{isAnime}` to route anime to different systems:

**Show Payload:**
```json
{
  "tvdbId": "{tvdbId}",
  "seasonNumber": {seasonNumber},
  "category": "{isAnime}",
  "targetSystem": "automatic"
}
```

Your endpoint can check `category` and route to anime-specific downloaders.

### Audit Logging

Use `{jellyfinUserId}` to maintain audit logs:

- Who requested what content
- When requests were made
- How many requests per user
- Usage patterns and analytics

## Migration from Other Modes

### From Native Mode

If migrating from Native (Radarr/Sonarr) integration:

1. Note your current Radarr/Sonarr URLs and API keys
2. Build a webhook endpoint that forwards to Radarr/Sonarr APIs
3. Test thoroughly before switching
4. Update JellyNext to Webhook mode
5. Configure webhook URLs pointing to your new endpoint

### From Jellyseerr Mode

If migrating from Jellyseerr integration:

1. Note your Jellyseerr configuration
2. Build a webhook endpoint that calls Jellyseerr API
3. Use Jellyseerr's `/api/v1/request` endpoint in your webhook
4. Include `X-Api-User` header for per-user attribution
5. Update JellyNext to Webhook mode

## Support

For webhook integration issues:

1. Check this guide thoroughly
2. Enable debug logging and check Jellyfin logs
3. Test your webhook endpoint independently (curl/Postman)
4. Report issues at: https://github.com/luall0/jellynext/issues

Include in bug reports:
- Webhook configuration (URLs, method, headers - redact secrets!)
- Jellyfin logs showing webhook requests
- Your endpoint logs showing received requests
- JellyNext version

## Comparison with Other Modes

| Feature | Native | Jellyseerr | Webhook |
|---------|--------|------------|---------|
| **Radarr/Sonarr Required** | ✅ Yes | Via Jellyseerr | No |
| **Custom Systems** | ❌ No | ❌ No | ✅ Yes |
| **Approval Workflows** | ❌ No | ✅ Yes | Custom |
| **Request Tracking** | ❌ No | ✅ Yes | Custom |
| **Flexibility** | Low | Medium | High |
| **Setup Complexity** | Low | Medium | High |
| **Best For** | Direct downloads | Multi-user with approvals | Custom integrations |

---

**Ready to configure webhooks?** Head to **Dashboard → Plugins → JellyNext → Download Integration** and select **🔗 Webhooks** to get started!
