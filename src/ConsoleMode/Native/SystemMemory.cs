using System.Runtime.InteropServices;

namespace ConsoleMode.Native;

public static class SystemMemory
{
    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }

    [DllImport("kernel32.dll")]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx status);

    /// <summary>RAM in use and installed, in MB; nulls if Windows doesn't answer.</summary>
    public static (double? UsedMb, double? TotalMb) Read()
    {
        var status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        if (!GlobalMemoryStatusEx(ref status) || status.TotalPhys == 0) return (null, null);
        const double mb = 1024 * 1024;
        return ((status.TotalPhys - status.AvailPhys) / mb, status.TotalPhys / mb);
    }
}
