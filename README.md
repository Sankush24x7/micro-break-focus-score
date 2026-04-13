# Micro-Break Focus Score

[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-0078D6?logo=windows&logoColor=white)](https://www.microsoft.com/windows)
[![.NET](https://img.shields.io/badge/.NET-6.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![App Type](https://img.shields.io/badge/app-System%20Tray%20Utility-0A7E8C)](#)
[![Privacy](https://img.shields.io/badge/privacy-local%20only-success)](#privacy-what-this-tool-captures)
[![Status](https://img.shields.io/badge/status-production--ready-brightgreen)](#)

Lightweight desktop background tool for Windows that tracks activity signals (not typed content), calculates focus quality, and gives smart break reminders.

## Why This Tool Helps Users

- Reduces eye strain with timely break nudges
- Improves focus awareness with a daily score and trends
- Encourages healthier work habits (posture + hydration reminders)
- Runs quietly in system tray with low overhead

## Privacy: What This Tool Captures

This app captures only activity signals needed for productivity metrics:

- Keyboard activity events (key pressed happened, not key value)
- Mouse activity events (move/click happened)
- Foreground window switches (for distraction pattern signals)
- Idle/active durations and derived metrics

This app does **not** capture:

- Typed characters / keystroke content
- Clipboard content
- Screen contents or screenshots
- Network telemetry uploads

All processing is local and data is stored under `%LOCALAPPDATA%\MicroBreakReminder`.

## How Break Alerts Work

### Base Break Rule
- Starts with 20-minute break reminder target

### Adaptive Break Intelligence
- Dynamic reminder window adjusts to ~12-25 minutes based on:
- long continuous active sessions
- high app/tab switching (distraction spikes)
- late working hours
- rising eye-strain risk score

### Break Actions
When break is due, user sees actionable reminder with:

- `Take Break`
- `Snooze 5m`
- `Skip`

### Additional Health Nudges
- Posture reminder cadence: every ~45 minutes of active work
- Hydration nudge cadence: every ~90 minutes of active work

## Main Features

- System tray app (pause, resume, dashboard, export CSV)
- Focus score + active/focus/idle metrics
- Hourly activity vs focus graph
- Daily timeline (focus/distracted/idle/break states)
- Weekly trend panel with tooltips
- Dark mode with persistence
- Weekly CSV export

## Installation / Setup

### Option 1: Run prebuilt EXE
1. Build or get `MicroBreakReminder.exe`
2. Run executable
3. Use tray icon to open dashboard

### Option 2: Build from source
```powershell
git clone https://github.com/Sankush24x7/micro-break-focus-score.git
cd micro-break-focus-score\MicroBreakReminder
dotnet build .\MicroBreakReminder.csproj -c Release
```

## Publish Single EXE (Portable)

```powershell
cd MicroBreakReminder
dotnet publish .\MicroBreakReminder.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:PublishTrimmed=false
```

Output:
`MicroBreakReminder\bin\Release\net6.0-windows\win-x64\publish\MicroBreakReminder.exe`

## How to Use

1. Launch app
2. It starts in system tray and tracks activity automatically
3. Open `View Dashboard` from tray menu
4. Monitor:
- Focus Score
- Breaks Taken
- Eye Strain Risk
- Adaptive Break Minutes
- Timeline + Weekly Trend
5. Export weekly summary via `Export CSV`

## Tech Stack

- C# / .NET 6 WinForms
- Native Windows hooks (`user32.dll`) for activity events
- NotifyIcon tray integration
- Local JSON persistence

## Project Layout

- `MicroBreakReminder/` main source code
- `MicroBreakReminder/Services` tracking, scoring, reminders
- `MicroBreakReminder/UI` dashboard and charts
- `MicroBreakReminder/Persistence` stats/settings storage

