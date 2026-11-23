# gpui_windows — Console Mode 2 changes

Copy of `crates/gpui_windows` from [zed-industries/zed](https://github.com/zed-industries/zed) at
`917559da35dd4d626205da705b071c800c9c2553` (Apache-2.0, see `LICENSE-APACHE`), used through
`[patch]` in the workspace `Cargo.toml`. Everything else of GPUI still comes from that commit.

## What changed

**Frames on demand instead of on every vsync.** Upstream, the `VSyncProvider` thread invalidates
every window on every vsync, so an idle window still wakes the UI thread 60–240 times a second.
GPUI core already supports demand-driven platforms (`PlatformWindow::frame_waker` and
`schedule_frame`, used by macOS); this copy implements them on Windows:

- `window.rs`: each window has `frame_demand`; `frame_waker()` and `schedule_frame()` set it and
  wake the vsync thread.
- `platform.rs` (`begin_vsync_thread`): while some window wants a frame, and for `DEMAND_LINGER`
  (300 ms) after the last request, it behaves as upstream (invalidate on every vsync). Otherwise it
  parks until woken, with an `IDLE_HEARTBEAT` (1 s) as a safety net. `detach_gpu` wakes it to stop.
- `events.rs`: a draw deferred because another draw was running asks for a frame again.
- `vsync.rs`: the thread handle, `wake_vsync()`, and the two constants.
- `Cargo.toml`: workspace inheritance replaced with the values of zed's workspace at that commit.

Measured on the owner's PC (240 Hz monitor), app idle on the Home: 22–80 M CPU cycles/s before,
6–8 M after (QueryProcessCycleTime, 30 s windows).

## Updating GPUI

When the GPUI pin moves, copy the new `crates/gpui_windows` over this folder, re-apply the changes
above (search for "Console Mode"), and refresh the dependency versions in `Cargo.toml`.
