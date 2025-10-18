@echo off
REM Installation script for Python Game Auto-Clicker
REM Run this script to install all dependencies

echo ========================================
echo Python Game Auto-Clicker Installation
echo ========================================
echo.

REM Check if Python is installed
python --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: Python is not installed or not in PATH
    echo Please install Python 3.8 or higher from https://www.python.org/
    pause
    exit /b 1
)

echo Python found:
python --version
echo.

REM Check if pip is available
pip --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: pip is not installed
    echo Please reinstall Python with pip enabled
    pause
    exit /b 1
)

echo Installing dependencies...
echo.

REM Install dependencies
pip install -r requirements.txt

if errorlevel 1 (
    echo.
    echo ERROR: Failed to install dependencies
    pause
    exit /b 1
)

echo.
echo ========================================
echo Installation completed successfully!
echo ========================================
echo.
echo You can now run the auto-clicker:
echo.
echo   Command Line:
echo   python auto_clicker.py --config config_example.json --process YourGame
echo.
echo   GUI:
echo   python auto_clicker_gui.py
echo.
pause
