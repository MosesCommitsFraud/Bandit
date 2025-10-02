# GnR - Grab and Run Soundboard

A WPF desktop application that combines a YouTube audio downloader with a hotkey-enabled soundboard.

## Features

### 🎵 YouTube Downloader
- Download audio (MP3) or video from YouTube
- Automatic audio extraction and conversion
- Progress tracking and detailed logging
- Auto-add downloaded audio files to soundboard

### 🎹 Soundboard
- Play audio files with button clicks or global hotkeys
- Bind custom keyboard shortcuts (e.g., Ctrl+Alt+1)
- Save and load soundboard layouts
- Support for multiple audio formats (MP3, WAV, OGG, FLAC, M4A)

### ⌨️ Global Hotkeys
- System-wide hotkey support using MouseKeyHook
- Works even when the app is in the background

## Prerequisites

### Required Software
1. **.NET 9.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
2. **yt-dlp** - YouTube downloader
   - Download from [GitHub Releases](https://github.com/yt-dlp/yt-dlp/releases)
   - Place `yt-dlp.exe` in the application directory or add it to your system PATH

### NuGet Packages (automatically restored)
- NAudio - Audio playback
- MouseKeyHook - Global hotkey support
- Microsoft.Extensions.Hosting - Dependency injection and configuration

## Getting Started

### Build and Run

```bash
# Clone the repository
git clone <repository-url>
cd GnR

# Build the solution
dotnet build

# Run the application
dotnet run --project GnR.App
```

### First-Time Setup

1. **Install yt-dlp:**
   - Download `yt-dlp.exe` from [here](https://github.com/yt-dlp/yt-dlp/releases/latest)
   - Option A: Place it in the `GnR.App/bin/Debug/net9.0-windows/` directory
   - Option B: Add it to your system PATH

2. **Launch the app:**
   - The application will create a default download directory at `%USERPROFILE%\Music\GnR`
   - Settings are saved to `%APPDATA%\GnR\settings.json`

## Usage

### Downloading from YouTube

1. Paste a YouTube URL into the text box
2. Select "Audio" or "Video" from the dropdown
3. Click "Download"
4. If "Auto-add audio to soundboard" is checked, downloaded audio files will automatically appear in the soundboard

### Using the Soundboard

1. **Add Sounds:**
   - Click "Add Sound…" to browse for an audio file
   - Or download audio from YouTube (with auto-add enabled)

2. **Play Sounds:**
   - Click the "Play" button on any sound item
   - Or use the assigned hotkey if one is bound

3. **Bind Hotkeys:**
   - Click "Bind Hotkey" on a sound item
   - Enter a key combination (e.g., `Ctrl+Alt+1`)
   - The hotkey will work system-wide

4. **Save Layout:**
   - Click "Save Layout" to persist your soundboard configuration
   - The layout is automatically loaded on next startup

## Project Structure

```
GnR/
├── GnR.App/                  # WPF Application
│   ├── Services/             # Business logic services
│   │   ├── AudioService.cs             # NAudio wrapper for playback
│   │   ├── HotkeyService.cs            # Global hotkey management
│   │   ├── SettingsService.cs          # Save/load configuration
│   │   └── YouTubeDownloadService.cs   # yt-dlp wrapper
│   ├── ViewModels/           # MVVM view models
│   │   ├── MainViewModel.cs
│   │   └── SoundItemViewModel.cs
│   ├── Models/               # Data models
│   │   └── SoundItem.cs
│   └── MainWindow.xaml       # UI layout
│
└── GnR.Core/                 # Core library
    └── downloader/           # Download abstractions
        ├── IDownloadService.cs
        ├── DownloadRequest.cs
        ├── DownloadResult.cs
        └── DownloadOptions.cs
```

## Configuration

### Settings Location
- **Downloads:** `%USERPROFILE%\Music\GnR\`
- **Configuration:** `%APPDATA%\GnR\settings.json`

### Settings File Format
```json
[
  {
    "Path": "C:\\Music\\GnR\\sound.mp3",
    "DisplayName": "sound",
    "Hotkey": "Ctrl+Alt+1"
  }
]
```

## Development

### Architecture
- **MVVM Pattern:** Clean separation between UI and logic
- **Dependency Injection:** Using Microsoft.Extensions.DependencyInjection
- **Service-based:** Modular services for audio, hotkeys, settings, and downloads

## Troubleshooting

### "yt-dlp not found" errors
- Ensure `yt-dlp.exe` is in the app directory or system PATH
- Try running `yt-dlp --version` in a command prompt to verify installation

### Hotkeys not working
- Ensure the key combination isn't already in use by another application
- Run the application with administrator privileges if needed

### Audio playback issues
- Verify the audio file format is supported (MP3, WAV, OGG, FLAC, M4A)
- Check Windows audio device settings

## License

[Specify your license here]

## Credits

- **NAudio** - .NET audio library
- **yt-dlp** - YouTube download tool
- **MouseKeyHook** - Global keyboard/mouse hook library

