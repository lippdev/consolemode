using System.Runtime.InteropServices;

namespace ConsoleMode.Services;

/// <summary>
/// Hides the mouse pointer during a session when the user asks for it in the session menu (emulators and
/// some games leave it in the middle of the TV), and shows it again when they turn it off or the session
/// ends. Nothing automatic: moving the mouse doesn't bring it back.
/// Windows has no "hide the pointer for every app" call, so the system cursors are swapped for a blank one
/// (SetSystemCursor) and reloaded from the user's scheme afterwards (SPI_SETCURSORS). A marker file covers
/// a crash in between: the next start puts the cursors back (<see cref="RecoverAfterCrash"/>).
/// </summary>
public static class CursorHider
{
    // OCR_NORMAL, IBEAM, WAIT, CROSS, UP, SIZENWSE, SIZENESW, SIZEWE, SIZENS, SIZEALL, NO, HAND, APPSTARTING, HELP, PIN, PERSON.
    private static readonly uint[] SystemCursors =
        [32512, 32513, 32514, 32515, 32516, 32642, 32643, 32644, 32645, 32646, 32648, 32649, 32650, 32651, 32671, 32672];
    private const uint SpiSetCursors = 0x0057;
    private const int SmCxCursor = 13;
    private const int SmCyCursor = 14;

    private static readonly object Gate = new();
    private static bool _hidden;
    private static bool _exitHooked;

    public static void SetHidden(bool hidden)
    {
        if (hidden) Hide();
        else Show();
    }

    private static void Hide()
    {
        lock (Gate)
        {
            if (_hidden) return;
            if (!_exitHooked)
            {
                _exitHooked = true;
                AppDomain.CurrentDomain.ProcessExit += (_, _) => Show();
            }
            _hidden = true;
            WriteMarker(true);
            var blank = CreateBlankCursor();
            if (blank == 0)
            {
                AppLog.Write($"Cursor: não foi possível criar o cursor vazio ({Marshal.GetLastWin32Error()})");
                return;
            }
            // SetSystemCursor takes ownership of each handle, so every slot gets its own copy.
            foreach (var id in SystemCursors) SetSystemCursor(CopyIcon(blank), id);
            DestroyCursor(blank);
        }
    }

    private static void Show()
    {
        lock (Gate)
        {
            if (!_hidden) return;
            _hidden = false;
            Restore();
        }
    }

    /// <summary>At startup: a previous run that died with the pointer hidden left the marker behind.</summary>
    public static void RecoverAfterCrash()
    {
        try
        {
            if (!File.Exists(MarkerPath)) return;
            Restore();
            AppLog.Write("Cursor: restaurado após um encerramento inesperado");
        }
        catch (Exception ex)
        {
            AppLog.Write($"Cursor: restaurar: {ex.Message}");
        }
    }

    private static void Restore()
    {
        // Reloads the user's own scheme (Settings → Mouse pointer), whatever it is.
        if (!SystemParametersInfo(SpiSetCursors, 0, 0, 0))
            AppLog.Write($"Cursor: não foi possível restaurar os cursores ({Marshal.GetLastWin32Error()})");
        WriteMarker(false);
    }

    private static string MarkerPath => Path.Combine(AppPaths.DataDir, "cursor-hidden");

    private static void WriteMarker(bool hidden)
    {
        try
        {
            if (string.IsNullOrEmpty(AppPaths.DataDir)) return;
            if (hidden) File.WriteAllText(MarkerPath, "");
            else if (File.Exists(MarkerPath)) File.Delete(MarkerPath);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Cursor: marcador: {ex.Message}");
        }
    }

    /// <summary>A cursor of the system size whose AND mask keeps the screen and whose XOR mask adds nothing.</summary>
    private static nint CreateBlankCursor()
    {
        var width = Math.Max(GetSystemMetrics(SmCxCursor), 32);
        var height = Math.Max(GetSystemMetrics(SmCyCursor), 32);
        var size = (width + 15) / 16 * 2 * height;   // rows are WORD-aligned
        var and = new byte[size];
        Array.Fill(and, (byte)0xFF);
        var xor = new byte[size];
        return CreateCursor(GetModuleHandle(null), 0, 0, width, height, and, xor);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint CreateCursor(nint instance, int hotX, int hotY, int width, int height, byte[] andPlane, byte[] xorPlane);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyCursor(nint cursor);

    [DllImport("user32.dll")]
    private static extern nint CopyIcon(nint icon);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetSystemCursor(nint cursor, uint id);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfo(uint action, uint param, nint value, uint winIni);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandle(string? moduleName);
}
