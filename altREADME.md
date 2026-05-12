<div align="center">

# LegendBar

A customizable Windows 11 top bar built with WinUI 3.

Acrylic glass • Auto-hide • Media controls • Multi-monitor support

<br>

<img src="Assets/GIF.gif" alt="LegendBar Demo" width="100%"/>

<br>

<!-- Platform & Stack -->
![Platform](https://img.shields.io/badge/Platform-Windows%2011-0078D6?style=for-the-badge&logo=windows11&logoColor=white) ![Framework](https://img.shields.io/badge/Built%20With-WinUI%203-6A5ACD?style=for-the-badge&logo=microsoft&logoColor=white) ![Language](https://img.shields.io/badge/C%23-.NET%208-512BD4?style=for-the-badge&logo=dotnet&logoColor=white) ![License](https://img.shields.io/badge/License-MIT-22c55e?style=for-the-badge)

</div>

![GitHub Stats](https://github-readme-stats.vercel.app/api?username=Baldev8910&show_icons=true&theme=tokyonight)
![Top Languages](https://github-readme-stats.vercel.app/api/top-langs/?username=Baldev8910&layout=compact&theme=tokyonight)
[![GitHub Streak](https://streak-stats.demolab.com?user=Baldev8910&theme=tokyonight)](https://git.io/streak-stats)
[![Activity Graph](https://github-readme-activity-graph.vercel.app/graph?username=Baldev8910&theme=tokyo-night)](https://github.com/ashutosh00710/github-readme-activity-graph)
[![LegendBar](https://github-readme-stats.vercel.app/api/pin/?username=Baldev8910&repo=LegendBar&theme=tokyonight)](https://github.com/Baldev8910/LegendBar)

<!-- Add a screenshot here: a full-width screenshot of both monitors showing the bar at the top with the media widget, clock, date, and settings icon visible. Save it as screenshot.png in the root of the repo and replace this comment with: ![LegendBar Screenshot](screenshot.png) -->

---

## Features

### 🎨 Interface & Appearance

- Acrylic, Mica, Mica Alt, and Solid material support
- Live acrylic tint and blur customization
- Smooth auto-hide with cubic-ease animations
- Borderless always-on-top WinUI 3 window
- Thin Windows 11-style bottom border
- Dynamic popup material synchronization
- Adjustable bar height and animation speeds

---

### 🖥️ Window & Monitor Integration

- Full dual-monitor support
- Windows AppBar API integration for pinned mode
- Auto-hide detection using low-level mouse hooks
- Proper reserved screen space when pinned
- Hidden from Alt+Tab and taskbar switchers
- Re-asserts topmost state automatically

---

### 🎵 Media Controls

- System-wide media controls (Spotify, Chrome, Firefox, VLC, etc.)
- Play, pause, previous, and next controls
- Click song title to focus media app
- Per-app volume control via mouse wheel
- Animated volume overlay indicator
- Dynamic media session detection

---

### ⏰ Productivity Widgets

- Pomodoro timer with animated transitions
- Expandable timer controls (pause/reset/skip)
- Reminder system with recurring schedules
- Clipboard history with image support
- Quick Notes popup with Markdown + KaTeX rendering
- Clock and Date widgets
- PowerToys shortcuts reference panel
- DevToys launcher integration

---

### ⚙️ Customization & Settings

- Live settings preview
- Launch on startup support
- Widget visibility toggles
- Persistent JSON-based settings
- Reset-to-defaults support
- Dynamic popup resizing

---

### 🛠️ Technical Highlights

- Built with WinUI 3 and .NET 8
- Uses DesktopAcrylicController for true acrylic blur
- Uses Windows System Media Transport Controls (SMTC)
- Uses low-level Win32 hooks for edge detection
- Uses WebView2 for the Notes editor
- Uses NAudio for per-app audio sessions
- JSON-based persistence system
- Timer-driven custom animation engine

---

## Requirements

| Requirement | Details |
|---|---|
| Operating System | Windows 11 |
| Architecture | x64 |
| Display Scaling | 100% DPI scaling on all monitors |
| Runtime | Windows App Runtime |

> [!WARNING]
> LegendBar currently relies on fixed DPI calculations for monitor layout and popup positioning.  
> Running at DPI scales other than 100% may cause visual alignment issues.

---

## Installation

1. Go to the [Releases](https://github.com/Baldev8910/LegendBar/releases/latest) page
2. Download the latest installer
3. Run `LegendBarSetup.exe`
4. Launch LegendBar from the Start Menu

> [!IMPORTANT]
> Windows App Runtime is required.  
> If it is not already installed, the setup process will prompt you automatically.

> [!NOTE]
> Developer Mode may be required for sideloaded MSIX installation on some systems.

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

## Uninstalling

1. Open **Settings → Apps → Installed Apps**
2. Locate **LegendBar**
3. Click **Uninstall**

---

## Known Limitations

- Only **100% DPI scaling** is currently supported across all monitors
- Browser volume scrolling adjusts the Windows audio session volume, not in-page player volume (e.g. YouTube volume slider)
- When multiple media apps are playing simultaneously, title focus detection may occasionally target the wrong app
- Windows 10 is currently unsupported and untested

---

## Built With

[![WinUI 3](https://img.shields.io/badge/WinUI%203-Docs-6A5ACD?style=flat-square&logo=microsoft&logoColor=white)](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/)
[![Windows App SDK](https://img.shields.io/badge/Windows%20App%20SDK-Docs-0078D6?style=flat-square&logo=windows&logoColor=white)](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/)
[![WebView2](https://img.shields.io/badge/WebView2-Docs-0078D6?style=flat-square&logo=microsoftedge&logoColor=white)](https://developer.microsoft.com/en-us/microsoft-edge/webview2/)
[![NAudio](https://img.shields.io/badge/NAudio-GitHub-333333?style=flat-square&logo=github&logoColor=white)](https://github.com/naudio/NAudio)
[![C#](https://img.shields.io/badge/C%23-.NET%208-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Win32](https://img.shields.io/badge/Win32%20APIs-Reference-0078D6?style=flat-square&logo=windows&logoColor=white)](https://learn.microsoft.com/en-us/windows/win32/)
[![SMTC](https://img.shields.io/badge/SMTC-Media%20Controls-6A5ACD?style=flat-square)](https://learn.microsoft.com/en-us/uwp/api/windows.media.systemmediatransportcontrols)

---

## License

![License](https://img.shields.io/badge/License-MIT-22c55e?style=for-the-badge)

This project is released under the [MIT License](LICENSE).

---

## Acknowledgements

LegendBar was built as an experimental Windows 11 desktop enhancement project focused on acrylic composition, low-level window management, and productivity tooling.

Inspired by the flexibility of custom desktop environments and utility bars found across Linux and macOS ecosystems — rebuilt for native Windows 11 using WinUI 3 and Win32 APIs.