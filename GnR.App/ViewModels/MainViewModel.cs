using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Windows.Input;
using GnR.App.Models;
using GnR.App.Services;
using GnR.Core.Download;

namespace GnR.App.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly IDownloadService _downloader;
    private readonly AudioService _audio;
    private readonly HotkeyService _hotkeys;
    private readonly SettingsService _settings;

    public ObservableCollection<SoundItemViewModel> Sounds { get; } = new();

    private string _url = "";
    public string Url { get => _url; set { _url = value; OnPropertyChanged(); } }

    public string SelectedKind { get; set; } = "Audio";

    private string _logText = "";
    public string LogText { get => _logText; set { _logText = value; OnPropertyChanged(); } }

    private bool _autoAddToSoundboard = true;
    public bool AutoAddToSoundboard { get => _autoAddToSoundboard; set { _autoAddToSoundboard = value; OnPropertyChanged(); } }

    private bool _extractAudioFromVideos = false;
    public bool ExtractAudioFromVideos { get => _extractAudioFromVideos; set { _extractAudioFromVideos = value; OnPropertyChanged(); } }

    public ICommand DownloadCommand { get; }
    public ICommand AddSoundCommand { get; }
    public ICommand SaveLayoutCommand { get; }
    public ICommand OpenFolderCommand { get; }

    public MainViewModel(IDownloadService downloader, AudioService audio, HotkeyService hotkeys, SettingsService settings)
    {
        _downloader = downloader;
        _audio = audio;
        _hotkeys = hotkeys;
        _settings = settings;

        DownloadCommand = new RelayCommand(async () =>
        {
            if (string.IsNullOrWhiteSpace(Url)) { Append("Enter a URL."); return; }

            var outDir = _settings.DefaultDownloadDirectory;
            Directory.CreateDirectory(outDir);

            var kind = SelectedKind.Equals("Video", StringComparison.OrdinalIgnoreCase) ? DownloadKind.Video : DownloadKind.Audio;
            
            // If extracting audio from videos, force audio download
            if (ExtractAudioFromVideos && kind == DownloadKind.Video)
            {
                kind = DownloadKind.Audio;
                Append($"Downloading and extracting audio: {Url}");
            }
            else
            {
                Append($"Downloading {kind}: {Url}");
            }
            
            Append($"📁 Saving to: {outDir}");

            var progress = new Progress<string>(Append);
            var res = await _downloader.DownloadAsync(new DownloadRequest(Url, outDir, kind), progress);

            if (res.Success)
            {
                Append($"✅ Done: {res.OutputPath}");
                
                // Auto-add audio files to soundboard
                if (AutoAddToSoundboard && kind == DownloadKind.Audio && !string.IsNullOrEmpty(res.OutputPath))
                {
                    AddSoundToBoard(res.OutputPath);
                    Append($"🎵 Added to soundboard");
                }
            }
            else
            {
                Append($"❌ Error: {res.Error}");
            }
        });

        AddSoundCommand = new RelayCommand(() =>
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "Audio|*.mp3;*.wav;*.ogg;*.flac;*.m4a",
                Multiselect = false
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                var model = new SoundItem
                {
                    Path = ofd.FileName,
                    DisplayName = Path.GetFileNameWithoutExtension(ofd.FileName)
                };
                var vm = new SoundItemViewModel(model, _audio, _hotkeys, RemoveSound);
                Sounds.Add(vm);
            }
        });

        SaveLayoutCommand = new RelayCommand(() =>
        {
            _settings.Save(Sounds);
            Append("Layout saved.");
        });

        OpenFolderCommand = new RelayCommand(() =>
        {
            var outDir = _settings.DefaultDownloadDirectory;
            Directory.CreateDirectory(outDir);
            System.Diagnostics.Process.Start("explorer.exe", outDir);
            Append($"📁 Opened: {outDir}");
        });

        // Load all audio files from the downloads folder
        LoadSoundsFromFolder();
    }

    private void LoadSoundsFromFolder()
    {
        var outDir = _settings.DefaultDownloadDirectory;
        if (!Directory.Exists(outDir))
        {
            Directory.CreateDirectory(outDir);
            return;
        }

        // Load saved settings to restore hotkeys
        var savedSounds = _settings.Load().ToDictionary(s => s.Path, StringComparer.OrdinalIgnoreCase);

        // Get all audio files from the folder
        var audioFiles = Directory.GetFiles(outDir)
            .Where(f => _audio.IsAudioFile(f))
            .OrderBy(f => Path.GetFileName(f));

        foreach (var filePath in audioFiles)
        {
            // Check if we have saved settings for this file
            SoundItem? savedSound = null;
            savedSounds.TryGetValue(filePath, out savedSound);

            var model = new SoundItem
            {
                Path = filePath,
                DisplayName = savedSound?.DisplayName ?? Path.GetFileNameWithoutExtension(filePath),
                Hotkey = savedSound?.Hotkey
            };

            var vm = new SoundItemViewModel(model, _audio, _hotkeys, RemoveSound);
            Sounds.Add(vm);

            // Restore hotkey binding if it exists
            if (!string.IsNullOrWhiteSpace(model.Hotkey))
            {
                try
                {
                    _hotkeys.Bind(model.Hotkey!, () =>
                    {
                        try { _audio.Play(model.Path); } catch { /* Silent fail for hotkey */ }
                    });
                }
                catch
                {
                    // Hotkey binding failed, silently ignore
                }
            }
        }
    }

    private void AddSoundToBoard(string filePath)
    {
        // Validate that it's an audio file
        if (!_audio.IsAudioFile(filePath))
        {
            Append($"⚠️ Cannot add {Path.GetFileName(filePath)} - only audio files can be added to soundboard");
            return;
        }

        // Check if already in soundboard
        if (Sounds.Any(s => s.Model.Path.Equals(filePath, StringComparison.OrdinalIgnoreCase)))
        {
            return; // Already exists, don't add duplicate
        }

        var model = new SoundItem
        {
            Path = filePath,
            DisplayName = Path.GetFileNameWithoutExtension(filePath)
        };
        var vm = new SoundItemViewModel(model, _audio, _hotkeys, RemoveSound);
        Sounds.Add(vm);
    }

    private void RemoveSound(SoundItemViewModel vm)
    {
        if (!string.IsNullOrWhiteSpace(vm.Model.Hotkey))
            _hotkeys.Unbind(vm.Model.Hotkey!);
        Sounds.Remove(vm);
    }

    public void HandleDroppedFiles(string[] files)
    {
        if (files == null || files.Length == 0) return;

        Append($"📥 Dropped {files.Length} file(s)");
        var outDir = _settings.DefaultDownloadDirectory;
        Directory.CreateDirectory(outDir);

        foreach (var file in files)
        {
            try
            {
                if (!File.Exists(file))
                {
                    Append($"❌ File not found: {Path.GetFileName(file)}");
                    continue;
                }

                var fileName = Path.GetFileName(file);
                var ext = Path.GetExtension(file).ToLowerInvariant();

                // Check if it's an audio file
                if (_audio.IsAudioFile(file))
                {
                    // Copy to downloads folder
                    var destPath = Path.Combine(outDir, fileName);
                    
                    // Handle duplicate names
                    if (File.Exists(destPath) && !file.Equals(destPath, StringComparison.OrdinalIgnoreCase))
                    {
                        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                        var counter = 1;
                        do
                        {
                            fileName = $"{nameWithoutExt} ({counter}){ext}";
                            destPath = Path.Combine(outDir, fileName);
                            counter++;
                        } while (File.Exists(destPath));
                    }

                    // Copy file if not already in the target folder
                    if (!file.Equals(destPath, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Copy(file, destPath, overwrite: false);
                        Append($"📋 Copied: {fileName}");
                    }
                    else
                    {
                        Append($"📋 Using: {fileName}");
                    }

                    // Add to soundboard
                    AddSoundToBoard(destPath);
                    Append($"🎵 Added to soundboard: {Path.GetFileNameWithoutExtension(fileName)}");
                }
                else if (ext == ".mp4" || ext == ".webm" || ext == ".mkv" || ext == ".avi" || ext == ".mov")
                {
                    Append($"⚠️ {fileName} is a video file - only audio files can be added to soundboard");
                    Append($"💡 Tip: Use the downloader with 'Extract audio from videos' option for videos");
                }
                else
                {
                    Append($"⚠️ {fileName} - unsupported format");
                }
            }
            catch (Exception ex)
            {
                Append($"❌ Error processing {Path.GetFileName(file)}: {ex.Message}");
            }
        }
    }

    private void Append(string s) => LogText += (LogText.Length > 0 ? Environment.NewLine : "") + s;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? p = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}

public class RelayCommand : ICommand
{
    private readonly Func<Task>? _async;
    private readonly Action? _sync;

    public RelayCommand(Func<Task> async) => _async = async;
    public RelayCommand(Action sync) => _sync = sync;

    public bool CanExecute(object? parameter) => true;
    public async void Execute(object? parameter)
    {
        if (_async != null) await _async();
        else _sync?.Invoke();
    }
    public event EventHandler? CanExecuteChanged;
}
