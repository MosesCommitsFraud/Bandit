namespace Bandit.App.Models;

public class SoundItem
{
    public string Path { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Hotkey { get; set; } // e.g. "Ctrl+Alt+1"
}
