# Setup Guide for GnR (Grab and Run)

## Quick Start

### 1. Install yt-dlp

GnR requires **yt-dlp** to download content from YouTube.

#### Windows

**Option A: Place in Application Directory (Recommended for distribution)**
1. Download `yt-dlp.exe` from https://github.com/yt-dlp/yt-dlp/releases/latest
2. Place it in `GnR.App\bin\Debug\net9.0-windows\` (or next to the built .exe)

**Option B: Add to System PATH**
1. Download `yt-dlp.exe` from https://github.com/yt-dlp/yt-dlp/releases/latest
2. Place it in a directory (e.g., `C:\Tools\`)
3. Add that directory to your system PATH:
   - Right-click "This PC" → Properties → Advanced System Settings
   - Click "Environment Variables"
   - Under "System variables", find and edit "Path"
   - Add your yt-dlp directory
   - Click OK and restart any open terminals

**Option C: Using winget (Windows Package Manager)**
```powershell
winget install yt-dlp
```

**Option D: Using Chocolatey**
```powershell
choco install yt-dlp
```

### 2. Verify Installation

Open PowerShell or Command Prompt and run:
```bash
yt-dlp --version
```

You should see version information. If you get an error, yt-dlp is not in your PATH.

### 3. Build and Run

```bash
# Navigate to the project directory
cd GnR

# Restore NuGet packages (happens automatically on build)
dotnet restore

# Build the solution
dotnet build

# Run the application
dotnet run --project GnR.App
```

Or simply open `GnR.sln` in Visual Studio and press F5.

## Testing the Download Feature

### Using the Real YouTube Downloader

1. Launch the application
2. Paste a YouTube URL (e.g., `https://www.youtube.com/watch?v=dQw4w9WgXcQ`)
3. Select "Audio" from the dropdown
4. Click "Download"
5. Watch the log output for progress

## File Locations

### Downloads
By default, files are downloaded to:
```
%USERPROFILE%\Music\GnR\
```
Example: `C:\Users\YourName\Music\GnR\`

### Settings
Soundboard layouts are saved to:
```
%APPDATA%\GnR\settings.json
```
Example: `C:\Users\YourName\AppData\Roaming\GnR\settings.json`

## Common Issues

### Issue: "yt-dlp is not recognized"
**Solution:** yt-dlp is not in your PATH. Place `yt-dlp.exe` in the application directory or follow the PATH setup instructions above.

### Issue: Downloads fail with "Unable to extract"
**Solution:** 
- Update yt-dlp to the latest version: `yt-dlp -U`
- Some videos may be region-locked or have download restrictions

### Issue: Hotkeys don't work
**Solution:**
- Try running the application as Administrator
- Check if another application is using the same hotkey combination
- Try a different key combination

### Issue: Audio doesn't play
**Solution:**
- Verify the file was downloaded successfully (check the log)
- Ensure the audio file format is supported
- Check Windows audio settings

## Building for Release

To create a release build:

```bash
dotnet publish GnR.App -c Release -r win-x64 --self-contained false
```

The output will be in: `GnR.App\bin\Release\net9.0-windows\win-x64\publish\`

**Don't forget to include yt-dlp.exe in your distribution!**

## Advanced Configuration

### Custom Download Directory

Edit `GnR.App/Services/SettingsService.cs` and modify:
```csharp
DefaultDownloadDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "GnR");
```

### Audio Quality Settings

Edit `GnR.App/Services/YouTubeDownloadService.cs` and modify:
```csharp
args.Add("--audio-quality 0"); // 0=best, 9=worst
```

## Development Tips

### Hot Reload
The app supports .NET hot reload for faster development:
```bash
dotnet watch run --project GnR.App
```

### Debugging
- Set breakpoints in Visual Studio or VS Code
- Check the log output window for yt-dlp messages
- The log text box in the UI shows all download progress

### Adding New Features
The codebase uses:
- **MVVM pattern** for UI separation
- **Dependency Injection** for service management
- **Commands** for button actions

See the architecture section in README.md for more details.

