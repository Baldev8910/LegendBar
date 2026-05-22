<div align="center">

<img src="Screenshots/AdvScreenshots//LegendBar.png" alt="LegendBar" width="100%"/>

<br>
<br>

A customizable Windows 11 top bar built with WinUI 3.

Acrylic glass • Auto-hide • Media controls • Multi-monitor support

[![Sponsor](https://img.shields.io/badge/Sponsor-EA4AAA?style=for-the-badge&logo=githubsponsors&logoColor=white)](https://github.com/sponsors/Baldev8910) 

<a href="https://github.com/Baldev8910/LegendBar/releases/latest">
  <img src="https://img.shields.io/badge/Download-Latest%20Release-22c55e?style=for-the-badge"/>
</a>

![Downloads](https://img.shields.io/github/downloads/Baldev8910/LegendBar/1.1.6.7/total?style=flat-square) ![Downloads](https://img.shields.io/github/downloads/Baldev8910/LegendBar/1.1.6.6/total?style=flat-square) ![Downloads](https://img.shields.io/github/downloads/Baldev8910/LegendBar/1.1.6.5/total?style=flat-square) ![Downloads](https://img.shields.io/github/downloads/Baldev8910/LegendBar/1.1.6.4/total?style=flat-square) ![Downloads](https://img.shields.io/github/downloads/Baldev8910/LegendBar/1.1.6.3/total?style=flat-square)

<br>

<img src="https://img.shields.io/badge/Platform-Windows%2011-0078D6?style=for-the-badge&logo=windows"/>
<img src="https://img.shields.io/badge/Built%20With-WinUI%203-6A5ACD?style=for-the-badge"/>
<img src="https://img.shields.io/badge/.NET-8-512BD4?style=for-the-badge&logo=dotnet"/>
<img src="https://img.shields.io/github/license/Baldev8910/LegendBar?style=for-the-badge"/>
<img src="https://img.shields.io/github/stars/Baldev8910/LegendBar?style=for-the-badge"/>

<br><br>

<img src="Assets/GIF.gif" alt="LegendBar Demo" width="100%"/>

</div>

</div>

## Contents

- [Contents](#contents)
- [Why LegendBar?](#why-legendbar)
- [Screenshots](#screenshots)
- [Features](#features)
- [Requirements](#requirements)
- [Installation](#installation)
- [Uninstalling](#uninstalling)
- [Settings](#settings)
- [Known Limitations](#known-limitations)
- [Roadmap](#roadmap)
- [Contributing](#contributing)
- [Built With](#built-with)
- [License](#license)
- [Acknowledgements](#acknowledgements)

## Why LegendBar?

LegendBar was designed to bring a cleaner and more productive desktop workflow to Windows 11 while staying visually native to the operating system.

Unlike traditional desktop widgets or overlays, LegendBar integrates directly with WinUI 3, Win32 APIs, AppBar behavior, and Windows composition effects to create a lightweight top bar focused on usability, media control, productivity, and customization.

## Screenshots

<div align="center">

<img src="/Screenshots/AdvScreenshots/13.png" width="100%">

</div>

<div align="center">

<img src="/Screenshots/AdvScreenshots/2.png" width="50%">
<img src="/Screenshots/AdvScreenshots/3.png" width="49%">

</div>

<div align="center">

<img src="/Screenshots/AdvScreenshots/1.png" width="100%">

</div>

<div align="center">

<img src="/Screenshots/AdvScreenshots/5.png" width="50%"> 
<img src="/Screenshots/AdvScreenshots/4.png" width="49%">

</div>

<div align="center">

<img src="/Screenshots/AdvScreenshots/7.png" width="50%"> 
<img src="/Screenshots/AdvScreenshots/11.png" width="49%">

</div>

<div align="center">

<img src="/Screenshots/AdvScreenshots/10.png" width="100%"> 

</div>

<div align="center">

<img src="/Screenshots/AdvScreenshots/6.png" width="100%">

</div>

<div align="center">

<img src="/Screenshots/AdvScreenshots/12.png" width="100%"> 

</div>

</div>

<div align="center">

<img src="/Screenshots/AdvScreenshots/9.png" width="100%"> 

</div>

<div align="center">

<img src="/Screenshots/AdvScreenshots/8.png" width="100%"> 

</div>


---

## Features

<details>
<summary><b>🎨 Interface & Appearance</b></summary>

<br>

- Acrylic, Mica, Mica Alt, and Solid material support
- Live acrylic tint and blur customization
- Smooth auto-hide with cubic-ease animations
- Borderless always-on-top WinUI 3 window
- Thin Windows 11-style bottom border
- Dynamic popup material synchronization
- Adjustable bar height and animation speeds

</details>

<details>
<summary><b>🖥️ Window & Monitor Integration</b></summary>

<br>

- Full dual-monitor support
- Windows AppBar API integration for pinned mode
- Auto-hide detection using low-level mouse hooks
- Proper reserved screen space when pinned
- Hidden from Alt+Tab and taskbar switchers
- Re-asserts topmost state automatically

</details>

<details>
<summary><b>🎵 Media Controls</b></summary>

<br>

- System-wide media controls (Spotify, Chrome, Firefox, VLC, etc.)
- Play, pause, previous, and next controls
- Click song title to focus media app
- Per-app volume control via mouse wheel
- Animated volume overlay indicator
- Dynamic media session detection

</details>

<details>
<summary><b>⏰ Productivity Widgets</b></summary>

<br>

- Pomodoro timer with animated transitions
- Expandable timer controls (pause/reset/skip)
- Reminder system with recurring schedules
- Clipboard history with image support
- Quick Notes popup with Markdown + KaTeX rendering
- Clock and Date widgets
- PowerToys shortcuts reference panel
- DevToys launcher integration

</details>

<details>
<summary><b>⚙️ Customization & Settings</b></summary>

<br>

- Live settings preview
- Launch on startup support
- Widget visibility toggles
- Persistent JSON-based settings
- Reset-to-defaults support
- Dynamic popup resizing

</details>

<details>
<summary><b>🛠️ Technical Highlights</b></summary>

<br>

- Built with WinUI 3 and .NET 8
- Uses DesktopAcrylicController for true acrylic blur
- Uses Windows System Media Transport Controls (SMTC)
- Uses low-level Win32 hooks for edge detection
- Uses WebView2 for the Notes editor
- Uses NAudio for per-app audio sessions
- JSON-based persistence system
- Timer-driven custom animation engine

</details>

---

## Requirements

| Requirement | Details |
|---|---|
| Operating System | Windows 11 |
| Architecture | x64/x86/arm64 |
| Display Scaling | Same DPI scaling on all monitors |
| Runtime | Windows App Runtime |

---

## Installation

1. Go to the [Releases](https://github.com/Baldev8910/LegendBar/releases/latest) page
2. Download the appropriate latest installer as per your architecture
3. Run `LegendBar_X.X.X.X_xXX.msixbundle`
4. Launch LegendBar from the Start Menu

> [!IMPORTANT]
> Windows App Runtime is required.  
> If it is not already installed, the setup process will prompt you automatically.

> [!NOTE]
> Developer Mode may be required for sideloaded MSIX installation on some systems.

---

## Uninstalling

1. Open **Settings → Apps → Installed Apps**
2. Locate **LegendBar**
3. Click **Uninstall**

---

## Settings

Access settings from the ⚙️ icon on the right side of the bar.

| Category | Options |
|---|---|
| Appearance | Material type, acrylic tint, blur intensity, bar height |
| Animation | Show speed, hide speed, hide delay |
| Widgets | Toggle visibility for Clock, Date, Media, and utility widgets |
| System | Launch on startup, reset to defaults |
| Behavior | Temperature unit and interaction settings |

All settings are applied live and saved automatically.

---

## Known Limitations

- Browser volume scrolling adjusts the Windows audio session volume, not in-page player volume (e.g. YouTube volume slider)
- When multiple media apps are playing simultaneously, title focus detection may occasionally target the wrong app
- Windows 10 is currently unsupported and untested

---

## Roadmap

- [x] DPI scaling support
- [ ] Plugin/widget system
- [ ] Custom themes
- [ ] Keyboard shortcut customization
- [x] Weather widget
- [ ] Calendar integration
- [ ] Performance optimization pass

---

## Contributing

Contributions, bug reports, feature suggestions, and pull requests are welcome.

If you encounter issues or have ideas for improvements, feel free to open an issue or discussion.

---

## Built With

- [![WinUI 3](https://img.shields.io/badge/WinUI%203-Docs-6A5ACD?style=flat-square&logo=microsoft&logoColor=white)](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/)
- [![Windows App SDK](https://img.shields.io/badge/Windows%20App%20SDK-Docs-0078D6?style=flat-square&logo=windows&logoColor=white)](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/)
- [![WebView2](https://img.shields.io/badge/WebView2-Docs-0078D6?style=flat-square&logo=microsoftedge&logoColor=white)](https://developer.microsoft.com/en-us/microsoft-edge/webview2/)
- [![NAudio](https://img.shields.io/badge/NAudio-GitHub-333333?style=flat-square&logo=github&logoColor=white)](https://github.com/naudio/NAudio)
- [![C#](https://img.shields.io/badge/C%23-.NET%208-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
- [![Win32](https://img.shields.io/badge/Win32%20APIs-Reference-0078D6?style=flat-square&logo=windows&logoColor=white)](https://learn.microsoft.com/en-us/windows/win32/)
- [![SMTC](https://img.shields.io/badge/SMTC-Media%20Controls-6A5ACD?style=flat-square)](https://learn.microsoft.com/en-us/uwp/api/windows.media.systemmediatransportcontrols)

---

## License

![License](https://img.shields.io/badge/License-MIT-22c55e?style=for-the-badge)

This project is released under the [MIT License](LICENSE).

---

## Acknowledgements

LegendBar was built as an experimental Windows 11 desktop enhancement project focused on acrylic composition, low-level window management, and productivity tooling.

Inspired by the flexibility of custom desktop environments and utility bars found across Linux and macOS ecosystems — rebuilt for native Windows 11 using WinUI 3 and Win32 APIs.
