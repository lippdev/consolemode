using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace ConsoleMode.Services;

/// <summary>
/// Reads DS4/DualSense buttons straight from HID so the session chords work while a game has
/// focus. XInput never sees a Sony pad without Steam Input or DS4Windows, and
/// Windows.Gaming.Input only delivers input to the foreground window. Opened shared, so Steam
/// and the game keep reading the pad too.
/// </summary>
public static class SonyHidReader
{
    private static readonly object Gate = new();
    private static readonly Dictionary<string, ushort> Buttons = new(StringComparer.OrdinalIgnoreCase);
    private static CancellationTokenSource? _cts;
    private static int _users;

    /// <summary>XInput-style bits held on any Sony pad (see ControllerMapping.SonyButtons).</summary>
    public static ushort Held
    {
        get
        {
            lock (Gate)
            {
                ushort bits = 0;
                foreach (var b in Buttons.Values) bits |= b;
                return bits;
            }
        }
    }

    public static void Acquire()
    {
        lock (Gate)
        {
            if (_users++ > 0) return;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _ = Task.Run(() => ScanLoopAsync(token));
        }
    }

    public static void Release()
    {
        lock (Gate)
        {
            if (_users == 0 || --_users > 0) return;
            _cts?.Cancel();
            _cts = null;
            Buttons.Clear();
        }
    }

    /// <summary>Picks up pads as they connect; each open pad gets its own read loop.</summary>
    private static async Task ScanLoopAsync(CancellationToken ct)
    {
        var reading = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (!ct.IsCancellationRequested)
        {
            try
            {
                foreach (var path in EnumerateHidPaths())
                {
                    lock (reading) { if (reading.Contains(path)) continue; }
                    if (TryOpenSony(path) is not { } pad) continue;
                    lock (reading) { reading.Add(path); }
                    AppLog.Write($"Controle: lendo PlayStation via HID para os atalhos ({(pad.DualSense ? "DualSense" : "DualShock 4")})");
                    _ = Task.Run(async () =>
                    {
                        await ReadLoopAsync(path, pad, ct);
                        lock (Gate) { Buttons.Remove(path); }
                        lock (reading) { reading.Remove(path); }
                    });
                }
            }
            catch (Exception ex)
            {
                AppLog.Write($"Controle: HID: {ex.Message}");
            }
            try { await Task.Delay(3000, ct); } catch (OperationCanceledException) { break; }
        }
    }

    private sealed record Pad(SafeFileHandle Handle, bool DualSense, int ReportLength);

