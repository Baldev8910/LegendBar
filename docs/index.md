# Release Notes

## Version 1.1.6.7

### Bugs Fixed

**#BF1 — PowerToys icon path hardcoded to a specific user directory**

`LoadPowerToysIcon()` was referencing `C:\Users\XXXXX\AppData\Local\PowerToys\PowerToys.exe` — a hardcoded path that silently failed on any machine where the username wasn't `ADMIN`. Fixed by resolving the path dynamically via `Environment.SpecialFolder.LocalApplicationData`, making it work correctly on any machine regardless of username or Windows drive. Additionally, if PowerToys is not installed at all, the PowerToys button is now automatically hidden from the bar rather than showing a blank icon that opens an empty popup.

**#BF2 — Bar and popups misaligned at non-100% DPI scaling**

At display scaling above 100% (e.g. 125%, 150%), several issues were present: the bar did not hide completely off screen, hover detection only worked on part of the bar width, and all popups appeared at incorrect positions. Root causes: `HiddenY()` in `AutoHideHelper` was not accounting for DPI scale when calculating the off-screen position; the mouse hook callback was comparing physical pixel coordinates against logical X bounds; `CheckTimer_Tick` was comparing physical cursor coordinates against a mix of logical and physical values; and all popup positioning was using `LogicalBounds` coordinates directly in `MoveAndResize` which expects physical pixels. Fixed by introducing `MonitorHelper.PrimaryDpiScale` and a `ToPhysical()` helper, scaling `HiddenY()` and all mouse hook thresholds correctly, and wrapping all popup `MoveAndResize` calls with `ToPhysical()` across all six popup files — `AddReminderPopup`, `ClipboardHistoryPopup`, `NotesPopup`, `PomodoroPopup`, `PowerToysPopup`, and `ViewRemindersPopup`.

### Improvements

**#I1 — Widget and behavior toggles aligned to right edge in Settings**

`ToggleSwitch` controls across the Widgets and Behavior pages in the Settings window were not sitting flush against the right edge of their cards due to WinUI 3's default `MinWidth` of ~154px on the `ToggleSwitch` control. Fixed by setting `MinWidth="0"` and `HorizontalAlignment="Right"` on all toggle switches, allowing them to shrink to their natural size and align correctly to the card boundary.

## Version 1.1.6.6

### Features

**#F1 — NTP-synced clock with configurable format**

The clock widget now synchronizes with WorldTimeAPI (`worldtimeapi.org/api/ip`) on startup and every 6 hours automatically, calculating a precise offset between the API time and the local clock (accounting for network round-trip latency using midpoint correction). The local tick timer continues running every second using the cached offset, so the display never stutters or jumps. If the sync fails, the widget silently falls back to local system time using the last known offset. A new Clock Format selector in Settings → Widgets lets the user choose between `HH:mm`, `HH:mm:ss`, `h:mm tt`, and `h:mm:ss tt`. The selected format persists across restarts via `AppSettings.ClockFormat` and is applied live on the next tick with no restart required.

**#F2 — Weather widget updates on internet reconnection**

The weather service now monitors network connectivity via `NetworkInformation.NetworkStatusChanged`. When the internet connection is lost, a `_wasOffline` flag is set. When connectivity is restored, `RefreshAsync()` is triggered immediately rather than waiting for the next 30-minute scheduled refresh. This ensures the weather widget and popup always show current data after a network interruption without requiring a bar restart.

**#F3 — Weather widget visibility toggle in Settings**

Added a Weather toggle to the Widgets page in Settings under the Right section, between Clipboard and the Center group. Toggling it off hides both the weather widget and its accompanying separator in a single operation by wrapping them in a shared `StackPanel` (`WeatherWidgetContainer`), preventing orphaned dividers from appearing when the widget is hidden. The toggle state persists across restarts via `AppSettings.ShowWeather` (default: `true`).

### Bugs Fixed

**#BF1 — Popup and control files reorganised into Popups\ and Controls\ folders**

