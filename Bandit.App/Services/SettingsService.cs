using System.IO;
using System.Text.Json;
using Bandit.App.Models;
using Bandit.App.ViewModels;

namespace Bandit.App.Services;

public class SettingsService
{
    private readonly string _dir;
    private readonly string _file;

    public string DefaultDownloadDirectory { get; }

    public SettingsService()
    {
        _dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GnR");
        Directory.CreateDirectory(_dir);
        _file = Path.Combine(_dir, "settings.json");
        DefaultDownloadDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "GnR");
    }

    public void Save(IEnumerable<SoundItemViewModel> vms)
    {
        var list = vms.Select(v => v.Model).ToList();
        File.WriteAllText(_file, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
    }

    public List<SoundItem> Load()
    {
        if (!File.Exists(_file)) return new();
        return JsonSerializer.Deserialize<List<SoundItem>>(File.ReadAllText(_file)) ?? new();
    }
}