    private static async Task ReadLoopAsync(string path, Pad pad, CancellationToken ct)
    {
        try
        {
            await using var stream = new FileStream(pad.Handle, FileAccess.Read, 0, isAsync: true);
            var buffer = new byte[Math.Max(pad.ReportLength, 64)];
            while (!ct.IsCancellationRequested)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, pad.ReportLength), ct);
                if (read <= 0) break;
                var bits = ControllerMapping.SonyButtons(buffer.AsSpan(0, read), pad.DualSense);
                lock (Gate) { Buttons[path] = bits; }
            }
        }
        catch (Exception ex) when (ex is IOException or OperationCanceledException or ObjectDisposedException)
        {
            // Unplugged, or the watch stopped: the scan loop reopens the pad if it comes back.
        }
        finally
        {
            pad.Handle.Dispose();
        }
    }

    private static Pad? TryOpenSony(string path)
    {
        var handle = CreateFile(path, GenericRead, FileShareRead | FileShareWrite, 0, OpenExisting, FileFlagOverlapped, 0);
        if (handle.IsInvalid) { handle.Dispose(); return null; }
        var attributes = new HiddAttributes { Size = Marshal.SizeOf<HiddAttributes>() };
        if (!HidD_GetAttributes(handle, ref attributes) || attributes.VendorId != ControllerMapping.SonyVendorId)
        {
            handle.Dispose();
            return null;
        }
        // Only gamepads (usage page 1, joystick or gamepad): Sony also ships headsets and other HID devices.
        if (!HidD_GetPreparsedData(handle, out var preparsed)) { handle.Dispose(); return null; }
        var ok = HidP_GetCaps(preparsed, out var caps) == HidpStatusSuccess;
        HidD_FreePreparsedData(preparsed);
        if (!ok || caps.UsagePage != 0x01 || caps.Usage is not (0x04 or 0x05) || caps.InputReportByteLength == 0)
        {
            handle.Dispose();
            return null;
        }
        return new Pad(handle, ControllerMapping.IsDualSense(attributes.ProductId), caps.InputReportByteLength);
    }

    private static IEnumerable<string> EnumerateHidPaths()
    {
        HidD_GetHidGuid(out var guid);
        var set = SetupDiGetClassDevs(ref guid, 0, 0, DigcfPresent | DigcfDeviceInterface);
        if (set == -1) yield break;
        try
        {
            var data = new SpDeviceInterfaceData { Size = Marshal.SizeOf<SpDeviceInterfaceData>() };
            for (uint i = 0; SetupDiEnumDeviceInterfaces(set, 0, ref guid, i, ref data); i++)
            {
                SetupDiGetDeviceInterfaceDetail(set, ref data, 0, 0, out var needed, 0);
                if (needed == 0) continue;
                var detail = Marshal.AllocHGlobal((int)needed);
                try
                {
                    // cbSize of SP_DEVICE_INTERFACE_DETAIL_DATA_W: 8 on x64, 6 on x86.
                    Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6);
                    if (!SetupDiGetDeviceInterfaceDetail(set, ref data, detail, needed, out _, 0)) continue;
                    var path = Marshal.PtrToStringUni(detail + 4);
                    if (!string.IsNullOrEmpty(path)) yield return path;
                }
                finally
                {
                    Marshal.FreeHGlobal(detail);
                }
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(set);
        }
    }

    private const uint GenericRead = 0x80000000;
    private const uint FileShareRead = 0x1;
    private const uint FileShareWrite = 0x2;
    private const uint OpenExisting = 3;
    private const uint FileFlagOverlapped = 0x40000000;
    private const uint DigcfPresent = 0x2;
    private const uint DigcfDeviceInterface = 0x10;
    private const int HidpStatusSuccess = 0x00110000;

    [StructLayout(LayoutKind.Sequential)]
    private struct HiddAttributes
    {
        public int Size;
        public ushort VendorId;
        public ushort ProductId;
        public ushort VersionNumber;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HidpCaps
    {
        public ushort Usage;
        public ushort UsagePage;
        public ushort InputReportByteLength;
        public ushort OutputReportByteLength;
        public ushort FeatureReportByteLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)] public ushort[] Reserved;
        public ushort NumberLinkCollectionNodes;
        public ushort NumberInputButtonCaps;
        public ushort NumberInputValueCaps;
        public ushort NumberInputDataIndices;
        public ushort NumberOutputButtonCaps;
        public ushort NumberOutputValueCaps;
        public ushort NumberOutputDataIndices;
        public ushort NumberFeatureButtonCaps;
        public ushort NumberFeatureValueCaps;
        public ushort NumberFeatureDataIndices;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SpDeviceInterfaceData
    {
        public int Size;
        public Guid InterfaceClassGuid;
        public uint Flags;
        public nint Reserved;
    }

    [DllImport("hid.dll")]
    private static extern void HidD_GetHidGuid(out Guid guid);

    [DllImport("hid.dll")]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool HidD_GetAttributes(SafeFileHandle device, ref HiddAttributes attributes);

    [DllImport("hid.dll")]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool HidD_GetPreparsedData(SafeFileHandle device, out nint preparsed);

    [DllImport("hid.dll")]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool HidD_FreePreparsedData(nint preparsed);

    [DllImport("hid.dll")]
    private static extern int HidP_GetCaps(nint preparsed, out HidpCaps caps);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode)]
    private static extern nint SetupDiGetClassDevs(ref Guid classGuid, nint enumerator, nint parent, uint flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiEnumDeviceInterfaces(nint set, nint deviceInfo, ref Guid classGuid, uint index, ref SpDeviceInterfaceData data);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiGetDeviceInterfaceDetail(nint set, ref SpDeviceInterfaceData data, nint detail, uint detailSize, out uint requiredSize, nint deviceInfo);

    [DllImport("setupapi.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiDestroyDeviceInfoList(nint set);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(string name, uint access, uint share, nint security, uint creation, uint flags, nint template);
}