All popup window files (`AddReminderPopup`, `ClipboardHistoryPopup`, `NotesPopup`, `PomodoroPopup`, `PowerToysPopup`, `ReminderNotificationPopup`, `ViewRemindersPopup`, `WeatherPopup`) have been moved from the project root into a new `Popups\` folder. `PomodoroControl` has been moved into a new `Controls\` folder. Namespaces updated to `LegendBar.Popups` and `LegendBar.Controls` respectively. All referencing files updated with the corresponding `using` directives and `x:Class` declarations. The defunct `SettingsPopup.xaml` was removed entirely.

## Version 1.1.6.5

### Features

**#F1 — Weather widget and popup**

A weather widget sits in the bar between the Clipboard and Date widgets, showing a Meteocons Lottie animated icon and the current temperature. Clicking it opens `WeatherPopup`, a full weather detail popup showing city name, current date/time, large animated weather icon, temperature, feels like, condition description, humidity, wind speed and compass direction, UV index, precipitation, visibility, cloud cover, sunrise and sunset times, and a horizontally scrollable 7-day forecast strip with per-day animated icons, high/low temperatures, and precipitation totals.

Weather data is fetched from the Open-Meteo API (free, no API key required). Location is detected automatically in two stages: Windows Location API first, falling back to ip-api.com. City name is resolved via reverse geocoding using the Nominatim OpenStreetMap API with a `LegendBar/1.0` User-Agent header. Data is cached to `AppData\Roaming\LegendBar\weather_cache.json` and loaded instantly on startup while fresh data fetches in the background. The cache expires after 1 hour and a `System.Threading.Timer` refreshes data every 30 minutes automatically.

If Meteocons Lottie JSON files are present in `Assets/Weather/`, animated icons are shown via `AnimatedVisualPlayer` and `LottieVisualSource`. If a file is missing, the widget and popup fall back gracefully to a Unicode weather emoji so the bar remains functional on machines without the icon assets. Temperature unit respects `SettingsService.Current.TemperatureUnit` (`"C"` or `"F"`) throughout both the widget and popup. The popup height is dynamic, capped at 700px, and position is right-aligned to the primary monitor edge below the bar.

**#F2 — Full-screen app detection**

Added a full-screen watcher that runs every 2 seconds and detects when a true exclusive full-screen application (such as a game) is running on the primary monitor. When detected, the bar completely backs off — the topmost timer stops, the mouse hook is unhooked, and all auto-hide timers are paused — preventing the bar from fighting the full-screen app for focus and causing it to minimize. When the full-screen app is closed, the mouse hook is reinstalled, timers resume, and the bar reinstates itself as topmost automatically. Regular maximized windows such as browsers are excluded via a `WS_CAPTION` style check, so the bar behaves normally during everyday use.

**#F3 — Pinned app & file launcher**

Replaced the hardcoded DevToys launcher with a fully configurable pins system. Users can now pin up to 10 files or applications of any type directly from the Settings window via a file picker. Pinned items appear in the bar as icon-only buttons with the actual filesystem icon pulled from the file or executable, and a tooltip showing the item name on hover. Clicking a pin launches the target with its default application via `UseShellExecute`. Pins are managed from a new **Pins** page in the Settings window, where items can be added, removed, and reordered. All pins persist across sessions via the existing JSON settings file.

### Bugs Fixed

**#BF1 — Bar shadow bleeding onto windows when pinned**

When the bar was pinned, its DWM shadow was casting onto windows positioned beneath it, making them appear trapped behind the bar rather than simply below it in the z-order. Root cause: `DwmExtendFrameIntoClientArea` called during the pin sequence was re-enabling DWM non-client rendering as a side effect, overriding the `DWMNCRP_DISABLED` attribute (`DWMWA_NCRENDERING_POLICY = 2`) set at startup. Fixed by explicitly re-applying `DwmSetWindowAttribute(hWnd, 2, ref noShadow, sizeof(int))` immediately after both `DwmExtendFrameIntoClientArea` calls in `PinButton_Click` — once outside the timer and once inside the timer tick.

**#BF2 — Bar forcibly hiding when full-screen app detected**

When a full-screen application was detected, the bar was calling `ForceHide()` and sliding itself off the top of the screen. This was unintended — the bar should simply stop asserting topmost and let the full-screen app sit over it naturally. Fixed by removing the `ForceHide()` call and switching from `HWND_TOPMOST` to `HWND_NOTOPMOST` when full-screen is detected, so the bar stays in place and the full-screen app covers it without any conflict.

### Improvements

**#I1 — About page implemented in Settings**

The About page in the Settings window was previously a stub with no content. Now displays app name, version (read dynamically from the MSIX package manifest), framework info, all open-source dependencies with their versions, and quick-link cards for the Release Notes and GitHub pages that open in the system browser on click.

## Version 1.1.6.4

### Features

**#F1 — New Settings Window (WinUI 3 NavigationView)**

Replaced the old compact `SettingsPopup` (TabView-based floating window) with a proper full WinUI 3 settings window `(SettingsWindow)` modeled after the Windows 11 Settings app. The new window features:

- `NavigationView` with a left pane and four pages: Appearance, Widgets, Behavior, About (About in progress)
Working search box with AutoSuggestBox that filters and navigates to the relevant settings page
- Mica material always applied, independent of the bar's material setting
- Resizable window with minimize and maximize buttons
- Window position and size persisted across sessions via SettingsService
- Clean single-row card layout — label on the left, control flush to the right
- Acrylic opacity sliders only visible when Acrylic material is selected
- Tint color input only visible when Acrylic or Solid material is selected
- Proper title bar with drag region

**#F2 — Dynamic PowerToys Shortcuts Panel**

The PowerToysPopup now reads `%LocalAppData%\Microsoft\PowerToys\settings.json` at open time and shows only the modules the user has enabled in PowerToys. Each module's shortcut section is wrapped in a named `StackPanel` and its `Visibility` is set to `Collapsed` or `Visible` based on the `enabled` map in the JSON. A `FileSystemWatcher` monitors the file for changes and re-applies visibility via `ApplyPowerToysVisibility()` with a 200ms debounce to allow PowerToys to finish writing before parsing. The popup height is fully dynamic — `MainStackPanel.Measure()` recalculates after every visibility change and `AppWindow.MoveAndResize()` snaps the window to fit the visible content exactly. Path resolution uses `Environment.SpecialFolder.LocalApplicationData` so it works on any machine regardless of username or install location.

### Bugs Fixed

**#BF1 — White line at top of all popup windows**

All popup windows (`ClipboardHistoryPopup`, `PomodoroPopup`, `ViewRemindersPopup`, and others) displayed a thin white line at the very top of the window frame despite `SetBorderAndTitleBar(false, false)` being set. Root cause: WinUI 3 does not fully strip the `WS_CAPTION` window style when using `OverlappedPresenter`, leaving a caption remnant that DWM renders as a bright strip. Fixed by explicitly stripping `WS_CAPTION` via `SetWindowLong(hWnd, GWL_STYLE, style & ~WS_CAPTION)` called at the very top of each popup's `SetupWindow()`, before any other window configuration. A try/catch wraps the call to suppress the non-fatal Win32 SEH exception that Visual Studio intercepts during the style change.

**#BF2 — Material not syncing to View Reminders and Clipboard popups**

`ViewRemindersPopup` and `ClipboardHistoryPopup` were missed in the earlier material sync pass. Both now correctly switch between Acrylic, Mica, and MicaAlt based on `SettingsService.Current.MaterialType`, consistent with all other popups in the bar.

**#BF3 — Right-click context menu animates down with the bar when it auto-hides**

The `MenuFlyout` is anchored to the bar window, so when the auto-hide animation triggered while the menu was open, the menu moved with the bar. Fixed by calling `SetExternalWindowOpen(true)` when the flyout opens and `SetExternalWindowOpen(false)` on its `Closed` event, keeping the bar locked in position for the duration of the menu.

**#BF4 — Unhandled Win32 exception crashing the bar during Clipboard and Notes use**

The bar was intermittently crashing with an unhandled Win32 exception, most reproducible during Clipboard history and Quick Notes interactions. Root cause was a combination of issues across multiple files that together created conditions for exceptions to escape managed handlers entirely.

**#BF5 — Mouse hook callback had no exception handling**

`AutoHideHelper.MouseHookCallback` had no try/catch around `Marshal.PtrToStructure<MSLLHOOKSTRUCT>()`. Any exception inside a `WH_MOUSE_LL` hook callback escapes to the Win32 level and becomes an unhandled exception. Wrapped the callback body in a try/catch with Debug logging.

**#BF6 — Mouse hook delegate could be garbage collected**

`_mouseProc` was declared as an instance field, meaning the GC could collect the delegate while the native hook still held a pointer to it, causing a callback into freed memory. Changed to a `static` field to keep the delegate alive for the process lifetime.

**#BF7 — Animation timer leak in `AutoHideHelper.AnimateTo`**

Every show/hide animation created a new `DispatcherQueueTimer` without stopping the previous one. Rapid open/close of popups stacked dozens of timers all calling `MoveAndResize` simultaneously on the bar window. Fixed by tracking the animation timer in `_animTimer` and stopping it before starting a new animation.

**#BF8 — Mouse hook never unhooked on shutdown**

`UnhookWindowsHookEx` was never called when the bar closed, leaving a dangling hook. Added `Dispose()` to `AutoHideHelper` and wired it to `MainWindow.Closed`.

**#BF9 — Clipboard debounce timer leak**

`ClipboardWidget.Clipboard_ContentChanged` created a new `DispatcherQueueTimer` on every clipboard change event, stacking async read operations that fought each other. Refactored to a single reusable timer initialized once, with the tick logic moved to a named `Debounce_Tick` method.

**#BF10 — Clipboard event never unsubscribed**

`Clipboard.ContentChanged` was subscribed in the constructor but never unsubscribed, causing the handler to keep firing even after the widget was unloaded. Added unsubscription on `Unloaded`.

**#BF11 — WebView2 `NavigationCompleted` handler stacking in `NotesPopup`**

`InitWebView` used `+=` to subscribe `PreviewView_NavigationCompleted` without ever unsubscribing first. If `InitWebView` ran more than once, scripts would execute multiple times per navigation. Fixed by adding `-=` before `+=`.

**#BF12 — `ExecuteScriptAsync` called on failed navigation**

`PreviewView_NavigationCompleted` called `ExecuteScriptAsync` regardless of whether navigation succeeded, risking a call into a broken WebView2 environment. Added an `args.IsSuccess` guard at the top of the handler.

**#BF13 — `SettingsService.Save()` called twice unconditionally in `PinButton_Click`**

Both `IsPinned = true` and `IsPinned = false` assignments ran at the end of `PinButton_Click` regardless of which branch executed, always saving `false`. Replaced with a single `SettingsService.Current.IsPinned = _isPinned` using the already-toggled value.

**#BF14 — `_topmostTimer` used but never initialized**

`_topmostTimer` was declared and referenced throughout `MainWindow` but never created, meaning all `?.Stop()` and `?.Start()` calls silently did nothing. Added proper initialization in `SetupWindow()` after `_autoHide` is created.

**#BF15 — Pomodoro color animation timer leak**

`AnimateColor` created a new `DispatcherQueueTimer` on every call. Rapid state changes (focus → pause → resume) stacked multiple color timers running simultaneously. Refactored to a single reusable `_colorTimer` with tick logic in a named `ColorTimer_Tick` method. The pill brush is now tracked as `_pillBrush` field to be accessible across methods.

**#BF16 — Pomodoro expand/collapse timer leak and wrong handler**

`ExpandActions` and `CollapseActions` each created new timers and used separate tick handlers. When `CollapseActions` reused `_expandTimer` created by `ExpandActions`, it still had the expand tick handler attached and would expand instead of collapse. Refactored to a single `_expandTimer` with a unified `ExpandCollapseTimer_Tick` method controlled by an `_expanding` direction flag.

**#BF17 — Pomodoro `Reset_Click` left state as `Paused` instead of `Idle`**

After reset, `_state` was set to `PomodoroState.Paused` and the pill turned red, which was confusing and incorrect. Reset now sets state to `Idle`, collapses the actions panel, and animates the pill back to transparent.

**#BF18 — Pomodoro `PausePlay_Click` was performing a reset instead of pausing**

The pause branch had reset logic accidentally placed inside it during an earlier edit, stopping the timer and clearing state instead of simply pausing. Restored correct pause/resume behavior.

**#BF19 — Pomodoro session auto-advance did not restart the timer**

When `_remaining` reached zero in `Timer_Tick`, the session flipped and color animated but `_timer` was not restarted, silently stopping the countdown. Also, a stray `_timer?.Start()` was placed outside the `if` block, restarting the timer on every tick. Fixed by removing the stray call and keeping the restart inside the session-flip block only.

**#BF20 — Pomodoro `SkipRequested` started timer regardless of state**

`_timer?.Start()` was called unconditionally in the skip handler, starting the timer even when the session was idle or paused. Guarded with a state check so the timer only restarts if already in `Focus` or `Break` state.

**#BF21 — Pomodoro popup events wired after `Activate()`**

`TimersChanged` and `SkipRequested` were subscribed after `_popup.Activate()`, creating a window where events fired before handlers were attached. Moved all event subscriptions to before `Activate()`.

**#BF22 — `ReminderService` timer leak in `ScheduleNext`**

Every call to `ScheduleNext` created a new `DispatcherQueueTimer`, accumulating timers over time. Refactored to reuse a single `_preciseTimer`, only updating its `Interval` before each start.

**#BF23 — `ReminderService.Save` could throw uncaught from a timer tick**

`Save()` had no exception handling. A file lock or disk error during a timer tick callback would become an unhandled exception. Wrapped in try/catch with Debug logging.

### Improvements

**#I1 — Global unhandled exception logging**

Added `App.UnhandledException`, `TaskScheduler.UnobservedTaskException`, and `AppDomain.CurrentDomain.UnhandledException` handlers in `App.xaml.cs`. All unhandled exceptions are now written to `%AppData%\LegendBar\crash.log` with full stack traces instead of silently crashing.

**#I2 — Bare `catch { }` blocks replaced with logged catches**

All silent `catch { }` blocks across `NotesPopup`, `ClipboardWidget`, `ReminderService`, and `MainWindow` now log the exception message to the Debug output, making issues visible during development without affecting release behavior.

**#I3 — `Activated` deactivation guard hardened in popups**

The `_loaded` flag pattern in `ClipboardHistoryPopup` and `NotesPopup` could close the popup prematurely if `Activated` fired with a `Deactivated` state before the window fully showed. Updated to only set `_loaded = true` on a non-deactivated activation event.

---

## Version 1.1.6.3

### Features

**#F1 — Material sync across the popups**

The bar now syncs the material (Acrylic, Mica, and Mica Alt) across the popups of the bar (Settings, Timer, Add Reminder, Reminder Notification, PowerToys, and Quick Notes), except the View Reminders and Clipboard.

### Bugs Fixed

**#BF1 — Bar content appearing above vertical center**

`RootGrid` had `Margin="0,-1,0,0"` to hide WinUI's 1px border, but `Padding="0,0,0,1"` was compensating on the wrong side, pushing all content upward. Fixed by changing padding to `Padding="0,1,0,0"` and adding `VerticalAlignment="Center"` to `ContentGrid`.

**#BF2 — Browser window overlapping bar when pinned**

When the bar is pinned, browser windows were overlapping 1-2px of the bar's bottom edge due to the AppBar reservation being exact to the bar height. Increased the reserved height by 3px in `AppBarHelper.RegisterSingleAppBar()` to push windows fully below the bar. Additionally, pinning the bar now removes the topmost flag so browser windows can sit above the bar, while unpinning restores topmost behavior.

**#BF3 — Pomodoro popup rendering at default full size in screen center**

`MoveAndResize` call was accidentally removed from `PomodoroPopup.SetupWindow()` during the material sync fix, causing the popup to render at WinUI's default window size centered on screen. Restored `MoveAndResize` with corrected dimensions `270x320`.

### KnownBugs

**#B1 — White line over all the popups**

Investigating the white line over the popups. It seems like WinUI3 issue. Working on the fix using the WinUI3 Gallery and Windows SDK Tools.

---

## Version 1.1.6.2

### Features

**#F1 — Borderless always-on-top window**

The bar is a borderless WinUI 3 window using `OverlappedPresenter` with `SetBorderAndTitleBar(false, false)` and `ExtendsContentIntoTitleBar`. It is always on top via `HWND_TOPMOST` re-asserted every second using `SetWindowPos`, and is hidden from the taskbar and Alt+Tab switcher (`IsShownInSwitchers = false`). DWM attributes are set for dark mode, no shadow, no rounded corners, and full acrylic coverage.

**#F2 — Auto-hide with smooth cubic-ease animation**

The bar hides to a 1px sliver when the mouse moves away and slides back down when the mouse reaches the top edge of the screen. Show and hide transitions use a manually interpolated cubic-ease animation via `DispatcherQueueTimer` at 16ms intervals, driving `AppWindow.MoveAndResize`. Durations are configurable (default: 150ms show, 200ms hide, 300ms delay).

**#F3 — Low-level mouse hook for show trigger**

A `WH_MOUSE_LL` Win32 hook detects the mouse reaching the top edge of the screen even when another window's resize handle is sitting at `y=0`. This ensures the bar appears reliably over maximized windows, which the previous polling-based approach could not handle.

**#F4 — Pin mode via Windows AppBar API**

When pinned, the bar registers with Windows using `SHAppBarMessage`, reserving screen space at the top edge and pushing all maximized windows below it — identical to native taskbar behavior. The `_topmostTimer` stops when pinned and restarts when unpinned to avoid conflicting with AppBar z-order management. The bar re-registers automatically after the system wakes from sleep via `WM_POWERBROADCAST`. Pin state is persisted across restarts.

**#F5 — Pin icon button**

A pin icon button on the left side of the bar reflects the current pin state. The icon dims to 50% opacity when unpinned and brightens to 100% when pinned. Uses custom SVG icons (`pinned.svg` and `unpin.svg`) from `Assets/Pins/`. Tooltip updates accordingly.

**#F6 — Acrylic frosted glass background**

True acrylic frosted glass is achieved using `DesktopAcrylicController` with configurable `TintOpacity` and `LuminosityOpacity`. The effect dynamically blurs the content behind the bar. Tint and blur changes apply instantly as a live preview by removing and re-adding the system backdrop target.

**#F7 — Material type selector**

The Appearance tab in Settings lets the user choose between Acrylic, Mica, Mica Alt, and Solid color as the bar's background material.

**#F8 — Tint color input**

The Appearance tab in Settings includes a hex color input (e.g. `#4A2C6B`) to change the bar's tint color. The Notes popup background syncs to the current tint color setting.

