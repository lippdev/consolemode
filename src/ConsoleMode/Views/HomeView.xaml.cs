using ConsoleMode.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace ConsoleMode.Views;

public sealed partial class HomeView : UserControl
{
    public MainViewModel ViewModel { get; } = App.ViewModel!;

    // Controller navigation for the desktop interface lives in MainWindow (GamepadNavigator).
    public HomeView()
    {
        InitializeComponent();
    }
}
