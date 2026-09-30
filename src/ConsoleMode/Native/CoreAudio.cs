using System.Runtime.InteropServices;

namespace ConsoleMode.Native;

/// <summary>
/// Windows Core Audio (MMDevice API) for the output devices: list, switch the default, enable a
/// disabled one, volume and mute (issue #91).
/// Switching the default output uses IPolicyConfig, which Windows doesn't document but has kept
/// stable since Windows 7; the Control Panel, EarTrumpet and AudioSwitcher use it.
/// </summary>
internal static class CoreAudio
{
    public sealed record Endpoint(string Id, string Name, string AdapterName, bool IsActive, bool IsDefault);

    private const int ERender = 0;
    private const int EConsole = 0, EMultimedia = 1, ECommunications = 2;
    private const int DeviceStateActive = 0x1, DeviceStateDisabled = 0x2, DeviceStateUnplugged = 0x8;
    private const int StgmRead = 0;
    private const int ClsctxAll = 0x17;
    private const ushort VtLpwstr = 31;

    // PKEY_Device_DeviceDesc: the endpoint's own name ("Speakers", "LG TV").
    private static readonly PropertyKey DeviceDescKey = new(new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), 2);
    // PKEY_DeviceInterface_FriendlyName: the adapter ("Realtek(R) Audio", "NVIDIA High Definition Audio").
    private static readonly PropertyKey InterfaceFriendlyNameKey = new(new Guid("026e516e-b814-414b-83cd-856d6fef4822"), 2);

    /// <summary>Active, disabled and unplugged outputs.</summary>
    public static List<Endpoint> ListRenderEndpoints()
    {
        var result = new List<Endpoint>();
        var enumerator = CreateEnumerator();
        try
        {
            var defaultId = TryGetDefaultId(enumerator);
            var collection = enumerator.EnumAudioEndpoints(ERender, DeviceStateActive | DeviceStateDisabled | DeviceStateUnplugged);
            try
            {
                var count = collection.GetCount();
                for (var i = 0; i < count; i++)
                {
                    var device = collection.Item(i);
                    try
                    {
                        var id = device.GetId();
                        var state = device.GetState();
                        var store = device.OpenPropertyStore(StgmRead);
                        try
                        {
                            result.Add(new Endpoint(
                                id,
                                GetString(store, DeviceDescKey),
                                GetString(store, InterfaceFriendlyNameKey),
                                state == DeviceStateActive,
                                string.Equals(id, defaultId, StringComparison.OrdinalIgnoreCase)));
                        }
                        finally { Release(store); }
                    }
                    finally { Release(device); }
                }
            }
            finally { Release(collection); }
        }
        finally { Release(enumerator); }
        return result;
    }

    /// <summary>Makes the output the default for every role (console, multimedia, communications).</summary>
    public static void SetDefault(string endpointId)
    {
        var policy = CreatePolicyConfig();
        try
        {
            foreach (var role in new[] { EConsole, EMultimedia, ECommunications })
                Marshal.ThrowExceptionForHR(policy.SetDefaultEndpoint(endpointId, role));
        }
        finally { Release(policy); }
    }

    /// <summary>Enables an output disabled in Sound settings.</summary>
    public static void Enable(string endpointId)
    {
        var policy = CreatePolicyConfig();
        try { Marshal.ThrowExceptionForHR(policy.SetEndpointVisibility(endpointId, 1)); }
        finally { Release(policy); }
    }

    /// <summary>Master volume from 0 to 100.</summary>
    public static int GetVolumePercent(string endpointId) =>
        WithVolume(endpointId, v => (int)Math.Round(v.GetMasterVolumeLevelScalar() * 100));

    public static void SetVolumePercent(string endpointId, int percent) =>
        WithVolume(endpointId, v =>
        {
            var context = Guid.Empty;
            v.SetMasterVolumeLevelScalar(Math.Clamp(percent, 0, 100) / 100f, ref context);
            return 0;
        });

    public static void SetMute(string endpointId, bool mute) =>
        WithVolume(endpointId, v =>
        {
            var context = Guid.Empty;
            v.SetMute(mute, ref context);
            return 0;
        });

    private static T WithVolume<T>(string endpointId, Func<IAudioEndpointVolume, T> action)
    {
        var enumerator = CreateEnumerator();
        try
        {
            var device = enumerator.GetDevice(endpointId);
            try
            {
                var iid = typeof(IAudioEndpointVolume).GUID;
                var volume = (IAudioEndpointVolume)device.Activate(ref iid, ClsctxAll, IntPtr.Zero);
                try { return action(volume); }
                finally { Release(volume); }
            }
            finally { Release(device); }
        }
        finally { Release(enumerator); }
    }

    private static string? TryGetDefaultId(IMMDeviceEnumerator enumerator)
    {
        try
        {
            var device = enumerator.GetDefaultAudioEndpoint(ERender, EConsole);
            try { return device.GetId(); }
            finally { Release(device); }
        }
        catch (COMException)
        {
            return null; // no output at all
        }
    }

    private static string GetString(IPropertyStore store, PropertyKey key)
    {
        var value = default(PropVariant);
        try
        {
            store.GetValue(ref key, out value);
            return value.Type == VtLpwstr && value.Pointer != IntPtr.Zero ? Marshal.PtrToStringUni(value.Pointer) ?? "" : "";
        }
        finally { PropVariantClear(ref value); }
    }

    private static IMMDeviceEnumerator CreateEnumerator() => (IMMDeviceEnumerator)new MMDeviceEnumeratorCom();

    private static IPolicyConfig CreatePolicyConfig() => (IPolicyConfig)new PolicyConfigClientCom();

    private static void Release(object? comObject)
    {
        if (comObject is not null && Marshal.IsComObject(comObject)) Marshal.ReleaseComObject(comObject);
    }

    [DllImport("ole32.dll")]
    private static extern int PropVariantClear(ref PropVariant value);

    [StructLayout(LayoutKind.Sequential)]
    private struct PropertyKey(Guid formatId, int propertyId)
    {
        public Guid FormatId = formatId;
        public int PropertyId = propertyId;
    }

    /// <summary>Just enough of PROPVARIANT to read strings (24 bytes on 64-bit, 16 on 32-bit).</summary>
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    private struct PropVariant
    {
        [FieldOffset(0)] public ushort Type;
        [FieldOffset(8)] public IntPtr Pointer;
    }

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumeratorCom { }

    [ComImport, Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
    private class PolicyConfigClientCom { }

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        IMMDeviceCollection EnumAudioEndpoints(int dataFlow, int stateMask);
        IMMDevice GetDefaultAudioEndpoint(int dataFlow, int role);
        IMMDevice GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id);
        void RegisterEndpointNotificationCallback(IntPtr client);
        void UnregisterEndpointNotificationCallback(IntPtr client);
    }

    [ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceCollection
    {
        int GetCount();
        IMMDevice Item(int index);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object Activate(ref Guid iid, int clsCtx, IntPtr activationParams);
        IPropertyStore OpenPropertyStore(int access);
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetId();
        int GetState();
    }

    [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        int GetCount();
        PropertyKey GetAt(int index);
        void GetValue(ref PropertyKey key, out PropVariant value);
        void SetValue(ref PropertyKey key, ref PropVariant value);
        void Commit();
    }

    [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        void RegisterControlChangeNotify(IntPtr notify);
        void UnregisterControlChangeNotify(IntPtr notify);
        uint GetChannelCount();
        void SetMasterVolumeLevel(float levelDb, ref Guid eventContext);
        void SetMasterVolumeLevelScalar(float level, ref Guid eventContext);
        float GetMasterVolumeLevel();
        float GetMasterVolumeLevelScalar();
        void SetChannelVolumeLevel(uint channel, float levelDb, ref Guid eventContext);
        void SetChannelVolumeLevelScalar(uint channel, float level, ref Guid eventContext);
        float GetChannelVolumeLevel(uint channel);
        float GetChannelVolumeLevelScalar(uint channel);
        void SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, ref Guid eventContext);
        [return: MarshalAs(UnmanagedType.Bool)]
        bool GetMute();
    }

    /// <summary>
    /// Only the last two methods are called; the ones before them hold their place in the vtable,
    /// so their parameters don't matter.
    /// </summary>
    [ComImport, Guid("F8679F50-850A-41CF-9C72-430F290290C8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPolicyConfig
    {
        [PreserveSig] int GetMixFormat(IntPtr a, IntPtr b);
        [PreserveSig] int GetDeviceFormat(IntPtr a, int b, IntPtr c);
        [PreserveSig] int ResetDeviceFormat(IntPtr a);
        [PreserveSig] int SetDeviceFormat(IntPtr a, IntPtr b, IntPtr c);
        [PreserveSig] int GetProcessingPeriod(IntPtr a, int b, IntPtr c, IntPtr d);
        [PreserveSig] int SetProcessingPeriod(IntPtr a, IntPtr b);
        [PreserveSig] int GetShareMode(IntPtr a, IntPtr b);
        [PreserveSig] int SetShareMode(IntPtr a, IntPtr b);
        [PreserveSig] int GetPropertyValue(IntPtr a, IntPtr b, IntPtr c);
        [PreserveSig] int SetPropertyValue(IntPtr a, IntPtr b, IntPtr c);
        [PreserveSig] int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string deviceId, int role);
        [PreserveSig] int SetEndpointVisibility([MarshalAs(UnmanagedType.LPWStr)] string deviceId, int visible);
    }
}
