using System;
using System.Windows;
using System.Windows.Input;
using GnR.App.Models;
using GnR.App.Services;

namespace GnR.App.ViewModels;

public class SoundItemViewModel
{
    public SoundItem Model { get; }
    private readonly AudioService _audio;
    private readonly HotkeyService _hotkeys;
    private readonly Action<SoundItemViewModel> _remove;

    public string DisplayName => Model.DisplayName;
    public string HotkeyLabel => string.IsNullOrWhiteSpace(Model.Hotkey) ? "(no hotkey)" : Model.Hotkey!;

    public ICommand PlayCommand { get; }
    public ICommand BindCommand { get; }
    public ICommand RemoveCommand { get; }

    public SoundItemViewModel(SoundItem model, AudioService audio, HotkeyService hotkeys, Action<SoundItemViewModel> remove)
    {
        Model = model;
        _audio = audio;
        _hotkeys = hotkeys;
        _remove = remove;

        PlayCommand = new RelayCommand(() => TryPlay());
        BindCommand = new RelayCommand(BindHotkey);
        RemoveCommand = new RelayCommand(() => _remove(this));
    }

    private void TryPlay()
    {
        try
        {
            _audio.Play(Model.Path);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Playback Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BindHotkey()
    {
        var input = Microsoft.VisualBasic.Interaction.InputBox("Enter hotkey (e.g. Ctrl+Alt+1)", "Bind Hotkey", Model.Hotkey ?? "Ctrl+Alt+1");
        if (string.IsNullOrWhiteSpace(input)) return;

        if (!string.IsNullOrWhiteSpace(Model.Hotkey))
            _hotkeys.Unbind(Model.Hotkey!);

        Model.Hotkey = input.Trim();
        _hotkeys.Bind(Model.Hotkey!, () => TryPlayWithHotkey());
    }

    private void TryPlayWithHotkey()
    {
        try
        {
            _audio.Play(Model.Path);
        }
        catch
        {
            // Silently fail for hotkey playback to avoid interrupting user
            // The file might have been moved or deleted
        }
    }
}
