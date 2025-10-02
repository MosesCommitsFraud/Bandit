# Quick Start Guide

Get GnR (Grab and Run) up and running in 5 minutes!

## Step 1: Install yt-dlp and ffmpeg

### Install yt-dlp

Choose one method:

**Method A: Automatic (Recommended)**
```powershell
.\download-ytdlp.ps1
```

**Method B: Package Manager**
```powershell
# Using winget
winget install yt-dlp

# OR using Chocolatey
choco install yt-dlp
```

**Method C: Manual**
Download from https://github.com/yt-dlp/yt-dlp/releases/latest and place in the app directory.

### Install ffmpeg (Required for audio conversion)

**Easiest method (Chocolatey):**
```powershell
choco install ffmpeg
```

**Or using winget:**
```powershell
winget install Gyan.FFmpeg
```

See `INSTALL-FFMPEG.md` for more options.

## Step 2: Build and Run

```bash
dotnet run --project GnR.App
```

Or open `GnR.sln` in Visual Studio and press **F5**.

## Step 3: Download Your First Sound

1. Copy a YouTube URL (e.g., `https://www.youtube.com/watch?v=dQw4w9WgXcQ`)
2. Paste it in the text box
3. Make sure "Audio" is selected
4. Click **Download**
5. Watch the log for progress

✅ The sound will automatically appear in your soundboard!

## Step 4: Bind a Hotkey

1. Find your downloaded sound in the soundboard section
2. Click **⌨ Bind Hotkey**
3. Enter a combination like `Ctrl+Alt+1`
4. Click OK

🎵 Now press your hotkey anywhere in Windows to play the sound!

## Step 5: Save Your Layout

Click **Save Layout** to remember your sounds and hotkeys for next time.

---

## Tips

- **Downloaded Files**: Check `%USERPROFILE%\Music\GnR\` for your files
- **Settings**: Located at `%APPDATA%\GnR\settings.json`
- **Hotkey Not Working?**: Try running as Administrator

## Common Hotkey Combinations

- `Ctrl+Alt+1` through `Ctrl+Alt+9` - Numeric shortcuts
- `Ctrl+Shift+A` through `Ctrl+Shift+Z` - Letter shortcuts
- `Alt+F1` through `Alt+F12` - Function key shortcuts

## What's Next?

- Add more sounds using **Add Sound…** to browse local files
- Download playlists (future feature)
- Organize with categories (future feature)

Need help? Check `README.md` for detailed documentation or `SETUP.md` for troubleshooting.

---

**Enjoy your YouTube soundboard!** 🎵

