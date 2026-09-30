using ConsoleMode.Controls;
using ConsoleMode.Services;
using ConsoleMode.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace ConsoleMode.Views;

public sealed partial class SettingsView : UserControl
{
    public MainViewModel ViewModel { get; } = App.ViewModel!;

    public SettingsView()
    {
        InitializeComponent();
        HomeShortcutCard.Content = new ShortcutPicker(ViewModel, ShortcutSlot.Home);
        MenuShortcutCard.Content = new ShortcutPicker(ViewModel, ShortcutSlot.Menu);
        ExitShortcutCard.Content = new ShortcutPicker(ViewModel, ShortcutSlot.Exit);
    }
}
