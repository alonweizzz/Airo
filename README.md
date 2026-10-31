<div align="center">

<img src="Airo/assets/AiroLogo.png" alt="Airo logo" width="116" />

# Airo

**Your iPhone screen. Beautifully mirrored on Windows.**

A modern, minimal AirPlay screen-mirroring companion for Windows, designed around a clean interface and a smooth everyday experience.

**Windows · iPhone · AirPlay · Open-source project**

[Explore the current beta](#what-airo-can-do) · [Get started](#get-started) · [The roadmap](#whats-next)

</div>

---

## Meet Airo

Airo makes it easy to bring your iPhone screen into a dedicated Windows desktop window. It combines an external AirPlay receiver with a purpose-built interface, so connecting, viewing, and managing your receiver feels like part of one app.

The current design direction blends **Windows 11 Fluent aesthetics**, **glass-inspired surfaces**, and **Airo's signature blue**.

> **Development status:** Airo is an evolving beta. The `beta-0.4-ui` branch is a UI preview; some visual elements and features are still in development.

## What Airo can do

| Experience | Current capabilities |
| --- | --- |
| **iPhone screen mirroring** | Display the receiver's video window inside Airo on your Windows PC. |
| **Receiver Manager** | Find an installed UxPlay receiver, select its executable, or launch an optional WinGet installation/update after confirmation. |
| **Flexible viewing** | Switch between portrait and landscape layouts, resize the window, and request a viewport refresh. |
| **Video preferences** | Choose Balanced, Smooth, or Responsive receiver profiles to experiment with fluidity and latency. |
| **Personal settings** | Save receiver location, preferred layout, startup window behavior, and always-on-top preference. |
| **Modern desktop UI** | Dark blue styling, rounded controls, a floating bottom dock, and the new Airo logo. |

**Quick controls:** `F6` switches layout; `F7` refreshes the embedded video viewport.

Airo hosts an **external UxPlay receiver**; it does not implement AirPlay decoding itself. The actual image quality, connection reliability, and latency also depend on your iPhone, network, and the installed receiver.

## Get started

### Requirements

- A Windows PC with the **.NET 8 SDK** to build/run from source.
- An iPhone and PC on the same local network.
- A compatible **UxPlay Windows receiver** installed separately. You can manage the receiver from Airo's Receiver Manager.

### Run the app

Clone or download this branch, open a terminal in the repository root, and run:

```powershell
dotnet run --project Airo/Airo.csproj
```

Then:

1. Open **Devices** in Airo's floating dock to configure or start the receiver.
2. If needed, open **Settings → Receiver Manager** to detect or install a compatible receiver.
3. On your iPhone, open **Control Center → Screen Mirroring** and choose the name advertised by your receiver.
4. Your screen should appear in Airo once UxPlay opens its video window.

To create a Windows x64 build from source:

```powershell
dotnet publish Airo/Airo.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

If your iPhone cannot discover the receiver, check that both devices share the same network and that Bonjour/mDNS and Windows Firewall are not blocking discovery.

## What's next

Our goal is to make Airo feel like a polished, native Windows app while preserving the working mirroring experience.

| Focus | Direction |
| --- | --- |
| **Beta 0.4 — A new look** | Refine the glass-inspired UI, settings, floating dock, subtle transitions, and visual consistency. **In progress.** |
| **Smarter mirroring** | Explore better display-fit options, orientation handling, reconnection feedback, and clearer connection status. **Planned.** |
| **Everyday tools** | Investigate screenshots, recording, fullscreen viewing, and more convenient controls. **Planned.** |
| **More control** | Explore diagnostics, optional performance insights, improved receiver management, and future customization. **Planned.** |
| **Toward 1.0** | Prioritize compatibility, stability, installer quality, accessibility, and a dependable user experience before calling Airo stable. **Long-term goal.** |

*Roadmap items represent goals, not promises or confirmed release dates.*

## Built for a simple experience

Airo is developed using **C#**, **.NET 8**, and **WPF**. Its interface uses Windows desktop technologies, while screen reception currently relies on a separately installed UxPlay executable.

We develop experimental improvements in dedicated branches to protect versions already tested with real iPhone mirroring. The **`beta-0.4-ui`** branch focuses on interface work; the tested receiver and streaming behavior remains the baseline.

## Important notes

- **Beta software:** Some receiver builds, Windows configurations, and network setups may behave differently.
- **External receiver:** Airo does not bundle UxPlay. Receiver installation or updates through WinGet require user confirmation; check the source and license of any third-party software you install.
- **Not affiliated with Apple:** Airo is an independent project and is not endorsed by Apple.
- **Experimental features:** Video performance profiles are optional and do not guarantee a particular FPS or latency.

---

<div align="center">

### Airo — Mirror simply. Make it yours.

**Made for Windows. Designed around your iPhone.**

</div>
