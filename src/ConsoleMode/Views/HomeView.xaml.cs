using ConsoleMode.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace ConsoleMode.Views;

public sealed partial class HomeView : UserControl
{
    public MainViewModel ViewModel { get; } = App.ViewModel!;

    public HomeView()
    {
        InitializeComponent();
    }

    /// <summary>Where a controller starts: Play now, or Restore while in console mode.</summary>
    public UIElement? DefaultFocusTarget =>
        ViewModel.IsConsoleActive ? RestoreButton
        : PlayButton.IsEnabled ? PlayButton
        : FocusManager.FindFirstFocusableElement(this) as UIElement;
}
