using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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

    public ICommand DownloadCommand { get; }
    public ICommand AddSoundCommand { get; }
    public ICommand SaveLayoutCommand { get; }

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
            Append($"Downloading {kind}: {Url}");

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

        // Load saved
        foreach (var s in _settings.Load())
        {
            var vm = new SoundItemViewModel(s, _audio, _hotkeys, RemoveSound);
            Sounds.Add(vm);
            if (!string.IsNullOrWhiteSpace(s.Hotkey))
                _hotkeys.Bind(s.Hotkey!, () => _audio.Play(s.Path));
        }
    }

    private void AddSoundToBoard(string filePath)
    {
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
