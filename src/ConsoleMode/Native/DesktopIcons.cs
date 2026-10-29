using System.Runtime.InteropServices;
using ConsoleMode.Services;

namespace ConsoleMode.Native;

/// <summary>
/// Reads and moves the desktop icons through the shell's own view of the desktop
/// (IShellWindows → the desktop's IShellBrowser → IFolderView), the documented route instead of
/// poking at Explorer's list view from another process (issue #30).
/// Each call runs on its own STA thread: the shell objects live in Explorer and want one.
/// </summary>
internal static class DesktopIcons
{
    /// <summary>Every icon and its position, or null when they can't be read or Windows arranges them itself.</summary>
    public static List<DesktopIcon>? Read() => OnSta(() =>
    {
        using var desktop = DesktopView.Open();
        if (desktop is null) return null;
        if (desktop.View.GetAutoArrange() == 0)
        {
            AppLog.Write("Ícones da área de trabalho: organização automática ligada; posições não salvas");
            return null;
        }
        var icons = new List<DesktopIcon>();
        foreach (var (name, pidl) in desktop.Items())
        {
            if (desktop.View.GetItemPosition(pidl, out var point) == 0)
                icons.Add(new DesktopIcon(name, point.X, point.Y));
        }
        return icons;
    }, null);

    /// <summary>Puts the given icons where they were. Returns how many moved, or -1 on failure.</summary>
    public static int Move(IReadOnlyList<DesktopIcon> icons) => OnSta(() =>
    {
        if (icons.Count == 0) return 0;
        using var desktop = DesktopView.Open();
        if (desktop is null || desktop.View.GetAutoArrange() == 0) return -1;
        var wanted = new Dictionary<string, DesktopIcon>(StringComparer.OrdinalIgnoreCase);
        foreach (var icon in icons) wanted[icon.Name] = icon;

        var moved = 0;
        foreach (var (name, pidl) in desktop.Items())
        {
            if (!wanted.TryGetValue(name, out var icon)) continue;
            var hr = desktop.View.SelectAndPositionItems(1, [pidl], [new Point { X = icon.X, Y = icon.Y }], SvsiPositionItem);
            if (hr == 0) moved++;
        }
        return moved;
    }, -1);

