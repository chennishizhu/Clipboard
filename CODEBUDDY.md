# CODEBUDDY.md

This file provides guidance to CodeBuddy Code when working with code in this repository.

## Project Overview

**ClipVault** — Windows 10+ clipboard manager built with C# + WPF + SQLite. Single solution, single project.

- Solution: `ClipVault/ClipVault.sln`
- Main project: `ClipVault/ClipVault/ClipVault.csproj`
- Target framework: `net10.0-windows` (requires .NET 8+ SDK, Windows only)

## Build & Run Commands

```bash
# Restore NuGet packages
dotnet restore ClipVault/ClipVault.sln

# Debug build
dotnet build ClipVault/ClipVault.sln --configuration Debug

# Release build
dotnet build ClipVault/ClipVault.sln --configuration Release

# Run (must be on Windows — WPF is Windows-only)
dotnet run --project ClipVault/ClipVault/ClipVault.csproj

# Publish self-contained executable
dotnet publish ClipVault/ClipVault/ClipVault.csproj -c Release -r win-x64
```

**IDE:** Open `ClipVault/ClipVault.sln` in Visual Studio 2022 or JetBrains Rider. F5 to debug.

**No test project exists** in this solution.

## Icon Files (Required Before Running)

Place two `.ico` files in `ClipVault/ClipVault/Resources/` before building:
- `icon.ico` — window titlebar icon
- `tray.ico` — system tray icon

Both can be the same file.

## Architecture

The app follows **MVVM** with clean layer separation. All singleton services are created at startup in `App.xaml.cs` and exposed as static properties.

### Entry Point & Service Lifecycle (`App.xaml.cs`)

- Single-instance guard via `Mutex`
- Initializes all four services on startup, disposes them on exit
- Main window starts hidden; system tray controls visibility

```csharp
App.Database   // DatabaseService
App.Clipboard  // ClipboardService
App.Settings   // SettingsService
App.Hotkey     // HotkeyService
```

### Models (`Models/`)

- **`ClipItem`** — Core data entity: `Id`, `Content`, `Type` ("text"/"link"), `CreatedAt`, `IsFavorite`, `IsPinned`. Computed: `RelativeTime`, `Preview`.
- **`AppSettings`** — User config: `DatabasePath`, `MaxRecords` (default 500), `AutoStart`, `GlobalHotkey` ("Ctrl+Shift+V"), window geometry, `Theme`.

### Services (`Services/`)

| Service | Responsibility |
|---------|---------------|
| `DatabaseService` | SQLite CRUD for `ClipItems` table; filtering by keyword/time/type; deduplication; WAL mode |
| `ClipboardService` | Registers Win32 clipboard format listener; classifies content as text or link (regex URL detection); ignores app's own copies; 1MB size limit |
| `HotkeyService` | Registers/unregisters global Win32 hotkey; parses modifier strings (Ctrl, Alt, Shift, Win) |
| `SettingsService` | Reads/writes `%AppData%\ClipVault\settings.json`; manages Registry auto-start entry |

### ViewModels (`ViewModels/`)

- **`ViewModelBase`** — Implements `INotifyPropertyChanged`; `SetField<T>()` helper.
- **`RelayCommand` / `RelayCommand<T>`** — `ICommand` implementations for XAML binding.
- **`MainViewModel`** — Central orchestrator: search, time/type/favorites filtering, copy/toggle/delete commands, manages the observable clips collection.
- **`ClipItemViewModel`** — Wraps `ClipItem`; adds `IsExpanded` UI state.
- **`SettingsViewModel`** — Manages settings dialog state and applies changes via `SettingsService`.

### Views (`Views/`)

- **`MainWindow.xaml`** — Primary UI: search bar, filter dropdowns (time/type), virtualized item list. Each card shows pin icon, preview text, favorite star, relative time, expand button. Expanded view shows full content + action buttons.
- **`SettingsWindow.xaml`** — Settings dialog: hotkey, auto-start, max records, database path.
- **`TrayIcon.cs`** — System tray: left-click toggles main window; right-click menu for open/theme/settings/pause/exit.

### Data Flow

```
Windows Clipboard API
        │
        ▼
ClipboardService ──→ DatabaseService (SQLite: %AppData%\ClipVault\clipvault.db)
                              │
                              ▼
                       MainViewModel ──→ MainWindow.xaml (WPF binding)
                              │
                      SettingsViewModel ──→ SettingsWindow.xaml

HotkeyService ──→ toggles MainWindow visibility
TrayIcon ──→ toggles MainWindow visibility / opens SettingsWindow
```

### Data Storage

- **Database:** `%AppData%\ClipVault\clipvault.db` (SQLite, WAL mode)
- **Settings:** `%AppData%\ClipVault\settings.json`
- **Auto-start:** Windows Registry `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`

## Key Shortcuts (Runtime)

| Shortcut | Action |
|----------|--------|
| `Ctrl+Shift+V` | Toggle main window (configurable) |
| `Esc` | Clear search / close window |
| Click card text | Copy content and close window |
| Click `⋯` | Expand item detail |

## NuGet Dependencies

| Package | Purpose |
|---------|---------|
| `Hardcodet.NotifyIcon.Wpf` | System tray icon |
| `Microsoft.Data.Sqlite` | SQLite access |
| `ModernWpfUI` | Modern Windows UI theme |
