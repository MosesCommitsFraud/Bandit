using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Windows.Input;
using Bandit.App.Models;
using Bandit.App.Services;
using Bandit.Core.Download;

namespace Bandit.App.ViewModels;

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

    private bool _isDownloading = false;
    public bool IsDownloading { get => _isDownloading; set { _isDownloading = value; OnPropertyChanged(); } }

    private string _downloadProgress = "";
    public string DownloadProgress { get => _downloadProgress; set { _downloadProgress = value; OnPropertyChanged(); } }

    public ICommand DownloadCommand { get; }
    public ICommand AddSoundCommand { get; }
    public ICommand SaveLayoutCommand { get; }
    public ICommand OpenFolderCommand { get; }
    public ICommand ClearAllCommand { get; }
    public ICommand RefreshCommand { get; }

    public MainViewModel(IDownloadService downloader, AudioService audio, HotkeyService hotkeys, SettingsService settings)
    {
        _downloader = downloader;
        _audio = audio;
        _hotkeys = hotkeys;
        _settings = settings;

        DownloadCommand = new RelayCommand(async () =>
        {
            if (string.IsNullOrWhiteSpace(Url)) { return; }
            if (IsDownloading) { return; } // Prevent multiple simultaneous downloads

            try
            {
                IsDownloading = true;
                DownloadProgress = "Starting download...";

                var outDir = _settings.DefaultDownloadDirectory;
                Directory.CreateDirectory(outDir);

                // Always extract audio from videos
                var kind = DownloadKind.Audio;
                
                var progress = new Progress<string>(msg =>
                {
                    DownloadProgress = msg;
                    Append(msg);
                });
                
                var res = await _downloader.DownloadAsync(new DownloadRequest(Url, outDir, kind), progress);

                if (res.Success && !string.IsNullOrEmpty(res.OutputPath))
                {
                    DownloadProgress = "Download complete! Adding to soundboard...";
                    
                    // Check if already exists before adding
                    if (!Sounds.Any(s => s.Model.Path.Equals(res.OutputPath, StringComparison.OrdinalIgnoreCase)))
                    {
                        AddSoundToBoard(res.OutputPath);
                    }
                    
                    // Clear URL after successful download
                    Url = "";
                    DownloadProgress = "Ready!";
                    
                    // Clear progress message after 2 seconds
                    await System.Threading.Tasks.Task.Delay(2000);
                    if (DownloadProgress == "Ready!")
                    {
                        DownloadProgress = "";
                    }
                }
                else
                {
                    DownloadProgress = "Download failed. Please try again.";
                }
            }
            catch (Exception ex)
            {
                DownloadProgress = $"Error: {ex.Message}";
            }
            finally
            {
                IsDownloading = false;
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
        });

        OpenFolderCommand = new RelayCommand(() =>
        {
            var outDir = _settings.DefaultDownloadDirectory;
            Directory.CreateDirectory(outDir);
            System.Diagnostics.Process.Start("explorer.exe", outDir);
        });

        ClearAllCommand = new RelayCommand(() =>
        {
            if (Sounds.Count == 0) return;

            var result = System.Windows.MessageBox.Show(
                "Are you sure you want to remove all sounds from the soundboard?\n\nNote: Files will remain in the downloads folder.",
                "Clear All Sounds",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                // Unbind all hotkeys
                foreach (var sound in Sounds)
                {
                    if (!string.IsNullOrWhiteSpace(sound.Model.Hotkey))
                        _hotkeys.Unbind(sound.Model.Hotkey!);
                }
                Sounds.Clear();
            }
        });

        RefreshCommand = new RelayCommand(() =>
        {
            // Clear current sounds
            foreach (var sound in Sounds)
            {
                if (!string.IsNullOrWhiteSpace(sound.Model.Hotkey))
                    _hotkeys.Unbind(sound.Model.Hotkey!);
            }
            Sounds.Clear();

            // Reload from folder
            LoadSoundsFromFolder();
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
        if (!_audio.IsAudioFile(filePath)) return;

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

        var outDir = _settings.DefaultDownloadDirectory;
        Directory.CreateDirectory(outDir);

        foreach (var file in files)
        {
            try
            {
                if (!File.Exists(file)) continue;

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
                    }

                    // Add to soundboard
                    AddSoundToBoard(destPath);
                }
                else if (ext == ".mp4" || ext == ".webm" || ext == ".mkv" || ext == ".avi" || ext == ".mov")
                {
                    System.Windows.MessageBox.Show(
                        $"{fileName} is a video file.\n\nPlease use the YouTube downloader to extract audio from videos.",
                        "Video File",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error processing {Path.GetFileName(file)}:\n{ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
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
    
#pragma warning disable 67 // Event is never used - required by ICommand interface
    public event EventHandler? CanExecuteChanged;
#pragma warning restore 67
}
