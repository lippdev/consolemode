using System.Runtime.InteropServices;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace ConsoleMode.Native;

/// <summary>
/// A window backdrop with nothing in it: where the window's content is transparent, what is behind the window shows
/// through. The toast uses it to slide in from the screen's edge inside a window that already reaches it.
/// The backdrop takes a brush of the system compositor (Windows.UI.Composition), which needs a Windows dispatcher
/// queue on the thread; WinUI's UI thread only has its own, so one is created the first time.
/// </summary>
public sealed class TransparentBackdrop : SystemBackdrop
{
    private static Windows.UI.Composition.Compositor? _compositor;
    private static nint _queueController;

    protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
    {
        base.OnTargetConnected(connectedTarget, xamlRoot);
        EnsureDispatcherQueue();
        _compositor ??= new Windows.UI.Composition.Compositor();
        connectedTarget.SystemBackdrop = _compositor.CreateColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
    }

    protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop disconnectedTarget)
    {
        base.OnTargetDisconnected(disconnectedTarget);
        disconnectedTarget.SystemBackdrop = null;
    }

    private static void EnsureDispatcherQueue()
    {
        if (Windows.System.DispatcherQueue.GetForCurrentThread() is not null || _queueController != 0) return;
        var options = new DispatcherQueueOptions
        {
            Size = Marshal.SizeOf<DispatcherQueueOptions>(),
            ThreadType = 2, // DQTYPE_THREAD_CURRENT
            ApartmentType = 2 // DQTAT_COM_STA
        };
        Marshal.ThrowExceptionForHR(CreateDispatcherQueueController(options, out _queueController));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DispatcherQueueOptions
    {
        public int Size;
        public int ThreadType;
        public int ApartmentType;
    }

    [DllImport("CoreMessaging.dll")]
    private static extern int CreateDispatcherQueueController(DispatcherQueueOptions options, out nint controller);
}
