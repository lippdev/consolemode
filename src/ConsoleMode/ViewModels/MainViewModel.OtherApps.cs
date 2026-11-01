using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using ConsoleMode.Services;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace ConsoleMode.ViewModels;

// "More apps from the developer" in Settings: ControlFS, Tako Launcher and JoyChromium, logos from their repos.
// Links (in the views): their GitHub pages.
public partial class MainViewModel
{
    private ImageSource? _controlFsLogo;
    private ImageSource? _takoLogo;
    private ImageSource? _joyChromiumBrandLogo;

    public ImageSource? ControlFsLogo => _controlFsLogo ??= LoadLogo("controlfs.png");
    public ImageSource? TakoLogo => _takoLogo ??= LoadLogo("tako.png");
    /// <summary>JoyChromium's own logo (the session menu's row shows the installed exe's icon instead).</summary>
    public ImageSource? JoyChromiumBrandLogo => _joyChromiumBrandLogo ??= LoadLogo("joychromium.png");
    /// <summary>Embedded, so the installed and portable builds need no loose files.</summary>
    private static BitmapImage? LoadLogo(string file)
    {
        try
        {
            using var resource = typeof(MainViewModel).Assembly.GetManifestResourceStream($"ConsoleMode.Assets.OtherApps.{file}");
            if (resource is null) return null;
            // Copied: the image may decode after this method returns.
            var copy = new MemoryStream();
            resource.CopyTo(copy);
            copy.Position = 0;
            var image = new BitmapImage();
            image.SetSource(copy.AsRandomAccessStream());
            return image;
        }
        catch (Exception ex)
        {
            AppLog.Write($"Outros apps: logo {file}: {ex.Message}");
            return null;
        }
    }

    [RelayCommand]
    private void OpenLink(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
        catch (Exception ex) { SetStatus(LocalizationService.Get("BrowserOpenFailure", ex.Message), InfoBarSeverity.Error); }
    }
}
