# Airo

Airo is a Windows screen-mirroring project for iPhone AirPlay.

**Status: Beta 0.2 experimental external receiver window embedding. Airo does not implement the AirPlay protocol itself.**

## Start

Install the .NET 8 SDK on Windows and run:

```powershell
dotnet run --project Airo/Airo.csproj
```

To publish a Windows x64 executable:

```powershell
dotnet publish Airo/Airo.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

## Versioning

All development builds use **Airo Beta 0.x**. The first stable release will be **Airo 1.0**, after AirPlay compatibility and stability have been tested.

## Roadmap

- [x] Dark-themed Windows UI shell
- [x] Launch an externally installed AirPlay receiver on Windows\n- [ ] Validate experimental receiver window embedding on Windows
- [ ] Show connection status and mirrored picture/audio
- [ ] Fullscreen, recordings, screenshots
- [ ] Localization and UI refinements

AirPlay engine licensing and compatibility need review before distribution. Airo is not affiliated with Apple.

## Experimental receiver launcher

The Devices dock button can select and launch an **external** Windows AirPlay receiver executable. Settings lets you change its path. The path is stored under `%LOCALAPPDATA%\\Airo\\receiver-path.txt`. Closing Airo terminates the receiver process it launched.

This is **not** a bundled AirPlay implementation: no receiver executable is included, window embedding is experimental and may fail with some receiver builds, and a running process does not prove iPhone discovery or connection. Use only trusted receiver binaries and verify compatibility/licensing yourself. For an open-source option to investigate, see UxPlay; Windows builds and runtime dependencies require separate verification.

### Manual test

1. Obtain a compatible, trusted Windows receiver build separately.
2. Run Airo, click the Devices icon and select the receiver EXE.
3. Check that its own window starts successfully.
4. Connect iPhone and PC to the same local network, open iPhone Control Center → Screen Mirroring, and look for the receiver's advertised name.
5. If it does not appear, verify Bonjour/mDNS support, network isolation, Windows firewall permissions, and receiver logs. Airo currently cannot diagnose those issues automatically.

## Beta 0.2 window embedding test

Click Devices to start the configured UxPlay executable, then connect your iPhone. Airo polls for the process main window for up to two minutes and attempts to reparent that window into the central area. This is an experimental Win32 approach, not native video decoding. Some receiver builds create windows in child processes or use graphics modes incompatible with reparenting; these may remain separate or fail to display. The status "Receiver window embedded" indicates only that the window handle was reparented, **not** that AirPlay is connected or frames are rendering correctly. Stop the receiver using Devices before exiting Airo. Test audio separately.

No binaries or third-party dependencies are redistributed. Review the receiver license before packaging it with Airo.

### Native receiver viewport sizing (Beta 0.2)

Airo now sizes both the WPF video host and the embedded native UxPlay window to a centered aspect-ratio viewport. Portrait defaults to approximately 9:19.5 (typical modern iPhone); press **F6** to switch to a 16:9 landscape viewport, or back again. Native resizing uses `SetWindowPos` with `SWP_FRAMECHANGED` so the external window recalculates its frame after embedding. This is still experimental: UxPlay may draw its own letterboxing or use a different source aspect ratio, which Airo cannot reliably remove by resizing its host window. Verify both orientations and audio on the target Windows PC.

## Beta 0.2.1 — Settings (development branch)

Beta 0.2.1 is developed on the `beta-0.2.1` branch so the known-working Beta 0.2 remains unchanged on `main`.

The Settings dock button now opens a real settings window. You can:
- Select/change the installed UxPlay receiver executable.
- Open Airo maximized (off by default).
- Keep Airo above other windows (off by default).
- Select portrait or 16:9 wide video layout as the default; F6 still toggles during a session.

Settings are saved to `%LOCALAPPDATA%\\Airo\\settings.json` and loaded on the next launch. On first use, Airo imports the previously selected UxPlay path from `receiver-path.txt`. The receiver is **not** bundled and mirroring still depends on the installed third-party receiver. The receiver starts only when you use the Devices button; automatic receiver startup is intentionally disabled.

To test this branch, download the `beta-0.2.1` branch and run `dotnet run --project Airo/Airo.csproj`. Verify startup window settings, changing layouts, and the saved receiver path before merging to main.

## Beta 0.3 — Receiver Manager (preview branch)

Beta 0.3 is being developed on `beta-0.3` based on the tested `beta-0.2.1` settings branch; neither `main` nor `beta-0.2.1` was changed.

Open **Settings → Receiver Manager** to:
- Check the saved receiver path and common Windows installation directories.
- See the executable's embedded file version, when provided by Windows.
- Select a receiver EXE manually and save it for Airo.
- Install or update the receiver from inside Airo using Windows Package Manager (WinGet), with an explicit confirmation before each operation.\n- Open the [third-party uxplay-windows releases](https://github.com/leapbtw/uxplay-windows/releases) page as a manual fallback if WinGet is unavailable.

When the Devices button is used without a valid receiver, Airo now opens Receiver Manager instead of a bare file picker.

**Safety and scope:** The Manager only checks file existence and file metadata; it does not claim that AirPlay is connected or that a receiver build is trustworthy. The Install and Update buttons call WinGet for the exact `leapbtw.uxplay` package from the `winget` source **only after explicit confirmation**. WinGet downloads and launches the third-party installer and may request administrator permission. Airo itself does not bundle, download or directly execute third-party installer binaries. The manager rescans after WinGet exits; click **Use this receiver** to save the detected EXE. If WinGet is missing, use the manual releases-page fallback. Verify provenance, Windows compatibility, license and installer integrity before distributing a bundled installer in a future release.

The Beta 0.3 branch has its own Windows build workflow. Test the manager and mirroring on Windows before merging.

### Test in-app receiver installation

1. Open Settings → Receiver Manager. Confirm your existing receiver still appears.
2. Click **Install receiver** (or **Update receiver** if already installed). Read the publisher/package confirmation before approving.
3. Complete any Windows installer/UAC prompts. Airo waits for WinGet to finish and then scans the installed receiver again.
4. Click **Use this receiver** to save its executable path. Verify that Devices still launches the working receiver and mirroring continues to work.
5. If WinGet is unavailable, click **Manual download** and install from the third-party releases page, then click **Scan again**.

No receiver installation starts automatically when Airo launches. Airo cannot guarantee that every Windows system has WinGet or that every UxPlay release has an available WinGet package. The receiver's installer may be unsigned; verify the publisher and source before proceeding.

### Receiver Manager reliability update

Receiver Manager now treats **installation state** separately from **WinGet command status**:
- When an EXE is detected, **Install receiver** is disabled and **Update receiver** is available.
- When no EXE is detected, **Install receiver** is available and **Update receiver** is disabled.
- After a WinGet operation, Airo scans for the EXE again and shows a human-readable result.
- WinGet exit code `0x8A150014` (`-1978335212`) means **no matching packages found**, not necessarily a broken installation. If Airo still detects the EXE, it tells the user the receiver can continue to be used.
- The manager includes **Installation diagnostics** and **Copy diagnostics** to help troubleshoot WinGet source/package mismatches without relying on raw modal error dialogs.

This change does not uninstall, repair, or replace an existing receiver. It does not prove that an installed EXE is managed by WinGet; users who installed via MSI manually may need to update using **Manual download**. WinGet package availability is not guaranteed.

## Beta 0.3 — Performance preview

The `beta-0.3-performance` branch preserves the tested Receiver Manager from `beta-0.3` while refining the native video-host lifecycle.

- **Faster initial video-window attachment:** checks every 250 ms for the first 30 seconds instead of every 1000 ms. The receiver still starts only when Devices is clicked.
- **Lower idle polling overhead:** after 30 seconds without a window, checks every 750 ms; after attachment, checks the window every second.
- **Late connections:** continues waiting for the video window beyond the former two-minute cutoff.
- **Window recreation:** if the embedded window disappears while the receiver process is still running, Airo returns to waiting and attempts to attach a replacement window.
- **Less native resize work:** coalesces frequent panel resizes into a 40 ms update and skips identical sizes. The expensive `SWP_FRAMECHANGED` flag is applied only on the initial attach, not on every resize.
- **Cleanup:** stopping the receiver cancels pending attachment attempts and scheduled resizes.

These changes aim to reduce host UI overhead and improve attach responsiveness. They **do not** modify UxPlay's encoding/decoding pipeline or guarantee lower AirPlay streaming latency or CPU usage. Verify screen mirroring, F6 layout switching, resizing, long idle waits, and receiver stop/restart on Windows before merging.

### Hotfix — blank embedded video

Fixed a regression in the performance preview where WPF's initially unset `WindowsFormsHost.Width` and `Height` are `double.NaN`. The optimized size comparison previously skipped the initial dimensions entirely, so the receiver could report "embedded" with an empty viewport. The host now explicitly initializes dimensions when unset and performs the initial native resize after WPF's layout pass. Re-test iPhone mirroring, F6 and window resizing. This fix has not yet been verified on the target Windows machine.

## Beta 0.3 — Video stability and layout preview

Development branch: `beta-0.3-video-stability` (based on the user's working `beta-0.3-performance` preview). Previous branches are unchanged.

### Improvements

- **Portrait / landscape:** use **Layout** in the header or **F6**. The host fits a 9:19.5 portrait or 16:9 landscape viewport inside the available area without intentional cropping or non-uniform scaling. A layout change schedules a forced native resize and redraw after WPF has arranged the host.
- **Black / stale embedded viewport recovery:** use **Refresh** in the header or **F7**. This forces the existing native receiver window to recompute its client area and requests a redraw, without restarting or reinstalling UxPlay. This helps with stale host geometry; it cannot repair an upstream AirPlay decoder stall or missing stream frames.
- **Resizing stability:** host updates are coalesced to reduce unnecessary native resize work, but an additional 200 ms deadline prevents continuous dragging from delaying all native resize updates.
- **Native window safety:** verifies that the embedded handle still exists before native resizing and only caches dimensions after Windows reports a successful resize.
- **Preserves reconnect logic:** if the receiver's video window is destroyed, the existing attachment loop waits for a replacement.

### Test on Windows

1. Start UxPlay from Devices and mirror an iPhone. Confirm picture and sound behavior are unchanged.
2. Press **F6** repeatedly and use the **Layout** button; test both portrait and landscape with a maximized and restored Airo window.
3. Drag the window edges continuously and confirm the video updates without getting stuck black.
4. Press **F7** / **Refresh** if the viewport turns black; if the iPhone stream itself has stopped, reconnect the phone or restart the receiver.
5. Stop/restart the receiver and reconnect after a delay. Confirm the Receiver Manager remains functional.

**Scope:** This release targets the host's display quality and stability (fewer unnecessary native redraw/resize operations and consistent aspect fit). It does **not** change UxPlay's video codec, decoded stream resolution, or actual FPS; any future decoder quality controls require validating supported options of the installed receiver build. Build and runtime behavior still require verification on Windows.

## Beta 0.3 — Receiver stability preview

The `beta-0.3-stability` branch extends the previously tested video-stability branch without changing it.

- Blocks duplicate receiver launches while a start attempt is in progress (for example, a rapid double-click on Devices).
- Safely checks receiver process state even if Windows has already exited or disposed the process.
- Retries up to three transient window-embedding failures instead of abandoning the connection on the first failed attempt; shows a clear message if all retries fail.
- Clears stale native-window and resize state between failed attempts.
- Resets the Win32 last-error value before `SetParent`, distinguishing an actual failure from a successful reparenting that returns a null previous parent.

**Windows smoke test:** (1) Launch UxPlay with Devices, (2) double-click Devices rapidly and confirm only one receiver launches, (3) mirror the iPhone, (4) switch layout with F6 and refresh with F7, (5) stop and restart the receiver, (6) disconnect/reconnect the iPhone and confirm video returns. The branch is a preview and has not been verified on the user's Windows installation.