**#F9 — Right-click context menu**

Right-clicking anywhere on the bar opens a context menu at the cursor position with options: Add Reminder, View Reminders, Pin/Unpin, Settings, Exit. Styled with Segoe UI font to match Windows 11 aesthetics.

**#F10 — Clock widget**

Displays the current time in 24-hour format (`HH:mm`) as a WinUI 3 UserControl, updating every second.

**#F11 — Date widget**

Displays the current date in `"dddd, MMMM d"` format (e.g. "Friday, May 8") as a WinUI 3 UserControl, updating every minute.

**#F12 — Widget visibility toggles**

The Settings popup includes independent show/hide toggles for the Media Player, Clock, and Date widgets.

**#F13 — Settings popup**

A tabbed popup with three tabs — Appearance (material type, tint color, acrylic tint/blur sliders, bar height), Behavior (show speed, hide speed, hide delay, temperature unit), and System (launch on startup toggle, reset to defaults, version label). All changes to sliders apply instantly as a live preview. Settings persist to `AppData\Roaming\LegendBar\settings.json` and are loaded before the window initializes. The popup closes automatically on deactivation.

**#F14 — Reset to Defaults**

A button in the System settings tab restores all `AppSettings` properties to their default values and refreshes all UI controls.

**#F15 — Launch on Startup**

