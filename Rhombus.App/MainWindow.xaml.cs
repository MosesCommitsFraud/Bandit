using System.Windows;
using GnR.App.ViewModels;

namespace GnR.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
