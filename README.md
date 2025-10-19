# Kenshi Mod Manager

Modern mod manager for Kenshi with playset management system. Inspired by Paradox Launcher.

<img width="1578" height="886" alt="image" src="https://github.com/user-attachments/assets/0f01733c-4c87-4099-b270-2a29233a7627" />


## Features

- **Playset Management** - Create and switch between different mod configurations
- **Drag & Drop Reordering** - Easily arrange mod load order
- **Steam Workshop Support** - Automatic detection of Steam Workshop mods
- **Local Mod Support** - Manage mods from any location
- **Auto-Detection** - Automatically finds Kenshi and Steam installation paths
- **Dark Theme UI** - Modern, clean interface
- **Auto-Updates** - Automatic update notifications via GitHub Releases

## Installation

### For New Users

Download and run `KenshiModManager_setup_v1.0.0.exe` from [Releases](https://github.com/nonniks/KenshiModManager/releases).

### For Updates

The application will automatically check for updates on startup. Alternatively, download `KMM_portable_v1.0.0.zip` and extract to your installation folder.

## Usage

1. Launch Kenshi Mod Manager
2. Create a new playset or select existing one
3. Add mods using the "Add Mods" button
4. Drag mods to reorder load priority
5. Toggle mods on/off with checkboxes
6. Click "Save & Launch" to apply changes and start Kenshi

## Build from Source

### Prerequisites

- .NET 9.0 SDK or later
- Windows OS

### Build Steps

```bash
git clone https://github.com/nonniks/KenshiModManager.git
cd KenshiModManager
dotnet restore
dotnet build -c Release
```

### Create Self-Contained Release

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:IncludeNativeLibrariesForSelfExtract=true
```

Output will be in `bin\Release\net9.0-windows\win-x64\publish\`

## Dependencies

- [KenshiLib](https://github.com/nonniks/KenshiLib) - Core library for Kenshi mod management
- [AutoUpdater.NET](https://github.com/ravibpatel/AutoUpdater.NET) - Automatic update functionality
- [Ookii.Dialogs.Wpf](https://github.com/ookii-dialogs/ookii-dialogs-wpf) - Native Windows dialogs

## License

MIT License - See LICENSE file for details

## Support

For issues and feature requests, please use the [GitHub Issues](https://github.com/nonniks/KenshiModManager/issues) page.
