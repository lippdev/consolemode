using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using ConsoleMode.ViewModels;
using Microsoft.UI.Dispatching;

namespace ConsoleMode.Services;

/// <summary>
/// Local control API for other tools on this PC — a remote-control agent running as a Windows
/// service, scripts, a Stream Deck plugin — over the named pipe <c>\\.\pipe\ConsoleMode.Control</c>
/// (format in <see cref="ControlProtocol"/>).
/// <para>
/// start/stop/show do exactly what <c>consolemode://start|stop|show</c> do (they set the same
/// signals), then answer once the app has settled, so the caller learns whether it worked.
/// "status" is what a link can't give: whether console mode is on, and in which mode.
/// </para>
/// Only the signed-in user and LocalSystem may connect; nothing is exposed to the network.
/// </summary>
public sealed class ControlPipeService : IDisposable
{
    public const string PipeName = "ConsoleMode.Control";

    private readonly MainViewModel _vm;
    private readonly DispatcherQueue _dispatcher;
    private readonly Action _requestStart;
    private readonly Action _requestStop;
    private readonly Action _requestShow;
    private readonly CancellationTokenSource _cts = new();

    public ControlPipeService(MainViewModel vm, DispatcherQueue dispatcher, Action requestStart, Action requestStop, Action requestShow)
    {
        _vm = vm;
        _dispatcher = dispatcher;
        _requestStart = requestStart;
        _requestStop = requestStop;
        _requestShow = requestShow;
        _ = Task.Run(ListenAsync);
    }

    public void Dispose() => _cts.Cancel();

    private async Task ListenAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            NamedPipeServerStream? pipe = null;
            try
            {
                pipe = CreatePipe();
                await pipe.WaitForConnectionAsync(_cts.Token);
                var client = pipe;
                pipe = null;
                _ = Task.Run(() => HandleAsync(client));
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                AppLog.Write($"Controle: {ex.Message}");
                try { await Task.Delay(2000, _cts.Token); }
                catch (OperationCanceledException) { break; }
            }
            finally
            {
                pipe?.Dispose();
            }
        }
    }

    private static NamedPipeServerStream CreatePipe()
    {
        var security = new PipeSecurity();
        var user = WindowsIdentity.GetCurrent().User ?? throw new InvalidOperationException("no user SID");
        security.AddAccessRule(new PipeAccessRule(user, PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), PipeAccessRights.ReadWrite, AccessControlType.Allow));
        return NamedPipeServerStreamAcl.Create(PipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 0, 0, security);
    }

    private async Task HandleAsync(NamedPipeServerStream pipe)
    {
        await using (pipe)
        {
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
                timeout.CancelAfter(TimeSpan.FromSeconds(90));
                using var reader = new StreamReader(pipe, new UTF8Encoding(false), false, 1024, leaveOpen: true);
                var line = await reader.ReadLineAsync(timeout.Token);
                var reply = await ExecuteAsync(ControlProtocol.ParseCommand(line), timeout.Token);
                await pipe.WriteAsync(Encoding.UTF8.GetBytes(reply + "\n"), timeout.Token);
                await pipe.FlushAsync(timeout.Token);
            }
            catch (Exception ex)
            {
                AppLog.Write($"Controle: {ex.Message}");
            }
        }
    }

    private async Task<string> ExecuteAsync(string cmd, CancellationToken token)
    {
        if (cmd != ControlProtocol.Status) AppLog.Write($"Controle: {cmd}");
        switch (cmd)
        {
            case ControlProtocol.Status:
                return await ReplyAsync(true);
            case ControlProtocol.Start:
                _requestStart();
                // starting can ask "can you see this screen?" on a new TV: give it time
                var started = await WaitForAsync(s => s.Active, TimeSpan.FromSeconds(60), token);
                return await ReplyAsync(started, started ? null : "console mode did not start");
            case ControlProtocol.Stop:
                if (!(await SnapshotAsync()).Active) return await ReplyAsync(true);
                _requestStop();
                var stopped = await WaitForAsync(s => !s.Active && !s.Restoring, TimeSpan.FromSeconds(60), token);
                return await ReplyAsync(stopped, stopped ? null : "restore did not finish");
            case ControlProtocol.Show:
                _requestShow();
                return await ReplyAsync(true);
            default:
                return await ReplyAsync(false, "unknown command");
        }
    }

    private readonly record struct Snapshot(bool Active, bool Restoring, string Mode);

    // the view model belongs to the UI thread
    private Task<Snapshot> SnapshotAsync()
    {
        var done = new TaskCompletionSource<Snapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        var queued = _dispatcher.TryEnqueue(() =>
        {
            try
            {
                var mode = _vm.IsConsoleActive ? _vm.Engine.State.FullscreenMode : ConfigService.Load().FullscreenMode;
                done.SetResult(new Snapshot(_vm.IsConsoleActive, _vm.IsRestoring, mode));
            }
            catch (Exception ex)
            {
                done.SetException(ex);
            }
        });
        if (!queued) done.SetException(new InvalidOperationException("dispatcher unavailable"));
        return done.Task;
    }

    private async Task<bool> WaitForAsync(Func<Snapshot, bool> condition, TimeSpan limit, CancellationToken token)
    {
        var deadline = DateTime.UtcNow + limit;
        await Task.Delay(300, token);
        while (DateTime.UtcNow < deadline)
        {
            if (condition(await SnapshotAsync())) return true;
            await Task.Delay(500, token);
        }
        return condition(await SnapshotAsync());
    }

    private async Task<string> ReplyAsync(bool ok, string? error = null)
    {
        var s = await SnapshotAsync();
        var version = typeof(ControlPipeService).Assembly.GetName().Version?.ToString(3) ?? "";
        return ControlProtocol.Reply(ok, error, s.Active, s.Restoring, s.Mode, version);
    }
}