A toggle in the System settings tab registers or removes LegendBar from Windows startup using the Windows Startup Task API declared in `Package.appxmanifest`. Only functional when running as an MSIX-installed app.

**#F16 — Dual monitor support**

The bar spans both monitors using a dynamically calculated width and position from `MonitorHelper`, which detects all connected monitors at startup. No pixel values are hardcoded — `WinX`, `WinW`, `WinY`, `MouseXMin`, `MouseXMax`, and `PrimaryOffsetX` are all derived from actual monitor layout and DPI. A dummy `WS_POPUP` window registers the AppBar across the secondary monitor. Mouse detection spans the full virtual desktop so either monitor triggers bar show. `WM_DPICHANGED` is intercepted and swallowed to prevent WinUI from rescaling the window when it spans two monitors with different DPI contexts.

**#F17 — Weather widget**

> ⚠️ **Work in Progress — Currently Disabled** —
Displays current temperature and condition text on the left side of the bar. Weather data comes from the Open-Meteo API (free, no API key required). SVG weather icons from the Meteocons library (by Bas Milius) cover all major conditions: clear, cloudy, foggy, rainy, snowy, stormy, and hail. Location is detected via the Windows Location API (`Windows.Devices.Geolocation`) using WiFi scanning, falling back to IP-based detection via `ip-api.com`, then to a hardcoded fallback. Weather data is cached to disk and displayed instantly on startup while fresh data loads in the background (cache expires after 1 hour).

**#F18 — Weather popup**

> ⚠️ **Work in Progress — Currently Disabled** —
Clicking the weather widget opens a popup below the bar showing full current conditions: temperature, feels like, humidity, wind speed and direction, UV index, precipitation, visibility, cloud cover, sunrise/sunset times, and a 6-day forecast with icons and precipitation probability. The popup has a 300ms close delay when the mouse leaves, cancelling if the mouse re-enters before the delay expires.

**#F19 — Media controls widget**

A WinUI 3 widget using Windows System Media Transport Controls (SMTC) displays play/pause, previous, and next buttons alongside the current song title and artist. Works with Spotify, Firefox, Chrome, Edge, VLC, Screenbox, and any app that reports to SMTC. The widget fades in when media starts playing and fades out when no media is active. SVG media control icons from Lucide Icons (skip-back, play, pause, skip-forward) are used, with a custom `ControlTemplate` removing the default grey hover background.

**#F20 — Click song title to focus media app**

