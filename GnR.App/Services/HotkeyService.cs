using Gma.System.MouseKeyHook;

namespace GnR.App.Services;

public class HotkeyService : IDisposable
{
    private readonly IKeyboardMouseEvents _global;
    private readonly Dictionary<string, Action> _bindings = new(StringComparer.OrdinalIgnoreCase);

    public HotkeyService()
    {
        _global = Hook.GlobalEvents();
        _global.KeyDown += OnKeyDown;
    }

    public void Bind(string chord, Action action) => _bindings[chord] = action;
    public void Unbind(string chord) => _bindings.Remove(chord);

    private void OnKeyDown(object? sender, System.Windows.Forms.KeyEventArgs e)
    {
        var chord = $"{(e.Control ? "Ctrl+" : "")}{(e.Alt ? "Alt+" : "")}{(e.Shift ? "Shift+" : "")}{e.KeyCode}";
        if (_bindings.TryGetValue(chord, out var action)) action();
    }

    public void Dispose()
    {
        _global.KeyDown -= OnKeyDown;
        _global.Dispose();
    }
}
