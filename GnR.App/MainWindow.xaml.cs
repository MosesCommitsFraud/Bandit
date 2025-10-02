using System.Windows;
using GnR.App.ViewModels;

namespace GnR.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        _viewModel = vm;
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            _viewModel.HandleDroppedFiles(files);
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "GnR - Grab and Run Soundboard\n\n" +
            "A YouTube downloader with hotkey-enabled soundboard.\n\n" +
            "Features:\n" +
            "• Download audio from YouTube\n" +
            "• Drag & drop audio files\n" +
            "• Global hotkey support\n" +
            "• Auto-loads sounds from Music\\GnR folder\n\n" +
            "Version 1.0",
            "About GnR",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
