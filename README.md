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
