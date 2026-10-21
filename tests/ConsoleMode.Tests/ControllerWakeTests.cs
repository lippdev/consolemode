using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class ControllerWakeTests
{
    [Theory]
    [InlineData(@"USB\VID_045E&PID_02FE\6&1A2B3C&0&3_0", "XboxWirelessAdapter", WakeDeviceKind.XboxAdapter)]
    [InlineData(@"USB\VID_045E&PID_02E6\5&ABC&0&2_0", "Net", WakeDeviceKind.XboxAdapter)]
    [InlineData(@"USB\VID_8087&PID_0033\5&2A&0&10_0", "Bluetooth", WakeDeviceKind.Bluetooth)]
    [InlineData(@"PCI\VEN_8086&DEV_51F1\3&11583659&1&A3_0", "Bluetooth", WakeDeviceKind.Bluetooth)]
    [InlineData(@"BTHENUM\{00001124-0000-1000-8000-00805F9B34FB}_VID&0002045E_PID&0B13\8&1_0", "Bluetooth", WakeDeviceKind.None)]
    [InlineData(@"HID\VID_045E&PID_0B12&IG_00\8&2A&0&0000_0", "HIDClass", WakeDeviceKind.Controller)]
    [InlineData(@"USB\VID_045E&PID_0B12\3032363030303536_0", "XnaComposite", WakeDeviceKind.Controller)]
    [InlineData(@"USB\VID_054C&PID_0CE6&MI_03\7&1_0", "HIDClass", WakeDeviceKind.Controller)]
    [InlineData(@"USB\VID_2DC8&PID_3106\8&1_0", "HIDClass", WakeDeviceKind.Controller)]
    [InlineData(@"HID\VID_046D&PID_C547&MI_00\A&26C66045&0&0000_0", "Mouse", WakeDeviceKind.None)]
    [InlineData(@"HID\VID_045E&PID_07B2&MI_00\8&1_0", "Keyboard", WakeDeviceKind.None)]
    [InlineData(@"PCI\VEN_10EC&DEV_8168&SUBSYS_E0001458&REV_16\1CF_0", "Net", WakeDeviceKind.None)]
    [InlineData("", "Bluetooth", WakeDeviceKind.None)]
    public void Devices_are_told_apart_by_instance_id_and_class(string instance, string pnpClass, WakeDeviceKind expected) =>
        Assert.Equal(expected, ControllerWake.Classify(instance, pnpClass));

    [Fact]
    public void Only_controller_devices_are_kept_adapter_first()
    {
        var devices = new[]
        {
            new WakeDevice(@"HID\VID_046D&PID_C547&MI_00\A_0", "Mouse", "Mouse", true),
            new WakeDevice(@"USB\VID_8087&PID_0033\5_0", "Intel Bluetooth", "Bluetooth", false),
            new WakeDevice(@"USB\VID_045E&PID_02FE\6_0", "Xbox Wireless Adapter", "XboxWirelessAdapter", false),
        };
        var relevant = ControllerWake.Relevant(devices);
        Assert.Equal(["Xbox Wireless Adapter", "Intel Bluetooth"], relevant.Select(d => d.Name));
    }

    [Fact]
    public void The_query_output_is_read_as_a_list_or_a_single_object()
    {
        const string many = """[{"InstanceName":"USB\\VID_8087&PID_0033\\5_0","Name":"Intel Bluetooth","PnpClass":"Bluetooth","Enable":true},{"InstanceName":"PCI\\VEN_10EC\\1_0","Name":null,"PnpClass":null,"Enable":false}]""";
        var list = ControllerWake.Parse(many);
        Assert.Equal(2, list.Count);
        Assert.True(list[0].Armed);
        Assert.Equal(@"PCI\VEN_10EC\1", list[1].Name);
        Assert.False(list[1].Armed);

        const string one = """{"InstanceName":"USB\\VID_045E&PID_02FE\\6_0","Name":"Xbox Wireless Adapter","PnpClass":"XboxWirelessAdapter","Enable":false}""";
        Assert.Single(ControllerWake.Parse(one));
        Assert.Empty(ControllerWake.Parse(""));
        Assert.Empty(ControllerWake.Parse("[]"));
    }

    [Theory]
    [InlineData(@"USB\VID_8087&PID_0033\5&2A&0&10_0", @"USB\VID_8087&PID_0033\5&2A&0&10")]
    [InlineData(@"USB\X\Y_12", @"USB\X\Y")]
    [InlineData(@"USB\X\Y_ab", @"USB\X\Y_ab")]
    [InlineData(@"USB\X\Y", @"USB\X\Y")]
    public void The_wmi_suffix_is_dropped_from_the_device_id(string instance, string expected) =>
        Assert.Equal(expected, ControllerWake.DeviceId(instance));

    [Fact]
    public void The_enable_script_quotes_every_id()
    {
        var script = ControllerWake.EnableScript([@"USB\VID_045E&PID_02FE\6_0", "it's"]);
        Assert.Contains(@"$ids = @('USB\VID_045E&PID_02FE\6_0','it''s')", script);
        Assert.Contains("Set-CimInstance -Property @{ Enable = $true }", script);
    }

    [Fact]
    public void Scripts_are_encoded_for_powershell_as_utf16() =>
        Assert.Equal("dwBoAG8AYQBtAGkA", ControllerWake.Encode("whoami"));
}
