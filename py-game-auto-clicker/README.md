# Python Game Auto-Clicker with Image Recognition

A high-performance Python game auto-clicker that uses image recognition to detect UI elements and execute automated clicking operations. This tool is designed for automating repetitive tasks in games and applications while maintaining optimal performance.

## Features

### Core Functionality
- **Process & Window Management**: Automatically find and activate target application windows by process name
- **High-Performance Screen Capture**: Multiple capture methods (Win32 BitBlt, MSS) for optimal performance
- **Image Recognition**: OpenCV template matching with configurable thresholds
- **Automated Clicking**: Precise mouse control with configurable offsets
- **Configuration-Based**: JSON/YAML configuration files for flexible task definitions
- **Multi-Threading**: Non-blocking operation to maintain system responsiveness
- **Comprehensive Logging**: Detailed logging for debugging and monitoring

### Advanced Features
- **Icon Groups**: Match multiple icons sequentially before executing actions
- **Action Sequences**: Chain multiple actions (move, click, delay) together
- **Priority Ordering**: Execute tasks in configured order
- **DPI Awareness**: Works with different screen resolutions and DPI scaling
- **Template Caching**: Cache loaded templates for improved performance

## Installation

### Prerequisites
- Python 3.8 or higher
- Windows OS (uses Win32 APIs)
- Administrator privileges may be required for some games

### Install Dependencies

```bash
pip install -r requirements.txt
```

### Dependencies
- `opencv-python` - Image recognition and template matching
- `numpy` - Array operations for image processing
- `pywin32` - Windows API access for window and mouse control
- `mss` - High-performance screen capture
- `PyYAML` - YAML configuration file support

## Usage

### Basic Usage

```bash
python auto_clicker.py --config config_example.json --process YourGameProcess
```

### Command Line Arguments

- `--config`, `-c`: Path to configuration file (required)
- `--process`, `-p`: Target process name (required)
- `--capture`: Screen capture method (`win32` or `mss`, default: `win32`)
- `--duration`, `-d`: Auto-stop after specified seconds (0 = indefinite, default: 0)

### Examples

```bash
# Run indefinitely with default settings
python auto_clicker.py -c config.json -p MyGame

# Run for 60 seconds using mss capture
python auto_clicker.py -c config.json -p MyGame --capture mss --duration 60

# Stop with Ctrl+C
```

## Configuration

### Configuration File Structure

The configuration file defines processes, tasks, icon matching, and actions. Both JSON and YAML formats are supported.

#### JSON Example

```json
{
  "ProcessList": [
    {
      "ProcessName": "YourGameProcess",
      "ResourcePath": "resources",
      "MatchValue": 0.85,
      "Tasks": [
        {
          "IconGroups": [
            ["button1.png"]
          ],
          "TargetIndex": 0,
          "Actions": [
            {
              "Type": "move",
              "Offset": {"X": 10, "Y": 10}
            },
            {
              "Type": "delay",
              "Delay": 50
            },
            {
              "Type": "click",
              "Offset": {"X": 10, "Y": 10},
              "Button": "left"
            }
          ],
          "Delay": 100
        }
      ]
    }
  ]
}
```

### Configuration Parameters

#### Process Configuration
- `ProcessName` (string, required): Name of the target process (with or without `.exe`)
- `ResourcePath` (string, required): Path to directory containing template images (relative to config file)
- `MatchValue` (float, required): Template matching threshold (0.0-1.0, recommended: 0.8-0.9)
- `Tasks` (array, required): List of task configurations

#### Task Configuration
- `IconGroups` (array, required): Groups of icons to match
  - Each group is an array of icon filenames
  - All icons in a group must match for the task to execute
  - Icons are matched sequentially
- `TargetIndex` (integer, required): Index of the icon to use for action positioning (0-based)
- `Actions` (array, required): Sequence of actions to execute when icons match
- `Delay` (integer, optional): Delay in milliseconds after task execution (default: 0)

#### Action Types

##### Move Action
Moves the mouse cursor to a position relative to the matched icon.

```json
{
  "Type": "move",
  "Offset": {
    "X": 10,
    "Y": 10
  }
}
```

##### Click Action
Clicks the mouse at a position relative to the matched icon.

```json
{
  "Type": "click",
  "Offset": {
    "X": 10,
    "Y": 10
  },
  "Button": "left"
}
```

Supported buttons: `left`, `right`, `middle`

##### Delay Action
Waits for a specified time before the next action.

```json
{
  "Type": "delay",
  "Delay": 200
}
```

Delay is in milliseconds.

### Icon Groups Behavior

Icon groups allow matching multiple icons before executing actions:

1. **Single Icon Group**: `[["icon1.png"]]`
   - Matches one icon and executes actions