    private static T OnSta<T>(Func<T> work, T fallback)
    {
        var result = fallback;
        var thread = new Thread(() =>
        {
            try { result = work(); }
            catch (Exception ex) { AppLog.Write($"Ícones da área de trabalho: {ex.Message}"); }
        }) { IsBackground = true, Name = "DesktopIcons" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        if (thread.Join(TimeSpan.FromSeconds(10))) return result;
        AppLog.Write("Ícones da área de trabalho: o Explorer não respondeu");
        return fallback;
    }

    /// <summary>The desktop's folder view and folder; frees every shell object and item id it hands out.</summary>
    private sealed class DesktopView : IDisposable
    {
        public IFolderView View { get; }
        private readonly IShellFolder _folder;
        private readonly List<object> _comObjects;
        private readonly List<nint> _pidls = [];

        private DesktopView(IFolderView view, IShellFolder folder, List<object> comObjects)
        {
            View = view;
            _folder = folder;
            _comObjects = comObjects;
        }

        public static DesktopView? Open()
        {
            var objects = new List<object>();
            try
            {
                var type = Type.GetTypeFromCLSID(ClsidShellWindows, throwOnError: true)!;
                var windows = (IShellWindows)Activator.CreateInstance(type)!;
                objects.Add(windows);

                object location = CsidlDesktop;
                object root = null!;
                if (windows.FindWindowSW(ref location, ref root, SwcDesktop, out _, SwfoNeedDispatch, out var dispatch) != 0 || dispatch is null)
                {
                    AppLog.Write("Ícones da área de trabalho: a área de trabalho do Explorer não foi encontrada");
                    Release(objects);
                    return null;
                }
                objects.Add(dispatch);

                var service = SidTopLevelBrowser;
                var browserId = typeof(IShellBrowser).GUID;
                Marshal.ThrowExceptionForHR(((IComServiceProvider)dispatch).QueryService(ref service, ref browserId, out var browserObject));
                objects.Add(browserObject);
                Marshal.ThrowExceptionForHR(((IShellBrowser)browserObject).QueryActiveShellView(out var viewObject));
                objects.Add(viewObject);
                var view = (IFolderView)viewObject;

                var folderId = typeof(IShellFolder).GUID;
                Marshal.ThrowExceptionForHR(view.GetFolder(ref folderId, out var folderObject));
                objects.Add(folderObject);
                return new DesktopView(view, (IShellFolder)folderObject, objects);
            }
            catch
            {
                Release(objects);
                throw;
            }
        }

        /// <summary>Each icon's parsing name with its item id (valid until Dispose).</summary>
        public IEnumerable<(string Name, nint Pidl)> Items()
        {
            if (View.ItemCount(SvgioAllView, out var count) != 0) yield break;
            for (var i = 0; i < count; i++)
            {
                if (View.Item(i, out var pidl) != 0 || pidl == 0) continue;
                _pidls.Add(pidl);
                var name = ParsingName(pidl);
                if (!string.IsNullOrWhiteSpace(name)) yield return (name, pidl);
            }
        }

        private string? ParsingName(nint pidl)
        {
            var strret = Marshal.AllocCoTaskMem(StrretSize);
            try
            {
                if (_folder.GetDisplayNameOf(pidl, ShgdnForParsing, strret) != 0) return null;
                if (StrRetToStrW(strret, pidl, out var text) != 0 || text == 0) return null;
                try { return Marshal.PtrToStringUni(text); }
                finally { Marshal.FreeCoTaskMem(text); }
            }
            finally { Marshal.FreeCoTaskMem(strret); }
        }

        public void Dispose()
        {
            foreach (var pidl in _pidls) Marshal.FreeCoTaskMem(pidl);
            _pidls.Clear();
            Release(_comObjects);
        }

        private static void Release(List<object> objects)
        {
            for (var i = objects.Count - 1; i >= 0; i--)
            {
                try { if (Marshal.IsComObject(objects[i])) Marshal.ReleaseComObject(objects[i]); }
                catch { /* already gone */ }
            }
            objects.Clear();
        }
    }

    private static readonly Guid ClsidShellWindows = new("9BA05972-F6A8-11CF-A442-00A0C90A8F39");
    private static readonly Guid SidTopLevelBrowser = new("4C96BE40-915C-11CF-99D3-00AA004AE837");
    private const int CsidlDesktop = 0;
    private const int SwcDesktop = 8;
    private const int SwfoNeedDispatch = 1;
    private const uint SvgioAllView = 2;
    private const uint SvsiPositionItem = 0x80;
    private const uint ShgdnForParsing = 0x8000;
    // STRRET: a UINT and a union whose biggest member is WCHAR[MAX_PATH]; padded for 64-bit.
    private const int StrretSize = 8 + 260 * 2;

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    private static extern int StrRetToStrW(nint pstr, nint pidl, out nint ppsz);

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    // Only the slots up to the method that is called need to be right; the earlier ones are placeholders.
    [ComImport, Guid("85CB6900-4D95-11CF-960C-0080C7F4EE85"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
    private interface IShellWindows
    {
        void _Count();
        void _Item();
        void _NewEnum();
        void _Register();
        void _RegisterPending();
        void _Revoke();
        void _OnNavigate();
        void _OnActivated();
        [PreserveSig]
        int FindWindowSW(ref object pvarLoc, ref object pvarLocRoot, int swClass, out int phwnd, int swfwOptions,
            [MarshalAs(UnmanagedType.IDispatch)] out object? ppdispOut);
    }

    [ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IComServiceProvider
    {
        [PreserveSig]
        int QueryService(ref Guid guidService, ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppvObject);
    }

    [ComImport, Guid("000214E2-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellBrowser
    {
        void _GetWindow();
        void _ContextSensitiveHelp();
        void _InsertMenusSB();
        void _SetMenuSB();
        void _RemoveMenusSB();
        void _SetStatusTextSB();
        void _EnableModelessSB();
        void _TranslateAcceleratorSB();
        void _BrowseObject();
        void _GetViewStateStream();
        void _GetControlWindow();
        void _SendControlMsg();
        [PreserveSig]
        int QueryActiveShellView([MarshalAs(UnmanagedType.IUnknown)] out object ppshv);
    }

    [ComImport, Guid("CDE725B0-CCC9-4519-917E-325D72FAB4CE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFolderView
    {
        void _GetCurrentViewMode();
        void _SetCurrentViewMode();
        [PreserveSig] int GetFolder(ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);
        [PreserveSig] int Item(int iItemIndex, out nint ppidl);
        [PreserveSig] int ItemCount(uint uFlags, out int pcItems);
        void _Items();
        void _GetSelectionMarkedItem();
        void _GetFocusedItem();
        [PreserveSig] int GetItemPosition(nint pidl, out Point ppt);
        void _GetSpacing();
        void _GetDefaultSpacing();
        /// <summary>S_OK (0) when the icons are arranged automatically.</summary>
        [PreserveSig] int GetAutoArrange();
        void _SelectItem();
        [PreserveSig] int SelectAndPositionItems(uint cidl, [MarshalAs(UnmanagedType.LPArray)] nint[] apidl,
            [MarshalAs(UnmanagedType.LPArray)] Point[] apt, uint dwFlags);
    }

    [ComImport, Guid("000214E6-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellFolder
    {
        void _ParseDisplayName();
        void _EnumObjects();
        void _BindToObject();
        void _BindToStorage();
        void _CompareIDs();
        void _CreateViewObject();
        void _GetAttributesOf();
        void _GetUIObjectOf();
        [PreserveSig] int GetDisplayNameOf(nint pidl, uint uFlags, nint pName);
    }
}
