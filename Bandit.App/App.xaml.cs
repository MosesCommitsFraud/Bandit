using System.Windows;
using Microsoft.Extensions.Hosting;

namespace Bandit.App;

public partial class App : Application
{
    private readonly IHost _host;
    public App(IHost host) => _host = host;

    protected override void OnExit(ExitEventArgs e)
    {
        _host.Dispose();
        base.OnExit(e);
    }
}
