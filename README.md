# Clipboard History

A lightweight Windows clipboard manager that keeps a history of everything you copy. Access your clipboard history anytime with a single keypress.

![Clipboard History Screenshot](docs/screenshot.png)

## Features

- **Instant Access** - Press `Scroll Lock` to open the clipboard history panel
- **Slide-in Panel** - Modern dark-themed UI that slides in from the right edge of your screen
- **Text & Images** - Stores text, file paths, and screenshots/images
- **Search** - Quickly filter through your clipboard history
- **Persistent Storage** - History is saved between sessions
- **System Tray** - Runs quietly in the background
- **Auto-start** - Optionally starts with Windows

## Installation

### Option 1: Installer (Recommended)

1. Download `ClipboardHistorySetup.exe` from the [latest release](../../releases/latest)
2. Run the installer
3. The app will start automatically and appear in your system tray

### Option 2: Portable

1. Download `ClipboardHistory.exe` from the [latest release](../../releases/latest)
2. Run the executable directly
3. Optionally, add it to your startup folder

## Usage

| Action | Description |
|--------|-------------|
| `Scroll Lock` | Toggle the clipboard history panel |
| `Double-click` / `Enter` | Copy selected item to clipboard |
| `Esc` | Close the panel |
| `Type` | Search/filter history |

The app runs in the system tray. Right-click the tray icon for options.

## Data Storage

Clipboard history is stored locally at:
```
%LocalAppData%\ClipboardHistory\
├── history.json      # Text and metadata
└── images\           # Screenshot/image files
```

## Building from Source

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Windows 10/11

### Build

```bash
# Clone the repository
git clone https://github.com/yourusername/ClipboardHistory.git
cd ClipboardHistory

# Build the main application
cd ClipboardHistory
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Build the installer
cd ../ClipboardHistoryInstaller
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The built files will be in `bin/Release/net10.0-windows/win-x64/publish/`

## Project Structure

```
ClipboardHistory/
├── ClipboardHistory/              # Main application
│   ├── App.xaml                   # WPF application entry
│   ├── MainWindow.xaml            # Slide-in panel UI
│   ├── ClipboardEntry.cs          # Data model
│   ├── ClipboardHistoryManager.cs # History storage
│   ├── ClipboardMonitorWpf.cs     # Clipboard monitoring
│   └── GlobalKeyboardHook.cs      # Scroll Lock hotkey
│
├── ClipboardHistoryInstaller/     # Installer application
│   ├── MainWindow.xaml            # Installer UI
│   └── MainWindow.xaml.cs         # Installation logic
│
└── .github/
    └── workflows/
        └── release.yml            # GitHub Actions CI/CD
```

## License

MIT License - see [LICENSE](LICENSE) for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request
