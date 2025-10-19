# AutoClick - Game Automation Suite

A comprehensive game automation suite with two implementations: a .NET WPF desktop application and a Python command-line/GUI tool.

## Overview

This repository contains tools for automating repetitive clicking tasks in games and applications using image recognition technology.

## Implementations

### 1. AutoClick (.NET WPF)

A Windows desktop application built with .NET 8 and WPF.

**Location**: `AutoClick/`

**Features**:
- WPF-based GUI
- Emgu CV (OpenCV for .NET) for image recognition
- Process selection dialog
- Real-time configuration
- Visual feedback

**Requirements**:
- .NET 8 SDK
- Windows OS
- Visual Studio (recommended)

**Usage**:
```bash
cd AutoClick
dotnet run
```

See [AutoClick documentation](AutoClick/README.md) for more details.

### 2. Python Game Auto-Clicker

A high-performance Python implementation with both CLI and GUI interfaces.

**Location**: `py-game-auto-clicker/`

**Features**:
- Command-line interface
- Optional Tkinter GUI
- OpenCV for image recognition
- JSON/YAML configuration
- Multi-threading support
- Comprehensive logging
- Multiple screen capture methods

**Requirements**:
- Python 3.8+
- Windows OS
- See `py-game-auto-clicker/requirements.txt`

**Quick Start**:
```bash
cd py-game-auto-clicker
pip install -r requirements.txt
python auto_clicker.py --config config_example.json --process YourGame
```

See [Python Auto-Clicker documentation](py-game-auto-clicker/README.md) for more details.

## Core Functionality

Both implementations provide:

1. **Process & Window Management**
   - Find processes by name
   - Activate and restore windows
   - Window coordinate tracking

2. **High-Performance Screen Capture**
   - Win32 BitBlt API
   - Optimized for game windows
   - Minimal performance impact

3. **Image Recognition**
   - OpenCV template matching
   - Configurable matching thresholds
   - Template caching for performance

4. **Automated Actions**
   - Mouse movement
   - Click operations (left, right, middle)
   - Configurable delays
   - Action sequences

5. **Configuration System**
   - JSON-based configuration
   - Icon groups for complex matching
   - Task prioritization
   - Per-action offsets

## Configuration Format

Both implementations use a similar JSON configuration format:

```json
{
  "ProcessList": [
    {
      "ProcessName": "GameProcess",
      "ResourcePath": "resources",
      "MatchValue": 0.85,
      "Tasks": [
        {
          "IconGroups": [["button.png"]],
          "TargetIndex": 0,
          "Actions": [
            {
              "Type": "click",
              "Offset": {"X": 10, "Y": 10}
            }
          ],
          "Delay": 100
        }
      ]
    }
  ]
}
```

## Use Cases

- Automating repetitive game tasks
- Testing UI interactions
- Game bot development (single-player only)
- Application automation
- UI testing and QA

## Safety & Ethics

⚠️ **Important Notice**

- Always respect game terms of service
- Many games prohibit automation tools
- Use responsibly for personal, non-competitive purposes
- Not intended for online/competitive gaming
- Users are responsible for compliance with game rules

## Performance

Both implementations are optimized for performance:

- **Screen Capture**: Win32 BitBlt for minimal overhead
- **Image Matching**: Grayscale conversion and efficient algorithms
- **Template Caching**: Avoid repeated file I/O
- **Threading**: Non-blocking operation
- **Configurable Delays**: Balance between speed and CPU usage

## Choosing an Implementation

### Choose .NET WPF if:
- You prefer a desktop GUI
- You're familiar with C# and .NET
- You want integrated development with Visual Studio
- You need tight Windows integration

### Choose Python if:
- You prefer command-line tools
- You want easy scripting and customization
- You need JSON/YAML configuration flexibility
- You prefer Python's ecosystem
- You want both CLI and GUI options

## Development

### .NET Development
```bash
cd AutoClick
dotnet restore
dotnet build
```

### Python Development
```bash
cd py-game-auto-clicker
pip install -r requirements.txt
python -m pytest  # Run tests (if available)
```

## Project Structure

```
.
├── AutoClick/                    # .NET WPF implementation
│   ├── MainWindow.xaml          # Main UI
│   ├── model/                   # Action models
│   ├── utils/                   # Utility classes
│   └── resource/                # Configuration and templates
│
├── py-game-auto-clicker/        # Python implementation
│   ├── auto_clicker.py          # Main CLI application
│   ├── auto_clicker_gui.py      # GUI application
│   ├── window_manager.py        # Window operations
│   ├── screen_capture.py        # Screen capture
│   ├── image_matcher.py         # Image recognition
│   ├── mouse_controller.py      # Mouse control
│   ├── action_handler.py        # Action execution
│   ├── config_loader.py         # Configuration loading
│   └── resources/               # Template images
│
└── README.md                    # This file
```

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

MIT License - see [LICENSE](py-game-auto-clicker/LICENSE) for details.

## Acknowledgments

- OpenCV for image recognition capabilities
- Emgu CV for .NET OpenCV bindings
- Win32 API for window and input management

## Disclaimer

This software is provided for educational and personal use only. The authors are not responsible for any misuse or violations of game terms of service. Use at your own risk and always respect the rules of the games and applications you interact with.
