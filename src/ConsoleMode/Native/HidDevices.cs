using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace ConsoleMode.Native;

/// <summary>What a HID collection says about itself, read without opening it for input.</summary>
internal sealed record HidInfo(string Path, ushort VendorId, ushort ProductId, ushort UsagePage, ushort Usage, int InputReportLength)
{
    /// <summary>Usage page 1 (Generic Desktop), joystick or gamepad: a game controller, not a keyboard, a headset or a mouse.</summary>
    public bool IsGameController => UsagePage == 0x01 && Usage is 0x04 or 0x05;
}

/// <summary>
/// Lists the HID collections Windows knows, and walks the device tree above one of them. Opening a collection
/// "for query" (no read access) works even while XInput, Steam or a game has it open.
/// </summary>
internal static class HidDevices
{
    /// <summary>Every present HID collection, as a device interface path.</summary>
    public static IEnumerable<string> EnumeratePaths()
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

    /// <summary>Vendor, product and top-level usage of a collection; null when it can't be queried.</summary>
    public static HidInfo? Query(string path)
    {
        using var handle = CreateFile(path, 0, FileShareRead | FileShareWrite, 0, OpenExisting, 0, 0);
        if (handle.IsInvalid) return null;
        var attributes = new HiddAttributes { Size = Marshal.SizeOf<HiddAttributes>() };
        if (!HidD_GetAttributes(handle, ref attributes)) return null;
        if (!HidD_GetPreparsedData(handle, out var preparsed)) return null;
        try
        {
            if (HidP_GetCaps(preparsed, out var caps) != HidpStatusSuccess) return null;
            return new HidInfo(path, attributes.VendorId, attributes.ProductId, caps.UsagePage, caps.Usage, caps.InputReportByteLength);
        }
        finally
        {
            HidD_FreePreparsedData(preparsed);
        }
    }

    /// <summary>The device node behind a collection's interface path (0 when Windows doesn't know it).</summary>
    public static uint DevNodeOf(string interfacePath)
    {
        var key = DevPKeyInstanceId;
        var id = GetInterfaceString(interfacePath, ref key);
        if (string.IsNullOrEmpty(id)) return 0;
        return CM_Locate_DevNodeW(out var node, id, 0) == CrSuccess ? node : 0;
    }

    /// <summary>The parent of a device node, or 0 at the top.</summary>
    public static uint ParentOf(uint node) => CM_Get_Parent(out var parent, node, 0) == CrSuccess ? parent : 0;

    /// <summary>A one-byte property of a device node, or null when the node doesn't have it.</summary>
    public static byte? GetByte(uint node, Guid fmtid, uint pid)
    {
        var key = new DevPropKey { FmtId = fmtid, Pid = pid };
        var buffer = new byte[1];
        uint size = 1;
        if (CM_Get_DevNode_PropertyW(node, ref key, out var type, buffer, ref size, 0) != CrSuccess) return null;
        return type == DevPropTypeByte && size == 1 ? buffer[0] : null;
    }

    /// <summary>The name Windows shows for a device node (Device Manager, Settings → Bluetooth).</summary>
    public static string? GetName(uint node)
    {
        var friendly = DevPKeyFriendlyName;
        var name = GetString(node, ref friendly);
        if (!string.IsNullOrWhiteSpace(name)) return name;
        var description = DevPKeyDeviceDesc;
        name = GetString(node, ref description);
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    /// <summary>The device instance id of a node ("BTHLE\DEV_..."), for logs and stable keys.</summary>
    public static string? GetInstanceId(uint node)
    {
        var key = DevPKeyInstanceId;
        return GetString(node, ref key);
    }

    private static string? GetString(uint node, ref DevPropKey key)
    {
        uint size = 0;
        var status = CM_Get_DevNode_PropertyW(node, ref key, out _, null, ref size, 0);
        if (status != CrBufferSmall || size == 0) return null;
        var buffer = new byte[size];
        if (CM_Get_DevNode_PropertyW(node, ref key, out var type, buffer, ref size, 0) != CrSuccess || type != DevPropTypeString) return null;
        return System.Text.Encoding.Unicode.GetString(buffer, 0, (int)size).TrimEnd('\0');
    }

    private static string? GetInterfaceString(string interfacePath, ref DevPropKey key)
    {
        uint size = 0;
        var status = CM_Get_Device_Interface_PropertyW(interfacePath, ref key, out _, null, ref size, 0);
        if (status != CrBufferSmall || size == 0) return null;
        var buffer = new byte[size];
        if (CM_Get_Device_Interface_PropertyW(interfacePath, ref key, out var type, buffer, ref size, 0) != CrSuccess || type != DevPropTypeString) return null;
        return System.Text.Encoding.Unicode.GetString(buffer, 0, (int)size).TrimEnd('\0');
    }

    private static readonly DevPropKey DevPKeyInstanceId = new() { FmtId = new Guid("78c34fc8-104a-4aca-9ea4-524d52996e57"), Pid = 256 };
    private static readonly DevPropKey DevPKeyFriendlyName = new() { FmtId = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), Pid = 14 };
    private static readonly DevPropKey DevPKeyDeviceDesc = new() { FmtId = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), Pid = 2 };

    private const uint FileShareRead = 0x1;
    private const uint FileShareWrite = 0x2;
    private const uint OpenExisting = 3;
    private const uint DigcfPresent = 0x2;
    private const uint DigcfDeviceInterface = 0x10;
    private const int HidpStatusSuccess = 0x00110000;
    private const int CrSuccess = 0x00;
    private const int CrBufferSmall = 0x1A;
    private const uint DevPropTypeByte = 0x03;
    private const uint DevPropTypeString = 0x12;

    [StructLayout(LayoutKind.Sequential)]
    private struct DevPropKey
    {
        public Guid FmtId;
        public uint Pid;
    }

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

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    private static extern int CM_Get_Device_Interface_PropertyW(string deviceInterface, ref DevPropKey key, out uint type, byte[]? buffer, ref uint size, uint flags);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    private static extern int CM_Locate_DevNodeW(out uint devInst, string deviceId, uint flags);

    [DllImport("cfgmgr32.dll")]
    private static extern int CM_Get_Parent(out uint parent, uint devInst, uint flags);

    [DllImport("cfgmgr32.dll")]
    private static extern int CM_Get_DevNode_PropertyW(uint devInst, ref DevPropKey key, out uint type, byte[]? buffer, ref uint size, uint flags);
}
