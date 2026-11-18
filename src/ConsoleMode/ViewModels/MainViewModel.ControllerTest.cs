using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConsoleMode.Services;
using Microsoft.UI.Dispatching;
using Windows.ApplicationModel.DataTransfer;

namespace ConsoleMode.ViewModels;

// "Test controller": live raw readings so a user whose pad is detected but silent can tell us
// exactly what Windows delivers (which source, which button indexes, or an exception).
public partial class MainViewModel
{
    private DispatcherQueueTimer? _controllerTestTimer;

    [ObservableProperty] private bool _isControllerTestOpen;
    [ObservableProperty] private string _controllerDevicesText = "";
    [ObservableProperty] private string _controllerSampleText = "";

    [RelayCommand]
    private void OpenControllerTest()
    {
        IsPickerOpen = false;
        IsRolePanelOpen = false;
        StartControllerTestReaders();
        ControllerDevicesText = DescribeControllerTestDevices();
        ControllerSampleText = "";
        _controllerTestTimer ??= _dispatcher.CreateTimer();
        _controllerTestTimer.Interval = TimeSpan.FromMilliseconds(50);
        _controllerTestTimer.Tick -= OnControllerTestTick;
        _controllerTestTimer.Tick += OnControllerTestTick;
        _controllerTestTimer.Start();
        IsControllerTestOpen = true;
    }

    [RelayCommand]
    private void CloseControllerTest()
    {
        _controllerTestTimer?.Stop();
        StopControllerTestReaders();
        IsControllerTestOpen = false;
    }

    private bool _controllerTestReading;

    /// <summary>
    /// While the test is open: every HID collection of the pads is read (to show which usage each button sends) and the
    /// battery is read once right away, so the list shows what each source says.
    /// </summary>
    private void StartControllerTestReaders()
    {
        if (_controllerTestReading) return;
        _controllerTestReading = true;
        HidHomeButtonReader.AcquireDiagnostics();
        _ = Task.Run(() =>
        {
            try { ControllerBatteryMonitor.Refresh(); }
            catch (Exception ex) { AppLog.Write($"Teste de controle: bateria: {ex.Message}"); }
        });
    }

    private void StopControllerTestReaders()
    {
        if (!_controllerTestReading) return;
        _controllerTestReading = false;
        HidHomeButtonReader.ReleaseDiagnostics();
    }

    /// <summary>The device list and the battery of each source.</summary>
    private static string DescribeControllerTestDevices() =>
        ControllerInput.DescribeDevices() + Environment.NewLine + ControllerBatteryMonitor.DescribeSources();

    /// <summary>The live sample, with what the pads' HID collections declare and the usages pressed right now.</summary>
    private static string SampleControllerTest()
    {
        var sample = ControllerInput.SampleDiagnostics();
        var hid = HidHomeButtonReader.Describe();
        return hid.Length == 0 ? sample : sample + Environment.NewLine + hid;
    }

    /// <summary>The desktop expander binds IsControllerTestOpen two-way: start/stop the timer with it.</summary>
    partial void OnIsControllerTestOpenChanged(bool value)
    {
        if (value && _controllerTestTimer?.IsRunning != true) OpenControllerTest();
        else if (!value)
        {
            _controllerTestTimer?.Stop();
            StopControllerTestReaders();
        }
    }

    private int _controllerTestTicks;

    private void OnControllerTestTick(DispatcherQueueTimer sender, object args)
    {
        ControllerSampleText = SampleControllerTest();
        // The device list changes rarely; refresh it about once a second.
        if (++_controllerTestTicks % 20 == 0) ControllerDevicesText = DescribeControllerTestDevices();
    }

    [RelayCommand]
    private void CopyControllerDiagnostics()
    {
        var text = new StringBuilder()
            .AppendLine($"Console Mode {UpdateService.CurrentVersion} · {Environment.OSVersion.Version}")
            .AppendLine("Dispositivos:")
            .AppendLine(ControllerDevicesText)
            .AppendLine("Amostra:")
            .AppendLine(ControllerSampleText)
            .ToString();
        try
        {
            var package = new DataPackage();
            package.SetText(text);
            Clipboard.SetContent(package);
            SetStatus(LocalizationService.Get("DiagnosticsCopied"), Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Diagnóstico: {ex.Message}");
        }
    }
}
