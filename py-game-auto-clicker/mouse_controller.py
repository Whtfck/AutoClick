"""
Mouse Controller Module
Mouse operations using Win32 APIs
"""
import win32api
import win32con
import time
import logging
from typing import Tuple, Optional

logger = logging.getLogger(__name__)


class MouseController:
    """Mouse control using Win32 APIs"""
    
    def __init__(self, click_delay: float = 0.05):
        """
        Initialize mouse controller
        
        Args:
            click_delay: Delay between mouse down and up (seconds)
        """
        self.click_delay = click_delay
    
    def move(self, x: int, y: int) -> bool:
        """
        Move mouse cursor to absolute position
        
        Args:
            x: X coordinate
            y: Y coordinate
            
        Returns:
            True if successful
        """
        try:
            win32api.SetCursorPos((x, y))
            logger.debug(f"Moved mouse to ({x}, {y})")
            return True
        except Exception as e:
            logger.error(f"Failed to move mouse: {e}")
            return False
    
    def click(self, x: Optional[int] = None, y: Optional[int] = None, button: str = "left") -> bool:
        """
        Click at current position or move and click
        
        Args:
            x: X coordinate (optional, uses current position if None)
            y: Y coordinate (optional, uses current position if None)
            button: Mouse button ('left', 'right', 'middle')
            
        Returns:
            True if successful
        """
        try:
            # Move to position if coordinates provided
            if x is not None and y is not None:
                self.move(x, y)
            
            # Determine button codes
            if button == "left":
                down_code = win32con.MOUSEEVENTF_LEFTDOWN
                up_code = win32con.MOUSEEVENTF_LEFTUP
            elif button == "right":
                down_code = win32con.MOUSEEVENTF_RIGHTDOWN
                up_code = win32con.MOUSEEVENTF_RIGHTUP
            elif button == "middle":
                down_code = win32con.MOUSEEVENTF_MIDDLEDOWN
                up_code = win32con.MOUSEEVENTF_MIDDLEUP
            else:
                logger.error(f"Unknown button: {button}")
                return False
            
            # Perform click
            win32api.mouse_event(down_code, 0, 0, 0, 0)
            time.sleep(self.click_delay)
            win32api.mouse_event(up_code, 0, 0, 0, 0)
            
            logger.debug(f"Clicked {button} button at ({x}, {y})")
            return True
            
        except Exception as e:
            logger.error(f"Failed to click: {e}")
            return False
    
    def left_click(self, x: Optional[int] = None, y: Optional[int] = None) -> bool:
        """
        Left click at position
        
        Args:
            x: X coordinate (optional)
            y: Y coordinate (optional)
            
        Returns:
            True if successful
        """
        return self.click(x, y, "left")
    
    def right_click(self, x: Optional[int] = None, y: Optional[int] = None) -> bool:
        """
        Right click at position
        
        Args:
            x: X coordinate (optional)
            y: Y coordinate (optional)
            
        Returns:
            True if successful
        """
        return self.click(x, y, "right")
    
    def double_click(self, x: Optional[int] = None, y: Optional[int] = None, interval: float = 0.1) -> bool:
        """
        Double click at position
        
        Args:
            x: X coordinate (optional)
            y: Y coordinate (optional)
            interval: Delay between clicks (seconds)
            
        Returns:
            True if successful
        """
        try:
            self.left_click(x, y)
            time.sleep(interval)
            self.left_click(x, y)
            return True
        except Exception as e:
            logger.error(f"Failed to double click: {e}")
            return False
    
    def get_position(self) -> Tuple[int, int]:
        """
        Get current mouse cursor position
        
        Returns:
            Tuple of (x, y) coordinates
        """
        try:
            pos = win32api.GetCursorPos()
            return pos
        except Exception as e:
            logger.error(f"Failed to get cursor position: {e}")
            return (0, 0)