Clicking the song title in the media widget brings the media player app to the foreground. Works for browsers (Firefox, Chrome, Edge) and packaged apps (Screenbox). If the source app ID contains `!` it is treated as a UWP app and `ApplicationFrameHost` is focused instead.

**#F21 — Volume scroll on media widget**

Hovering over the media widget and scrolling up/down adjusts the per-app audio volume of the currently playing app (not system volume). Works via the Windows audio session API (NAudio).

**#F22 — Volume indicator**

A small acrylic box showing the current volume percentage appears between the controls and song title when scrolling. It fades out automatically after 1 second.

**#F23 — Reminders system**

A full reminder system supporting one-time, daily, weekly, and monthly repeat types. All reminders are stored permanently to disk in JSON format. The reminder timer fires at the exact scheduled time (not polled). Weekly reminders support individual day-of-week selection via toggle buttons (Su, Mo, Tu, We, Th, Fr, Sa). Monthly reminders use a `CalendarView` with `SelectionMode=Multiple` to select specific dates across months, stored as `List<DateTime> SpecificDates` in the `Reminder` model. The old `DayOfMonth` int is retained as a backwards-compatible fallback.

**#F24 — Add Reminder popup**

A popup window for creating reminders with a scrollable time picker (mouse wheel support for hours and minutes), a date picker, a title text box, and a repeat type dropdown. When Monthly is selected, the popup expands from 500px to 780px height to reveal the calendar picker, and shrinks back when switching to another type. Width is 420px, centered on the primary monitor.

**#F25 — View Reminders popup**

Lists all active reminders with a delete button per entry and an Add button to open the Add Reminder popup. Shows an empty state when no reminders exist.

**#F26 — Reminder notification popup**

When a reminder fires, a notification popup appears centered below the clock widget with a fade-in animation. It shows the reminder title and a Dismiss button. If the bar is hidden in auto-hide mode, it slides down automatically before showing the notification.

**#F27 — Clipboard history widget**

A clipboard icon button on the right side of the bar captures the last 10 clipboard items (text and images) in session memory. Clicking opens `ClipboardHistoryPopup` showing all captured items in a scrollable list. Text items can be clicked to re-copy. Images display as thumbnails (UniformToFill). A Clear All button empties the history. A 300ms debounce prevents screenshot double-capture.

**#F28 — Quick Notes widget**

A notes icon button (custom pen SVG) opens `NotesPopup`, a 420×460px popup containing a Toast UI Editor WYSIWYG instance loaded via WebView2. Notes auto-save on every keystroke to `AppData\LegendBar\notes.md` via `WebMessageReceived`. A folder button opens File Explorer with the notes file selected. The editor runs fully offline with all required files bundled in `Assets/Editor/` (`toastui-editor-all.min.js`, CSS, KaTeX, and KaTeX fonts). Background syncs with the bar tint color setting.

**#F29 — Notes math preview overlay**

A math preview button (∑ icon) in the Notes popup calls `window.showMathPreview()` via `ExecuteScriptAsync`. A JavaScript overlay appears inside the WebView2 showing each found formula with its source text above and KaTeX-rendered output below. Supports `$...$`, `$$...$$`, `\(...\)`, and `\[...\]` delimiters. A 400ms debounce after every editor change event re-renders formulas targeting `.toastui-editor-contents`. A Close button dismisses the overlay.

**#F30 — Pomodoro timer widget**

A pill-shaped button in the right group of the bar shows a countdown timer. Clicking it when idle opens `PomodoroPopup` with scrollable Focus (default 45:00) and Break (default 15:00) duration pickers and a Start button. Once started, the pill shows the live countdown. Sessions are infinite and alternate automatically between focus and break with a smooth color fade. Focus sessions are blue (`#4472C4`), break sessions are green (`#3AAA64`), paused state is red. All animations use `DispatcherQueueTimer` at 16ms — no XAML storyboards. Duration changes in the popup apply immediately to the running timer. Segoe UI Mono is used for the timer digits to prevent the pill from shifting size as digits change width.

**#F31 — Pomodoro hover expand**

Hovering over the Pomodoro pill expands it leftward to reveal a Pause/Play button and a Reset (X) button. The pill contracts back on mouse leave. The timer column never moves during expand/collapse thanks to a two-column Grid layout.

**#F32 — Pomodoro skip**

A skip button in the Pomodoro popup jumps to the next session regardless of whether the timer is running or paused.

**#F33 — DevToys launcher button**

A button in the right group of the bar launches DevToys via `Process.Start`. The button displays the actual DevToys app icon extracted from the exe using `StorageFile.GetThumbnailAsync(ThumbnailMode.ListView, 32)`.

**#F34 — PowerToys shortcuts panel**

A button displaying the PowerToys icon opens `PowerToysPopup`, a static 480×720px reference panel listing all user-configured PowerToys shortcuts grouped by feature. Features covered: Advanced Paste, Always on Top, Color Picker, Command Palette, Crop and Lock, FancyZones, Mouse Highlighter, Peek, PowerToys Run, Screen Ruler, Shortcut Guide, Text Extractor, Workspaces. All shortcuts that use the Windows key display the Windows 11 SVG logo instead of the ⊞ symbol.

**#F35 — Separator between widget groups**

A thin vertical `Rectangle` (`Width=1`, `Height=14`, `Fill="#33FFFFFF"`) visually separates the utility buttons (Pomodoro, DevToys, PowerToys, Notes, Clipboard) from the information widgets (Date, Settings).

**#F36 — Inno Setup installer**

A single `LegendBarSetup.exe` installer that handles Windows App Runtime installation, certificate trust, MSIX package installation, and creates a desktop shortcut pointing to the installed app via `shell:AppsFolder`.

**#F37 — Thin bottom border**

A 1px bottom border on the bar matches the Windows 11 taskbar border color (`#454548`).

### Bug Fixes

**#BF1 — Duplicate ContextMenu declaration in MainWindow.xaml**

Two `ContextMenu` blocks existed in the XAML, causing `XLS0501` and `MC3024` build errors. Removed the old Exit-only menu block and kept the newer one containing Add Reminder, View Reminders, and Exit.

**#BF2 — Ambiguous namespace references (System.Windows.Forms vs System.Windows)**

`Application`, `MessageBox`, `Button`, and `UserControl` types were ambiguous between `System.Windows.Forms` and `System.Windows`. Resolved by adding explicit `using` aliases.

**#BF3 — ReminderService double-instance bug**

`AddReminderWindow` was creating its own independent `ReminderService` instance, so reminders were saved to one instance but the event fired on another. Fixed by passing the shared `ReminderService` instance down from `MainWindow`.

**#BF4 — Reminder popup not appearing (IsActive always False)**

