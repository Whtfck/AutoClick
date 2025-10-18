#!/bin/bash
# Installation script for Python Game Auto-Clicker
# Run this script to install all dependencies

echo "========================================"
echo "Python Game Auto-Clicker Installation"
echo "========================================"
echo ""

# Check if Python is installed
if ! command -v python3 &> /dev/null; then
    echo "ERROR: Python 3 is not installed"
    echo "Please install Python 3.8 or higher"
    exit 1
fi

echo "Python found:"
python3 --version
echo ""

# Check if pip is available
if ! command -v pip3 &> /dev/null; then
    echo "ERROR: pip3 is not installed"
    echo "Please install pip3"
    exit 1
fi

echo "Installing dependencies..."
echo ""

# Install dependencies
pip3 install -r requirements.txt

if [ $? -ne 0 ]; then
    echo ""
    echo "ERROR: Failed to install dependencies"
    exit 1
fi

echo ""
echo "========================================"
echo "Installation completed successfully!"
echo "========================================"
echo ""
echo "You can now run the auto-clicker:"
echo ""
echo "  Command Line:"
echo "  python3 auto_clicker.py --config config_example.json --process YourGame"
echo ""
echo "  GUI:"
echo "  python3 auto_clicker_gui.py"
echo ""
