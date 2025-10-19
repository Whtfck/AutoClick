# Quick Start Guide

Get started with Python Game Auto-Clicker in 5 minutes!

## Installation

### Step 1: Install Python
Make sure you have Python 3.8 or higher installed. Download from [python.org](https://www.python.org/).

### Step 2: Install Dependencies
```bash
pip install -r requirements.txt
```

### Step 3: Verify Installation
```bash
python test_capture.py notepad
```

This should capture a screenshot of Notepad if it's running.

## Basic Usage

### Method 1: Command Line

```bash
python auto_clicker.py --config config_example.json --process YourGameProcess
```

### Method 2: GUI

```bash
python auto_clicker_gui.py
```

Then:
1. Click "Browse" to select your config file
2. Enter the process name
3. Click "Start"

## Create Your First Configuration

### Step 1: Create Template Images

1. Open your game/application
2. Take screenshots of the UI elements you want to click
3. Crop to just the icon/button (save as PNG)
4. Place in `resources/` folder

Example:
```
resources/
└── mygame/
    ├── start_button.png
    └── confirm.png
```

### Step 2: Create Config File

Create `my_config.json`:

```json
{
  "ProcessList": [
    {
      "ProcessName": "MyGame",
      "ResourcePath": "resources/mygame",
      "MatchValue": 0.85,
      "Tasks": [
        {
          "IconGroups": [
            ["start_button.png"]
          ],
          "TargetIndex": 0,
          "Actions": [
            {
              "Type": "click",
              "Offset": {
                "X": 10,
                "Y": 10
              }
            },
            {
              "Type": "delay",
              "Delay": 500
            }
          ],
          "Delay": 1000
        }
      ]
    }
  ]
}
```

### Step 3: Run

```bash
python auto_clicker.py --config my_config.json --process MyGame
```

## Configuration Explained

### Process Configuration
- **ProcessName**: Name of your game (check Task Manager)
- **ResourcePath**: Folder containing template images
- **MatchValue**: How precisely to match (0.8-0.9 recommended)

### Task Configuration
- **IconGroups**: List of images to detect
- **TargetIndex**: Which icon's position to use (0-based)
- **Actions**: What to do when icons are found
- **Delay**: Wait time after task (milliseconds)

### Action Types

**Move**: Move mouse to icon position
```json
{
  "Type": "move",
  "Offset": {"X": 10, "Y": 10}
}
```

**Click**: Click at icon position
```json
{
  "Type": "click",
  "Offset": {"X": 10, "Y": 10},
  "Button": "left"
}
```

**Delay**: Wait before next action
```json
{
  "Type": "delay",
  "Delay": 200
}
```

## Testing Tips

### 1. Test Screenshot Capture
```bash
python test_capture.py MyGame
```
Check if `screenshot_MyGame.png` is created successfully.

### 2. Test Template Matching

Create a simple test script:
```python
import cv2
from image_matcher import ImageMatcher

matcher = ImageMatcher(threshold=0.8)
screenshot = cv2.imread("screenshot_MyGame.png")
result = matcher.match_template_from_file(screenshot, "resources/mygame/button.png")

print(f"Matched: {result.matched}")
print(f"Confidence: {result.confidence}")
print(f"Location: {result.location}")
```

### 3. Adjust Threshold

If icons aren't matching:
- Lower `MatchValue` (try 0.7-0.8)
- Ensure template is from same resolution
- Check if game uses anti-aliasing

### 4. Monitor Logs

Check `auto_clicker.log` for detailed information about what's happening.

## Common Issues

### "Process not found"
- Check process name in Task Manager
- Try with or without `.exe`
- Make sure game is running

### "Icons not matching"
- Lower `MatchValue`
- Retake template screenshots
- Check resolution matches

### "Clicks not working"
- Run as Administrator
- Check if game blocks simulated input
- Verify offset values

## Next Steps

1. Read the full [README.md](README.md) for detailed documentation
2. Check [config_example.json](config_example.json) for more examples
3. Experiment with different tasks and actions
4. Fine-tune delays and thresholds for your game

## Safety Reminder

⚠️ **Always respect game terms of service!**

Many games prohibit automation tools. Use this responsibly for:
- Single-player games
- Personal automation tasks
- Testing and development

Not for:
- Competitive multiplayer
- Games that explicitly prohibit automation
- Gaining unfair advantages

## Support

If you encounter issues:
1. Check the logs (`auto_clicker.log`)
2. Review the configuration format
3. Test with the example config first
4. Create an issue with detailed information

Happy automating! 🚀