Saved reminders all had `IsActive = false` due to the deactivation logic running too aggressively. Fixed by refactoring with a `HashSet` to track which reminders had already fired in the current session.

**#BF5 — Weather popup appearing at wrong position before animating**

The popup briefly flashed at an incorrect position before moving to the correct one. Fixed by setting window position before making it visible and using an opacity animation for the reveal.

**#BF6 — Weather popup closing too quickly when moving mouse to it**

`MouseLeave` on the weather widget was triggering popup close before the mouse could reach the popup. Fixed by adding a 300ms delay that cancels if the mouse enters the popup in time.

**#BF7 — Bar flickering on first open**

Animation flickering was caused by manual timer-based interpolation. Switched to WPF's built-in `BeginAnimation` with GPU acceleration and `CubicEase`.

**#BF8 — Unused `_isAnimating` field causing CS0414 warning**

Removed the unused field from `AutoHideHelper`.

**#BF9 — CS0104 ambiguous reference for UserControl**

`UserControl` was ambiguous between `System.Windows.Controls` and `System.Windows.Forms`. Added explicit `using` alias.

**#BF10 — XDG0008 errors for new widget UserControls**

New widget user controls were not being recognized by the designer. Resolved by running Build → Clean Solution → Rebuild Solution to clear the designer cache.

**#BF11 — Duplicate Resource Include entries causing RG1000 build error**

Duplicate `ItemGroup` entries for SVG assets existed in the `.csproj`. Removed the duplicate.

**#BF12 — Mouse scroll area too small on time picker**

The scroll zone on the Add Reminder time picker was too narrow. Replaced `TextBlock` mouse wheel handlers with larger `Border` elements containing the digits, providing a 70×60px scroll zone.

**#BF13 — PlaceholderText not working on standard WPF TextBox**

Standard WPF `TextBox` doesn't support `PlaceholderText`. Replaced with ModernWpf's `ui:ControlHelper.PlaceholderText` attached property.

**#BF14 — Weather location showing wrong city when VPN was active**

IP-based location detection was returning the VPN exit location instead of the real one. Replaced with Windows Location API using WiFi network scanning, which works correctly regardless of VPN.

**#BF15 — Settings not persisting after restart**

`SettingsService.Load()` was never called before `SetupWindow()`. Added an explicit `Load()` call at the top of the `MainWindow` constructor.

**#BF16 — Settings file saving to wrong folder (TopBar instead of LegendBar)**

File path in `SettingsService` still referenced the old `TopBar` folder name. Updated to `LegendBar`.

**#BF17 — Bar not appearing over other windows when maximized**

The window resize handle at `y=0` was intercepting mouse input before the polling-based show trigger could fire. Fixed by switching to a low-level `WH_MOUSE_LL` hook that intercepts mouse events before any window processes them.

**#BF18 — Bar spilling onto adjacent monitors**

The `-8px` X offset and `1936px` width used in auto-hide mode extended beyond the primary monitor edge. Fixed by using `0px` X offset and `1920px` width when in pinned mode.

**#BF19 — Gap visible at top of screen in auto-hide mode**

The `SliverHeight` was set to `4px`, leaving a visible dark line. Reduced to `1px`.

**#BF20 — Black pixels at top of bar**

`DwmExtendFrameIntoClientArea` was not fully covering the window edges. Fixed by using `-1` for all margins to extend the frame across the entire window.

**#BF21 — Bar settings reverting to defaults on every restart**

Same root cause as #BF15 — `SettingsService.Load()` was not being called before window initialization in the WinUI 3 rewrite. Fixed by placing the `Load()` call as the first line of the constructor.

**#BF22 — Settings file saving to AppData\TopBar after WinUI 3 migration**

`SettingsService` path not updated during migration. Fixed to save to `AppData\Roaming\LegendBar`.

**#BF23 — DesktopAcrylicController not visually updating on slider change**

When only tint or blur was changed (not both), the acrylic effect wasn't re-rendering. Fixed by removing and re-adding the system backdrop target on every opacity change to force a full re-render.

**#BF24 — Bar hiding while dragging height slider in Settings**

`ForceHide()` was executing even when the settings popup was open. Fixed by checking if `_settingsPopup` is null before calling `ForceHide()`.

**#BF25 — Bar height not remembered across restarts**

`AppSettings` had `_barHeight` (with underscore prefix) which is invalid for JSON serialization. Renamed to `BarHeight`.

**#BF26 — Ambiguous AppWindow reference in AutoHideHelper**

Both `Windows.UI.WindowManagement.AppWindow` and `Microsoft.UI.Windowing.AppWindow` were in scope. Fixed by adding an explicit `using` alias pointing to `Microsoft.UI.Windowing.AppWindow`.

**#BF27 — CS0105 duplicate using directive after adding pin functionality**

`using LegendBar.Helpers` appeared twice in `MainWindow.xaml.cs`. Removed the duplicate.

**#BF28 — Gap between bar and windows when pinned**

AppBar API expects logical pixels but physical pixel values were being passed. Fixed by dividing `barHeight` by `1.25` before passing to `SHAppBarMessage`.

**#BF29 — Windows going behind bar instead of below it when pinned**

`_topmostTimer` was re-asserting `HWND_TOPMOST` every second, conflicting with AppBar z-order management. Fixed by stopping the timer when pinned and restarting it when unpinned.

**#BF30 — Settings popup not closing when clicking outside**

The `AppWindow_Changed` handler was empty. Replaced with a proper `Activated` event handler checking for `WindowActivationState.Deactivated`.

**#BF31 — Media widget holding last title after playback stops**

The media widget was keeping the previous song title and artist visible after playback ended. Fixed by clearing title/artist and fading out the widget when no media session is active.

**#BF32 — Switching away from Solid color material getting stuck**

Background wasn't being reset before applying a new material type, leaving the solid color in place. Fixed by resetting the background to transparent before applying the new material.

**#BF33 — Right-click menu appearing at top-left corner of bar**

The context menu was not using cursor position. Fixed so the menu appears at the cursor's current position.

**#BF34 — Trimming stripping NAudio COM interop in Release builds**

`PublishTrimmed` was enabled, causing NAudio's COM interop code to be silently removed in Release/MSIX builds. Volume scroll and title click worked in Visual Studio but not in the installed version. Fixed by setting `PublishTrimmed` to `False`.

**#BF35 — ReminderService folder path still referencing old name**

`ReminderService` was saving data to the `TopBar` folder. Updated to `LegendBar`.

**#BF36 — ReminderService using WPF DispatcherTimer in WinUI 3**

`DispatcherTimer` pulled in `System.Windows.Threading`, which is not available in WinUI 3. Replaced with `DispatcherQueueTimer`.

