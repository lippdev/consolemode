using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConsoleMode.Services;
using Microsoft.UI.Xaml.Controls;

namespace ConsoleMode.ViewModels;

// Waking the PC from sleep with the controller (issue #155): shows which of the controller's
// adapters may wake it and turns the rest on (Windows asks for administrator rights).
public partial class MainViewModel
{
    private List<WakeDevice>? _wakeDevices;
    private bool _wakeChecked;

    [ObservableProperty] private string _controllerWakeStatus = "";
    /// <summary>Short value for the console row: on, the button's verb, or unavailable.</summary>
    [ObservableProperty] private string _controllerWakeValue = "";
    [ObservableProperty] private bool _canEnableControllerWake = true;

    private async Task RefreshControllerWakeAsync()
    {
        CanEnableControllerWake = false;
        ControllerWakeStatus = LocalizationService.Get("ControllerWakeChecking");
        ControllerWakeValue = ControllerWakeStatus;
        _wakeDevices = await Task.Run(ControllerWakeService.Read);
        _wakeChecked = true;
        CanEnableControllerWake = true;
        RefreshControllerWakeText();
    }

    private void RefreshControllerWakeText()
    {
        if (!_wakeChecked) return;
        if (_wakeDevices is null)
        {
            ControllerWakeStatus = LocalizationService.Get("ControllerWakeUnknown");
            ControllerWakeValue = LocalizationService.Get("ControllerWakeUnavailable");
            return;
        }
        if (_wakeDevices.Count == 0)
        {
            ControllerWakeStatus = LocalizationService.Get("ControllerWakeNone");
            ControllerWakeValue = LocalizationService.Get("ControllerWakeUnavailable");
            return;
        }
        var off = _wakeDevices.Where(d => !d.Armed).ToList();
        ControllerWakeStatus = off.Count == 0
            ? LocalizationService.Get("ControllerWakeReady", Names(_wakeDevices))
            : LocalizationService.Get("ControllerWakeOff", Names(off));
        ControllerWakeValue = LocalizationService.Get(off.Count == 0 ? "ToggleOn" : "ControllerWakeButton");
    }

    private static string Names(IEnumerable<WakeDevice> devices) =>
        string.Join(", ", devices.Select(d => d.Name).Distinct(StringComparer.CurrentCultureIgnoreCase).Take(3));

    /// <summary>Reads the devices again (an adapter may have been plugged in since) and turns on the ones still off.</summary>
    [RelayCommand]
    private async Task EnableControllerWakeAsync()
    {
        if (!CanEnableControllerWake) return;
        await RefreshControllerWakeAsync();
        var off = _wakeDevices?.Where(d => !d.Armed).ToList();
        if (off is not { Count: > 0 })
        {
            // Nothing to turn on: say why (the console row only shows a word).
            SetStatus(ControllerWakeStatus, _wakeDevices is { Count: > 0 } ? InfoBarSeverity.Success : InfoBarSeverity.Warning);
            return;
        }

        CanEnableControllerWake = false;
        var result = await Task.Run(() => ControllerWakeService.Enable(off));
        CanEnableControllerWake = true;
        await RefreshControllerWakeAsync();

        // PowerShell saying it's done isn't proof: read the devices again and check they really can wake the PC.
        if (result == ControllerWakeService.EnableResult.Done && ControllerWake.StillOff(off, _wakeDevices) is { Count: > 0 } stillOff)
        {
            AppLog.Write($"Acordar pelo controle: ainda desligado depois de ativar: {Names(stillOff)}");
            result = _wakeDevices is null ? ControllerWakeService.EnableResult.Failed : ControllerWakeService.EnableResult.NotFound;
        }

        switch (result)
        {
            case ControllerWakeService.EnableResult.Done:
                SetStatus(LocalizationService.Get("ControllerWakeEnabled"), InfoBarSeverity.Success);
                break;
            case ControllerWakeService.EnableResult.Canceled:
                SetStatus(LocalizationService.Get("ControllerWakeCanceled"), InfoBarSeverity.Warning);
                break;
            case ControllerWakeService.EnableResult.NotFound:
                SetStatus(LocalizationService.Get("ControllerWakeGone"), InfoBarSeverity.Warning);
                break;
            default:
                SetStatus(LocalizationService.Get("ControllerWakeFailed"), InfoBarSeverity.Error);
                break;
        }
    }
}
