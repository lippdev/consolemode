using ConsoleMode.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace ConsoleMode.Views;

public sealed partial class SettingsView : UserControl
{
    public MainViewModel ViewModel { get; } = App.ViewModel!;

    public SettingsView()
    {
        InitializeComponent();
    }
}
