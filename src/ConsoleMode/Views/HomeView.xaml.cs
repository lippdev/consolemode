using ConsoleMode.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace ConsoleMode.Views;

public sealed partial class HomeView : UserControl
{
    public MainViewModel ViewModel { get; } = App.ViewModel!;

    public HomeView()
    {
        InitializeComponent();
    }
}
