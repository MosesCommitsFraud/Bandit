# GnR (Grab and Run) Features

## Core Features

### 🎬 YouTube Downloader
- **URL Support**: YouTube videos, shorts, and standard watch links
- **Format Options**:
  - Audio: Extracts and converts to MP3 (best quality)
  - Video: Downloads MP4 or best available format
- **Real-time Progress**: Live download status in the application log
- **Smart Output**: Automatic file naming based on video title
- **Configurable Quality**: Audio quality settings (0-9, where 0 is best)

### 🎵 Integrated Soundboard
- **Multiple Audio Formats**: MP3, WAV, OGG, FLAC, M4A
- **Seamless Integration**: Downloaded audio automatically added to soundboard
- **Quick Playback**: One-click play from the interface
- **Persistent Layout**: Save and restore your soundboard configuration
- **Sound Counter**: Visual display of total sounds loaded

### ⌨️ Global Hotkey System
- **System-wide Shortcuts**: Hotkeys work even when app is minimized
- **Flexible Binding**: Support for Ctrl, Alt, Shift modifiers + any key
- **Easy Configuration**: Simple dialog to set hotkeys
- **Visual Feedback**: Hotkey labels displayed on each sound item
- **Unbind Support**: Remove or change hotkeys at any time

### 💾 Persistent Settings
- **Auto-save**: Soundboard layout persists between sessions
- **JSON Configuration**: Human-readable settings file
- **Default Directories**: Sensible defaults for downloads and config
- **Automatic Restore**: Loads previous session on startup

## User Interface

### Modern WPF Design
- **Clean Layout**: Three-panel design (downloader, logs, soundboard)
- **Responsive**: Scrollable soundboard for unlimited sounds
- **Visual Feedback**: Real-time log updates during downloads
- **Tooltips**: Helpful hints on UI elements
- **Card-based Design**: Each sound in its own styled card

### Workflow Optimization
- **Auto-add Toggle**: Optional automatic soundboard addition
- **Batch Operations**: Save entire layout with one click
- **Multi-select Support**: (Future enhancement)
- **Drag & Drop**: (Future enhancement)

## Technical Features

### Architecture
- **MVVM Pattern**: Clean separation of concerns
- **Dependency Injection**: Using Microsoft.Extensions.DependencyInjection
- **Service-based**: Modular, testable components
- **Async/Await**: Non-blocking operations for downloads

### Services
1. **AudioService**: NAudio wrapper for playback
2. **HotkeyService**: Global keyboard hook management
3. **SettingsService**: JSON-based configuration persistence
4. **YouTubeDownloadService**: yt-dlp process wrapper

### Error Handling
- **Validation**: URL and file path validation
- **Try-Catch**: Comprehensive exception handling
- **User Feedback**: Clear error messages in the log
- **Graceful Degradation**: App continues even if download fails

## Supported Formats

### Download
- **Input**: YouTube URLs (youtube.com, youtu.be, youtube.com/shorts)
- **Audio Output**: MP3 (auto-converted from source)
- **Video Output**: MP4, WebM, MKV (based on availability)

### Playback
- MP3
- WAV
- OGG/Vorbis
- FLAC
- M4A/AAC

## Future Enhancements (Roadmap)

### Planned Features
- [ ] Playlist download support
- [ ] Custom audio trimming/editing
- [ ] Volume controls per sound
- [ ] Sound categories/folders
- [ ] Search/filter sounds
- [ ] Drag-and-drop file support
- [ ] Custom themes
- [ ] Export/import soundboard layouts
- [ ] Sound waveform visualization
- [ ] Multiple soundboard pages
- [ ] Favorite sounds
- [ ] Recently played history
- [ ] Download queue management
- [ ] Video preview player
- [ ] Cloud sync (optional)

### Technical Improvements
- [ ] Plugin system for additional sources
- [ ] Better progress bar visualization
- [ ] Embedded yt-dlp (no external dependency)
- [ ] Auto-update for yt-dlp
- [ ] Crash reporting
- [ ] Performance metrics
- [ ] Memory optimization for large libraries

## Comparison with Alternatives

### vs. Traditional Soundboards
| Feature | GnR | Others |
|---------|---------|--------|
| YouTube Download | ✅ Built-in | ❌ Separate tool needed |
| Auto-add Downloads | ✅ Yes | ❌ No |
| Global Hotkeys | ✅ Yes | ⚠️ Sometimes |
| Persistent Layout | ✅ Yes | ⚠️ Varies |
| Free & Open Source | ✅ Yes | ⚠️ Varies |

### vs. Standalone Downloaders
| Feature | GnR | yt-dlp alone |
|---------|---------|--------------|
| GUI | ✅ Yes | ❌ CLI only |
| Playback | ✅ Built-in | ❌ No |
| Hotkeys | ✅ Yes | ❌ No |
| Organization | ✅ Soundboard | ❌ Files only |

## Performance

### Resource Usage
- **Memory**: ~50-100 MB (idle)
- **CPU**: Minimal when idle, varies during download
- **Disk**: Depends on downloads (audio ~3-10 MB per song)
- **Startup Time**: < 2 seconds

### Scalability
- **Sounds**: Tested with 100+ sounds
- **Concurrent Downloads**: Configurable (default: 3)
- **Large Files**: Supports any size (limited by yt-dlp)

## Platform Support

### Current
- ✅ Windows 10/11
- ✅ .NET 9.0

### Future
- ⏳ Linux (with Avalonia UI port)
- ⏳ macOS (with Avalonia UI port)

## Dependencies

### Required
- .NET 9.0 Runtime
- yt-dlp executable

### NuGet Packages
- NAudio 2.2.1
- MouseKeyHook 5.7.1
- Microsoft.Extensions.Hosting 9.0.9
- Microsoft.Extensions.Logging 9.0.9

### Optional
- ffmpeg (yt-dlp uses it for audio conversion if available)

