# Quick Debugging Reference

## TL;DR - Attach Debugger to Running Jellyfin

1. Start Jellyfin: Run **"Debug with Docker"** configuration in Rider
2. Wait for Jellyfin to start (check http://localhost:8096)
3. Attach debugger:
   - Go to **Run > Attach to Process...** in Rider
   - Select **Docker** from connection dropdown
   - Select container: `jellyfin-jellynext-debug`
   - Find and select the `jellyfin` process
   - Click **Attach with .NET Debugger**
4. Set breakpoints in your plugin code
5. Trigger your code through Jellyfin UI
6. Debug!

## Available Run Configurations

| Configuration | Purpose |
|--------------|---------|
| **Build (Debug)** | Build plugin in Debug mode |
| **Build (Release)** | Build plugin in Release mode |
| **Clean** | Clean build artifacts |
| **Rebuild (Release)** | Clean + build Release |
| **Debug with Docker** | Build + start Jellyfin container |
| **Debug: Build + Start + Attach** | Full workflow + attach instructions |
| **Attach to Docker** | Show PID and attach instructions |
| **Start Jellyfin Docker** | Start container only |
| **Stop Jellyfin Docker** | Stop and remove container |
| **Rebuild and Restart Docker** | Rebuild plugin + restart container |

## Port Mappings

- **8096** - Jellyfin HTTP UI
- **8920** - Jellyfin HTTPS UI (optional)
- **4024** - Remote debugger port (reserved for future use)

## File Locations

- **Dockerfile.debug** - Docker image with vsdbg debugger
- **docker-compose.debug.yml** - Docker composition
- **scripts/attach-debugger.sh** - Helper script to find Jellyfin PID
- **README.debug.md** - Full documentation

## Quick Commands

```bash
# Start Jellyfin
docker-compose -f docker-compose.debug.yml up --build

# Stop Jellyfin
docker-compose -f docker-compose.debug.yml down

# Rebuild plugin + restart container
dotnet build Jellyfin.Plugin.JellyNext/Jellyfin.Plugin.JellyNext.csproj -c Debug
docker-compose -f docker-compose.debug.yml up --build --force-recreate

# Find Jellyfin process ID
docker exec jellyfin-jellynext-debug pidof jellyfin

# View logs
docker logs -f jellyfin-jellynext-debug

# Clean start (remove all data)
docker-compose -f docker-compose.debug.yml down -v
```

## Typical Debug Session

1. **Start**: Run "Debug with Docker"
2. **Access**: Open http://localhost:8096
3. **Attach**: Run > Attach to Process > Docker > jellyfin process
4. **Breakpoint**: Set breakpoint in Plugin.cs or your code
5. **Trigger**: Navigate to Jellyfin Dashboard → Plugins
6. **Debug**: Breakpoint should hit!
7. **Code Change**: Edit code
8. **Reload**: Run "Rebuild and Restart Docker"
9. **Re-attach**: Attach debugger again
10. **Test**: Trigger code again

## Pro Tips

- Use **Rebuild and Restart Docker** frequently to test changes
- PDB files are auto-mounted for debug symbols
- Docker volumes persist Jellyfin config between restarts
- Use `docker-compose down -v` for a completely fresh start
- Check `docker logs jellyfin-jellynext-debug` if plugin doesn't load