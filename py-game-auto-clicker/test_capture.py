"""
Test Script for Screen Capture
Simple test to verify screen capture functionality
"""
import sys
import cv2
import logging
from window_manager import WindowManager
from screen_capture import ScreenCapture

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


def test_capture(process_name: str, save_screenshot: bool = True):
    """
    Test screen capture for a process
    
    Args:
        process_name: Name of the process to capture
        save_screenshot: Whether to save screenshot to file
    """
    # Initialize managers
    window_manager = WindowManager()
    screen_capture = ScreenCapture(method="win32")
    
    # Find and activate window
    logger.info(f"Finding process: {process_name}")
    hwnd = window_manager.get_window_by_process_name(process_name)
    
    if not hwnd:
        logger.error(f"Process '{process_name}' not found")
        return False
    
    # Activate window
    window_manager.restore_window(hwnd)
    window_manager.set_foreground(hwnd)
    
    # Capture screenshot
    logger.info("Capturing screenshot...")
    screenshot = screen_capture.capture_window(hwnd)
    
    if screenshot is None:
        logger.error("Failed to capture screenshot")
        return False
    
    logger.info(f"Screenshot captured: {screenshot.shape}")
    
    # Save screenshot
    if save_screenshot:
        output_file = f"screenshot_{process_name}.png"
        cv2.imwrite(output_file, screenshot)
        logger.info(f"Screenshot saved to: {output_file}")
    
    # Display screenshot (optional)
    # cv2.imshow("Screenshot", screenshot)
    # cv2.waitKey(0)
    # cv2.destroyAllWindows()
    
    return True


def main():
    """Main entry point"""
    if len(sys.argv) < 2:
        print("Usage: python test_capture.py <process_name>")
        print("Example: python test_capture.py notepad")
        sys.exit(1)
    
    process_name = sys.argv[1]
    success = test_capture(process_name)
    
    if success:
        logger.info("Test completed successfully")
    else:
        logger.error("Test failed")
        sys.exit(1)


if __name__ == '__main__':
    main()