2. **Multiple Icons in Group**: `[["icon1.png", "icon2.png", "icon3.png"]]`
   - All icons must be present on screen simultaneously
   - Icons are matched in order
   - If any icon fails to match, the task is skipped
   - Use `TargetIndex` to specify which icon's position to use for actions

3. **Multiple Groups**: `[["icon1.png"], ["icon2.png"]]`
   - Groups are checked in order
   - First matching group triggers action execution

## Performance Optimization

### Screen Capture
- **Win32 Method**: Uses BitBlt for fast window capture (recommended)
- **MSS Method**: Alternative capture method, may be faster in some cases
- Captures only the target window, not the entire screen

### Image Matching
- **Grayscale Conversion**: Images are converted to grayscale for faster matching
- **Template Caching**: Templates are loaded once and cached in memory
- **Optimized Algorithm**: Uses OpenCV's `TM_CCOEFF_NORMED` method for best accuracy

### CPU Usage
- Configurable delays between tasks and actions
- Small delay between task cycles (10ms) to prevent excessive CPU usage
- Multi-threading prevents UI blocking

### Tips for Best Performance
1. Use smallest possible template images
2. Set appropriate `MatchValue` threshold (0.85-0.9 recommended)
3. Add delays between actions to match game response times
4. Use specific, unique icons for better matching
5. Test with Win32 capture first, try MSS if performance issues occur

## Creating Template Images

1. **Capture Screenshots**: Use screenshot tool to capture the target game window
2. **Crop Icons**: Crop the specific UI elements you want to detect
3. **Save as PNG**: Save templates as PNG files in the resource directory
4. **Naming**: Use descriptive names (e.g., `start_button.png`, `confirm_dialog.png`)
5. **Size**: Keep templates small (20x20 to 100x100 pixels typically)
6. **Quality**: Ensure templates are clear and not blurry

## Troubleshooting

### Process Not Found
- Ensure the game/application is running
- Check the process name in Task Manager
- Try with or without `.exe` extension

### Icons Not Matching
- Lower the `MatchValue` threshold (try 0.7-0.8)
- Ensure template images are from the same resolution
- Check if game uses DPI scaling
- Verify template images are clear and not corrupted

### Clicks Not Working
- Run the script as Administrator
- Ensure the game window is not minimized
- Check if the game blocks simulated input
- Verify offset values are correct

### Performance Issues
- Try different capture methods (`--capture mss`)
- Increase delays between tasks
- Reduce number of tasks or icon checks
- Close other applications to free resources

## Safety & Ethics

### Important Notes
- This tool is for personal automation of legitimate tasks
- Always respect game terms of service and end-user license agreements
- Some games may prohibit automation tools and could result in account bans
- Use responsibly and at your own risk

### Recommendations
- Test in single-player or practice modes first
- Monitor the script's behavior
- Have a way to quickly stop the script (Ctrl+C)
- Don't use in competitive multiplayer games

## Project Structure

```
py-game-auto-clicker/
├── auto_clicker.py          # Main application entry point
├── window_manager.py        # Window and process management
├── screen_capture.py        # Screen capture functionality
├── image_matcher.py         # Image recognition and template matching
├── mouse_controller.py      # Mouse control operations
├── action_handler.py        # Action execution logic
├── config_loader.py         # Configuration file loading
├── requirements.txt         # Python dependencies
├── config_example.json      # JSON configuration example
├── config_example.yaml      # YAML configuration example
└── README.md               # This file
```

## Module Documentation

### window_manager.py
Handles process finding and window management using Win32 APIs.

**Key Classes:**
- `WindowManager`: Find processes, get window handles, activate windows

### screen_capture.py
High-performance screen capture using multiple methods.

**Key Classes:**
- `ScreenCapture`: Capture window content using Win32 or MSS

### image_matcher.py
Template matching using OpenCV.

**Key Classes:**
- `ImageMatcher`: Load templates, perform matching, cache results
- `MatchResult`: Data class for match results

### mouse_controller.py
Mouse control using Win32 APIs.

**Key Classes:**
- `MouseController`: Move cursor, perform clicks

### action_handler.py
Execute action sequences.

**Key Classes:**
- `ActionHandler`: Execute move, click, delay actions

### config_loader.py
Configuration file loading and validation.

**Key Classes:**
- `ConfigLoader`: Load JSON/YAML, validate structure

## License

This project is provided as-is for educational and personal use.

## Contributing

Contributions are welcome! Please ensure code follows the existing style and includes appropriate documentation.

## Changelog

### Version 1.0.0
- Initial release
- Core functionality: process management, screen capture, image matching, mouse control
- Support for JSON and YAML configuration files
- Multi-threading support
- Comprehensive logging
- Two screen capture methods (Win32 and MSS)

## Support

For issues, questions, or suggestions, please create an issue in the project repository.
