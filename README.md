# Airo

Airo is a Windows screen-mirroring project for iPhone AirPlay.

**Status: Beta 0.1 desktop UI prototype only. AirPlay reception is NOT implemented yet.**

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
- [ ] Integrate and test an AirPlay receiver on Windows
- [ ] Show connection status and mirrored picture/audio
- [ ] Fullscreen, recordings, screenshots
- [ ] Localization and UI refinements

AirPlay engine licensing and compatibility need review before distribution. Airo is not affiliated with Apple.

## Experimental receiver launcher

The Devices dock button can select and launch an **external** Windows AirPlay receiver executable. Settings lets you change its path. The path is stored under `%LOCALAPPDATA%\\Airo\\receiver-path.txt`. Closing Airo terminates the receiver process it launched.

This is **not** a bundled AirPlay implementation: no receiver executable is included, no stream is rendered inside Airo, and a running process does not prove iPhone discovery or connection. Use only trusted receiver binaries and verify compatibility/licensing yourself. For an open-source option to investigate, see UxPlay; Windows builds and runtime dependencies require separate verification.

### Manual test

1. Obtain a compatible, trusted Windows receiver build separately.
2. Run Airo, click the Devices icon and select the receiver EXE.
3. Check that its own window starts successfully.
4. Connect iPhone and PC to the same local network, open iPhone Control Center → Screen Mirroring, and look for the receiver's advertised name.
5. If it does not appear, verify Bonjour/mDNS support, network isolation, Windows firewall permissions, and receiver logs. Airo currently cannot diagnose those issues automatically.
