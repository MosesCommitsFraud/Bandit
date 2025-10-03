using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Bandit.App.Services;
using Bandit.App.ViewModels;
using Bandit.Core.Download;

namespace Bandit.App;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureLogging(l =>
            {
                l.ClearProviders();
                l.AddConsole();
            })
            .ConfigureServices(services =>
            {
                // Services
                services.AddSingleton<AudioService>();
                services.AddSingleton<HotkeyService>();
                services.AddSingleton<SettingsService>();

                // Download service - YouTube downloader using yt-dlp
                services.AddSingleton<IDownloadService, YouTubeDownloadService>();

                // ViewModels
                services.AddSingleton<MainViewModel>();

                // Windows
                services.AddSingleton<MainWindow>();
            })
            .Build();

        var app = new App(host);
        app.InitializeComponent();
        app.Run(host.Services.GetRequiredService<MainWindow>());
    }
}
