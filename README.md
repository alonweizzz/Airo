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
