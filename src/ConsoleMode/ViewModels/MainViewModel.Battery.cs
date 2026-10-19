using CommunityToolkit.Mvvm.ComponentModel;
using ConsoleMode.Services;
using Microsoft.UI.Dispatching;

namespace ConsoleMode.ViewModels;

// Controller battery (issue #70): the emptiest pad's level in the session menu header and in the
// console interface's top bar, and a corner notice during a session when a pad runs low.
public partial class MainViewModel
{
    private static readonly TimeSpan BatteryInterval = TimeSpan.FromSeconds(30);

    private DispatcherQueueTimer? _batteryTimer;
    private readonly Dictionary<string, LowBatteryAlarm> _batteryAlarms = new(StringComparer.OrdinalIgnoreCase);
    private bool _batteryReading;

    [ObservableProperty] private bool _hasControllerBattery;
    [ObservableProperty] private string _controllerBatteryGlyph = "";
    [ObservableProperty] private string _controllerBatteryText = "";
    [ObservableProperty] private string _controllerBatteryLabel = "";

    private void StartBatteryPolling()
    {
        if (_batteryTimer is not null) return;
        _batteryTimer = _dispatcher.CreateTimer();
        _batteryTimer.Interval = BatteryInterval;
        _batteryTimer.Tick += (_, _) => _ = RefreshBatteryAsync();
        _batteryTimer.Start();
        _ = RefreshBatteryAsync();
    }

    /// <summary>Reads the batteries off the UI thread, then updates the indicator and the warnings.</summary>
    private async Task RefreshBatteryAsync()
    {
        if (_batteryReading) return;
        _batteryReading = true;
        try
        {
            var readings = await Task.Run(() => ControllerBatteryMonitor.Refresh());
            ShowBattery(ControllerBattery.Lowest(readings));
            if (IsConsoleActive) CheckLowBattery(readings);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Controle: bateria: {ex.Message}");
        }
        finally
        {
            _batteryReading = false;
        }
    }

    private void ShowBattery(BatteryReading? reading)
    {
        HasControllerBattery = reading is not null;
        if (reading is null) return;
        ControllerBatteryGlyph = ControllerBattery.Glyph(reading.Percent, reading.Charging);
        ControllerBatteryText = ControllerBattery.Format(reading);
        ControllerBatteryLabel = LocalizationService.Get(reading.Charging ? "ControllerBatteryCharging" : "ControllerBattery", ControllerBatteryText);
    }

    private void CheckLowBattery(IReadOnlyList<BatteryReading> readings)
    {
        foreach (var reading in readings)
        {
            if (!_batteryAlarms.TryGetValue(reading.Id, out var alarm)) _batteryAlarms[reading.Id] = alarm = new LowBatteryAlarm();
            if (alarm.Update(reading.Percent, reading.Charging) is null) continue;
            AppLog.Write($"Controle: bateria fraca ({reading.Name}, {ControllerBattery.Format(reading)})");
            try
            {
                _ = new SessionHintWindow(LocalizationService.Get("LowBatteryTitle"),
                    LocalizationService.Get("LowBatteryBody", ControllerBattery.Format(reading)),
                    Engine.State.FocusMonitorRect, TimeSpan.FromSeconds(7),
                    ControllerBattery.Glyph(reading.Percent, false));
            }
            catch (Exception ex)
            {
                AppLog.Write($"Controle: aviso de bateria: {ex.Message}");
            }
        }
    }
}