**#BF37 — AppBar registration gap above bar when pinning**

Window was being registered before being moved to its target position, letting the shell reposition it. Fixed by moving the window first, calling `Register()`, then moving again to override the shell repositioning.

**#BF38 — Shell repositioning window during AppBar registration**

`WM_WINDOWPOSCHANGING` was allowing the shell to reposition the window during pin. Fixed by intercepting `WM_WINDOWPOSCHANGING` with a `_blockWindowPos` flag during registration.

**#BF39 — Add Reminder popup closing immediately on open**

The `Activated` event was triggering `Deactivated` during the window's initial show sequence, immediately calling `this.Close()`. Fixed by adding a `_loaded` bool flag to skip the first deactivation event.

**#BF40 — Weekly day buttons clipping text**

Single-letter day labels (S, M, T etc.) were too narrow for the button bounds. Switched to two-letter abbreviations (Su, Mo, Tu, We, Th, Fr, Sa) with `Width=36` and `FontSize=11`.

**#BF41 — AddReminderPopup XAML structure broken**

The Divider and Buttons were incorrectly nested inside the Repeat `StackPanel` instead of being siblings at the root level. The Repeat `StackPanel` was also missing its closing tag. Fixed by correcting the nesting structure.

**#BF42 — MinDate on CalendarView could not be set via XAML string**

`MinDate="2025-01-01"` caused `WMC0055: Cannot assign text value into property of type DateTimeOffset`. Fixed by removing `MinDate` entirely — the calendar defaults to a reasonable range.

**#BF43 — Today property not bindable via x:Bind on CalendarView**

`x:Bind` on a field (`_today`) doesn't work in WinUI 3 — only public properties are bindable. Resolved by removing the `MinDate` binding entirely.

**#BF44 — CS1061: DateTime naming conflict in MonthlyCalendar_SelectedDatesChanged**

`List<DateTime>` caused the compiler to confuse the generic type argument `DateTime` with a self-reference. Fixed by switching from `d.Date.DateTime` to `d.UtcDateTime.Date`.

**#BF45 — Monthly reminder Save_Click blocked when no date picker value set**

`DatePicker.Date == null` guard was preventing saves for monthly reminders, which use the calendar picker instead. Fixed by checking repeat type first and only enforcing `DatePicker.Date` validation for non-monthly repeats, and checking `_selectedMonthlyDates.Count == 0` for monthly.

**#BF46 — Clipboard screenshot being captured twice**

Windows fires `ContentChanged` twice during a screenshot — once when the tool starts and once when the final crop is written. Fixed by adding a 300ms debounce timer in `Clipboard_ContentChanged` so only the final write is captured.

**#BF47 — DispatcherQueueTimer not found in ClipboardWidget**

`DispatcherQueueTimer` lives in `Microsoft.UI.Dispatching`, which was missing from the `using` directives. Fixed by adding the missing namespace.

**#BF48 — NotesPopup crashing on open (unhandled exception)**

WPF/WinForms namespaces (`System.Windows.Controls`, `System.Windows.Forms`, `System.Windows.Media.Media3D`) had been accidentally added by IntelliSense, causing type conflicts with WinUI 3. Fixed by removing all non-WinUI namespaces.

**#BF49 — WebView2 CreateAsync signature mismatch**

Multiple `CoreWebView2Environment.CreateAsync` overloads failed due to version-specific API differences. Resolved by using `EnsureCoreWebView2Async(null)` directly, letting WebView2 use its default cache location.

**#BF50 — NotesPopup editor showing "File not found"**

`Package.Current.InstalledLocation.Path` only works when installed as MSIX, not when running from Visual Studio. Fixed by switching to `AppContext.BaseDirectory` which correctly points to the build output directory.

**#BF51 — Notes not auto-saving on popup close**

`SaveNow()` was `async void` and the popup closed before `ExecuteScriptAsync` completed, losing unsaved content. Fixed by removing `SaveNow()` from the close handler and relying solely on `WebMessageReceived`, which fires on every keystroke.

**#BF52 — KaTeX math not rendering in Notes (showing raw LaTeX)**

Toast UI Editor's WYSIWYG mode constantly re-renders its DOM, wiping any injected `renderMathInElement` HTML. Fixed by running `renderMathInElement` on the editor's content container with a 400ms debounce after every change event, targeting `.toastui-editor-contents`. Both `$...$` and `$$...$$` are supported.

**#BF53 — Notes editor text invisible (white on white)**

`transparent` background could not override Toast UI Editor's internally injected light-colored styles. Fixed by replacing all `transparent` backgrounds with explicit `#1a1a1a` in `editor.html`.

**#BF54 — PomodoroPopup controls not found (InitializeComponent, FocusTimeText, BreakTimeText)**

The popup was created as a Blank Page instead of a Blank Window, making the XAML root a `<Page>` which broke code-behind linkage. Fixed by recreating as Blank Window (WinUI 3).

**#BF55 — ClipToBounds not found on StackPanel / Border in WinUI 3**

`ClipToBounds` is a WPF-only property. Fixed by using a `RectangleGeometry` clip set programmatically in code-behind, and later resolved entirely by restructuring the layout so clipping was no longer needed.

**#BF56 — Click event not found on Border**

`Border` has no `Click` event in WinUI 3. Fixed by replacing `Border` with a `Button` using a custom `ControlTemplate` that strips default hover/press visual states.

**#BF57 — PomodoroControl hover expansion collapsing when moving cursor to icons**

`PointerExited` on `RootGrid` was firing when the mouse moved from the timer area to child button elements (X and Pause). Fixed by checking actual pointer coordinates against `RootGrid.ActualWidth/ActualHeight` before collapsing, so only genuine exits trigger collapse.

**#BF58 — Timer box showing grey background on hover**

WinUI 3's default `Button` style applies a grey overlay on hover/press, overriding the programmatic background color. Fixed by applying a custom `ControlTemplate` to `TimerBorder` that binds `Background` via `TemplateBinding` and skips all visual state animations.

**#BF59 — Break color not applying after focus timer ends**

`ShowRunningState` was creating a new `SolidColorBrush` on each call, losing the reference used by `AnimateColor`. Fixed by checking `if (TimerBorder.Background is SolidColorBrush existing)` and animating the existing brush. Later refactored to a timer-based `AnimateColor(Color to)` that manages its own brush reference.

**#BF60 — XAML storyboards not animating ActionsPanel width**

XAML `Storyboard` targeting `StackPanel.Width` was unreliable for the hover expand animation. Replaced entirely with a `DispatcherQueueTimer` at 16ms intervals manually stepping the width, following the same pattern as `AutoHideHelper`.

