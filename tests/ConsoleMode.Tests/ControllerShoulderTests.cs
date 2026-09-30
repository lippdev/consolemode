using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class ControllerShoulderTests
{
    // DS4 over USB: report 0x01, hat + face buttons at byte 5, L1/R1/Create/Options at byte 6.
    private static byte[] Ds4Usb(byte hatAndFace, byte shoulders)
    {
        var report = new byte[64];
        report[0] = 0x01;
        report[1] = report[2] = 128;   // stick centred
        report[5] = hatAndFace;
        report[6] = shoulders;
        return report;
    }

    [Fact]
    public void L1_and_R1_become_LB_and_RB()
    {
        Assert.Equal(0x0100, ControllerMapping.SonyButtons(Ds4Usb(0x08, 0x01), dualSense: false) & 0x0100);
        Assert.Equal(0x0200, ControllerMapping.SonyButtons(Ds4Usb(0x08, 0x02), dualSense: false) & 0x0200);
    }

    [Fact]
    public void Shoulders_do_not_leak_into_the_other_buttons()
    {
        var bits = ControllerMapping.SonyButtons(Ds4Usb(0x08, 0x03), dualSense: false);
        Assert.Equal(0x0300, bits);   // only LB + RB: no Back (0x20), Start (0x10), A/B/X/Y
    }

    [Fact]
    public void Nothing_pressed_reads_zero()
    {
        Assert.Equal(0, ControllerMapping.SonyButtons(Ds4Usb(0x08, 0x00), dualSense: false));
    }

    [Fact]
    public void Hid_pads_use_buttons_4_and_5_for_the_shoulders()
    {
        Assert.Equal(4, ControllerMapping.HidShoulderLeft);
        Assert.Equal(5, ControllerMapping.HidShoulderRight);
    }
}
