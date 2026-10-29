using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConsoleMode.Native;
using ConsoleMode.Services;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace ConsoleMode.ViewModels;

// The pinned "Browser (JoyChromium)" row of the session menu, right under ControlFS and working the same way:
// installed, A opens it full screen over the game; missing, A downloads and installs it (the row shows the progress).
public partial class MainViewModel
{
    /// <summary>Whether JoyChromium is installed; decides what the row says. Read each time the menu opens.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(JoyChromiumRowText))] private bool _joyChromiumInstalled;
    /// <summary>The install asked from the row is running (it goes on if the menu is closed meanwhile).</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(JoyChromiumRowText))] private bool _joyChromiumInstalling;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(JoyChromiumRowText))] private double _joyChromiumProgress;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(JoyChromiumRowText))] private bool _joyChromiumSetupRunning;
    /// <summary>The install failed: the next press opens the release page instead.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(JoyChromiumRowText))] private bool _joyChromiumInstallFailed;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(JoyChromiumRowText))] private bool _joyChromiumJustInstalled;

    // Downloading, installing and their outcomes read the same as ControlFS's row.
    public string JoyChromiumRowText =>
        JoyChromiumSetupRunning ? Texts.ControlFsInstalling
        : JoyChromiumInstalling ? LocalizationService.Get("ControlFsDownloading", (int)JoyChromiumProgress)
        : JoyChromiumJustInstalled ? Texts.ControlFsInstalled
        : JoyChromiumInstalled ? Texts.JoyChromiumOpen
        : JoyChromiumInstallFailed ? Texts.ControlFsInstallFailed
        : Texts.ControlFsMissing;

    /// <summary>The icon of the installed JoyChromium.exe, like the ControlFS logo beside it; a globe while it is missing.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowJoyChromiumGlobe))] private ImageSource? _joyChromiumLogo;
    public bool ShowJoyChromiumGlobe => JoyChromiumLogo is null;
    private string? _joyChromiumLogoExe;

    /// <summary>When the session menu opens: installed or not, and a failed install is offered again.</summary>
    private void RefreshJoyChromiumRow()
    {
        JoyChromiumInstalled = JoyChromiumService.FindExe() is not null;
        _ = LoadJoyChromiumLogoAsync();
        if (!JoyChromiumInstalling) JoyChromiumInstallFailed = false;
        JoyChromiumJustInstalled = false;
    }

    private async Task LoadJoyChromiumLogoAsync()
    {
        var exe = JoyChromiumService.FindExe();
        if (exe is null || string.Equals(exe, _joyChromiumLogoExe, StringComparison.OrdinalIgnoreCase)) return;
        _joyChromiumLogoExe = exe;
        var bitmap = await Task.Run(() => WindowIcons.ForApp(exe, null, 128));
        if (bitmap is null || !string.Equals(exe, _joyChromiumLogoExe, StringComparison.OrdinalIgnoreCase)) return;
        try
        {
            var image = new WriteableBitmap(bitmap.Width, bitmap.Height);
            using (var stream = image.PixelBuffer.AsStream()) stream.Write(bitmap.Pixels);
            JoyChromiumLogo = image;
        }
        catch (Exception ex) { AppLog.Write($"JoyChromium: ícone: {ex.Message}"); }
    }

    /// <summary>
    /// The menu closes and JoyChromium opens full screen on the session screen (or the open one comes to the front).
    /// Missing, the same press installs it, and the next one opens it; if the install failed, the release page opens.
    /// </summary>
    [RelayCommand]
    private void OpenJoyChromium()
    {
        if (JoyChromiumInstalling) return;
        var installed = JoyChromiumService.FindExe() is not null;
        JoyChromiumInstalled = installed;
        if (!installed && !JoyChromiumInstallFailed)
        {
            _ = InstallJoyChromiumAsync();
            return;
        }
        var screen = IsConsoleActive ? Engine.State.FocusMonitorRect : Engine.Monitors.GetMonitorRect(SessionMonitorName, Engine.State);
        CloseSessionMenu();
        if (installed && JoyChromiumService.Open())
        {
            // Going back to the PC takes it out of full screen (see ConsoleEngine.ReturnJoyChromiumToWindow).
            if (IsConsoleActive) Engine.State.JoyChromiumOpened = true;
            _ = Task.Run(() => JoyChromiumService.EnsureOnScreen(screen));
            return;
        }
        AppLog.Write("JoyChromium: não instalado, abrindo a página de download");
        JoyChromiumService.OpenDownloadPage();
    }

    private async Task InstallJoyChromiumAsync()
    {
        JoyChromiumProgress = 0;
        JoyChromiumInstalling = true;
        AppLog.Write("JoyChromium: não instalado, baixando e instalando a pedido do menu");
        try
        {
            var progress = new Progress<double>(p =>
            {
                if (!JoyChromiumInstalling) return;   // a late report, after it ended
                if (double.IsPositiveInfinity(p)) JoyChromiumSetupRunning = true;
                else JoyChromiumProgress = p * 100;
            });
            await Task.Run(() => JoyChromiumService.InstallAsync(progress));
            AppLog.Write("JoyChromium: instalado");
            JoyChromiumInstalled = true;
            JoyChromiumJustInstalled = true;
            _ = LoadJoyChromiumLogoAsync();
        }
        catch (Exception ex)
        {
            AppLog.Write($"JoyChromium: instalar: {ex.Message}");
            JoyChromiumInstallFailed = true;
        }
        finally
        {
            JoyChromiumSetupRunning = false;
            JoyChromiumInstalling = false;
        }
        // Not opened from here, like ControlFS: the menu may have lost the foreground by now. The next A opens it.
    }
}