**#BF61 — Math preview showing double backslashes (\\frac instead of \frac)**

Toast UI Editor escapes backslashes when saving markdown, turning `\frac` into `\\frac`. Fixed by applying `.replace(/\\\\/g, '\\')` to the markdown string before parsing for math formulas in `showMathPreview()`.

**#BF62 — Math preview showing escaped underscores (\\_)**

Toast UI Editor also escapes underscores as `\_` to prevent italic rendering. Fixed by applying `.replace(/\\_/g, '_')` to individual formula strings before passing to KaTeX — not to the entire markdown content, which would break the editor.

**#BF63 — Pomodoro action buttons too close together and misaligned from timer**

Button padding and spacing were not normalized across Pomodoro action buttons. Fixed by normalizing padding and spacing.

**#BF64 — Pomodoro pill shifting size as timer digits change width**

Variable-width digits caused the pill to expand and contract. Fixed by switching to Segoe UI Mono (fixed-width font) and setting an explicit `Width` on `TimerText`.

**#BF65 — Pomodoro actions panel leaving a phantom gap when collapsed**

The actions panel was set to `Width=0` when collapsed but still occupied layout space. Fixed by using `Visibility.Collapsed` instead.

**#BF66 — Pomodoro idle circle removed; timer always visible**

The idle hollow circle was unclickable in certain states. Replaced with an always-visible, always-clickable timer text.

**#BF67 — Pomodoro timer completely unclickable**

A fully transparent background was causing hit-testing to fail. Fixed by setting a near-transparent background (`#01000000`) and wrapping in a `Border` in the control template.

**#BF68 — Pomodoro popup opening off-screen (x=3353)**

`TransformToVisual` was returning coordinates in virtual dual-monitor space. Fixed by calculating position from primary monitor bounds directly.

**#BF69 — Pomodoro popup instantly closing on open**

`_topmostTimer` was stealing focus the moment the popup opened, immediately triggering deactivation. Fixed by stopping the timer in `OnBeforePopupOpen` before the popup activates.

**#BF70 — Pomodoro popup deactivating twice instantly**

Double deactivation was closing the popup before it could be interacted with. Fixed by adding a 500ms `_readyToClose` delay after open.

**#BF71 — Duplicate PopupOpened wiring for Pomodoro**

`PopupOpened` was wired twice, causing double `_topmostTimer` stop calls. Removed the duplicate subscription.

**#BF72 — PomodoroControl wired before _autoHide was initialized**

Early wiring of `PomodoroControl` events caused null reference calls on `_autoHide`. Fixed by ensuring `_autoHide` is initialized before `PomodoroControl` is wired up.

**#BF73 — ColumnDefinition named with x:Name causing crash**

`x:Name` on a `ColumnDefinition` is not supported in WinUI 3 and caused a runtime crash. Removed the name.

**#BF74 — OpenPopup() had duplicate null check and duplicate PopupOpened invocation**

A bad merge introduced a duplicate `if (_popup != null) return` guard and a duplicate `PopupOpened?.Invoke()` call. Cleaned up.

### Known Bugs

**#B1 — Only 100% DPI supported**

All monitor layout calculations use hardcoded physical pixel math. Running on any monitor at a DPI setting other than 100% will cause incorrect positioning of the bar and all popups. A related symptom: the black pixel line at the top of the bar requires `WinY = -4` as a workaround, and this value may need to change at other DPI scales.

**#B2 — Pin/unpin transition glitch**

There is a brief visual jump when pinning or unpinning the bar as the AppBar shell fights with the window position during registration. A 200ms delayed re-assert partially mitigates this but the glitch is still occasionally visible.

**#B3 — Bar occasionally drops from pinned position after long uptime or overnight**

The bar sometimes loses its pinned AppBar registration after extended uptime or after the system wakes from sleep. A mitigation via `WM_POWERBROADCAST` re-registration has been added but the issue is still being monitored.

**#B4 — Bar briefly loses HWND_TOPMOST status**

Certain apps (games, full-screen video players) aggressively claim topmost and can temporarily push the bar behind them. The 1-second reassertion timer mitigates this but does not fully eliminate it.

**#B5 — Settings popup position hardcoded to 1920px width**

The settings popup X position is calculated assuming a 1920px primary monitor. On screens with different resolutions the popup may not align correctly to the right edge.

**#B6 — Mica and Mica Alt materials do not sync to popups**

The Settings popup, Add Reminder popup, and Reminder Notification popup always use Acrylic regardless of the material selected for the main bar.

**#B7 — Reminder recurrence may not fire correctly in all edge cases**

The `GetNextTrigger()` logic for daily, weekly, and monthly recurrence has not been fully tested across all scheduling scenarios. Edge cases (e.g. DST transitions, months with fewer days) may produce incorrect next-trigger times.

**#B8 — Browser internal volume cannot be synced**

The media widget's scroll-to-volume feature only controls the Windows audio session volume for the media app process. Browser-internal volume sliders (e.g. YouTube's built-in volume control) are not affected.

**#B9 — Startup task only works as MSIX**

The Launch on Startup setting only functions correctly when LegendBar is installed via the MSIX installer. Running directly from Visual Studio does not register the startup task.

**#B10 — DevToys and PowerToys icon paths are hardcoded**

Both `LoadDevToysIcon()` and `LoadPowerToysIcon()` reference `C:\Users\ADMIN\...` hardcoded paths. These will silently fail on any machine where the apps are installed to a different location. Needs a settings-configurable path or auto-discovery before distribution.

**#B11 — Pomodoro pill expansion UX still being refined**

The hover expand animation and layout of the X and Pause icons inside the unified pill shape is still being iterated on. Timer text position and icon alignment may shift slightly during expand/collapse.

**#B12 — PomodoroPopup position is approximate**

The Pomodoro popup opens at a calculated X position (`primaryWidth - 340`), which approximates where the Pomodoro button sits in the bar. It may not align precisely below the button on all configurations.

**#B13 — Notes math preview not live**

KaTeX formulas in the Notes editor do not render inline as you type. Rendering only happens when the Math Preview button is clicked. Live rendering via CodeMirror 6 decorations was attempted but caused instability.

**#B14 — Notes editor background color not always synced**

The `setBackground()` call after `setContent()` in `PreviewView_NavigationCompleted` sometimes doesn't fully override all Toast UI Editor internal background colors, particularly `.toastui-editor-ww-container`, on certain render cycles.

**#B15 — Bar shadow dropping over windows below**

Investigated `DWMNCRENDERINGPOLICY` values. No clean fix found without introducing a white line artifact at the bar's bottom edge. Reverted to `noShadow = 2`. Shadow is a known DWM limitation for acrylic/mica borderless windows.

---
