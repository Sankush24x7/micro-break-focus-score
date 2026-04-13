# Micro-Break Reminder with Focus Score

A lightweight Windows tray app that monitors activity signals (not keystroke content), detects focus/idle patterns, nudges 20-20-20 eye breaks, and shows a daily focus score with trend insights.

## Why This Tool

Long uninterrupted screen time can reduce focus quality and increase eye strain.  
This tool helps by:

- reminding short visual breaks at the right time
- highlighting focus quality using transparent metrics
- keeping all data local for privacy
- running silently with low CPU and memory usage

## What It Does

- Tracks keyboard/mouse activity via event-driven Windows hooks
- Detects active vs idle time
- Detects frequent switching as distraction signals
- Computes daily focus score (`0-100`)
- Triggers break reminders using 20-20-20 logic
- Tracks break adherence (`Breaks Taken / Breaks Due`)
- Shows:
- dashboard cards
- hourly focus/activity graph
- weekly trend panel
- dark mode (persisted across restarts)
- CSV export for weekly report

## How It Works

1. App starts in system tray (`NotifyIcon`)
2. Global low-level input hooks publish activity timestamps
3. Focus engine aggregates per-second into:
- active/focus/idle seconds
- interruptions and app switches
- break due / break taken states
4. Reminder service shows non-intrusive notifications and handles fullscreen suppression
5. Dashboard renders live snapshots and weekly trend
6. Stats and settings are saved under `%LOCALAPPDATA%\MicroBreakReminder`

## Privacy and Security

- No key content is captured
- No keystrokes are stored
- No screenshots/content inspection
- No internet calls / telemetry
- Local JSON files only

## Project Structure

- `Program.cs`: app entry and crash logging
- `TrayApplicationContext.cs`: tray lifecycle, menu actions, export, dashboard orchestration
- `Services/ActivityTracker.cs`: keyboard/mouse/window hook capture
- `Services/FocusEngine.cs`: focus and break computation
- `Services/NotificationService.cs`: reminder notification delivery
- `Persistence/StatsStore.cs`: daily stats load/save + CSV export
- `Persistence/SettingsStore.cs`: user preferences (dark mode)
- `UI/DashboardForm.cs`: dashboard and theme transition
- `UI/ActivityGraphControl.cs`: hourly graph
- `UI/WeeklyTrendControl.cs`: weekly trend chart + hover tooltip

## Libraries and APIs Used

### Framework
- `.NET 6 (net6.0-windows)`
- `System.Windows.Forms` for tray app and dashboard UI

### Native Windows Interop (P/Invoke)
- `user32.dll`
- `SetWindowsHookEx`, `UnhookWindowsHookEx`, `CallNextHookEx`
- `SetWinEventHook`, `UnhookWinEvent`
- `GetForegroundWindow`, `GetWindowRect`, `GetWindowText`
- `kernel32.dll`
- `GetModuleHandle`

### Built-in .NET Libraries
- `System.Text.Json` for local storage
- `Microsoft.Win32.Registry` for optional startup entry (HKCU Run)
- `System.Drawing` / GDI+ for custom charts
- `System.Media` for optional reminder sound

No third-party NuGet package is required.

## Build

```powershell
dotnet build .\MicroBreakReminder.csproj -c Debug
```

## Publish Single Portable EXE

```powershell
dotnet publish .\MicroBreakReminder.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:PublishTrimmed=false
```

Output:

`bin\Release\net6.0-windows\win-x64\publish\MicroBreakReminder.exe`

## Run

Start the executable and use tray options:

- `View Dashboard`
- `Pause Tracking`
- `Export Weekly CSV`
- `Snooze Reminder (5 min)`
- `Run At Startup`
- `Exit`

## Performance Notes

- Event-driven tracking (no hot polling loop)
- Lightweight timer aggregation
- Designed for near-zero idle CPU usage
- Typical memory footprint fits lightweight desktop utility targets
