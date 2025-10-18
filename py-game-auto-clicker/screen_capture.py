"""
Screen Capture Module
High-performance screen capture using Win32 APIs (BitBlt) and mss
"""
import win32gui
import win32ui
import win32con
from ctypes import windll
from PIL import Image
import numpy as np
import logging
from typing import Optional, Tuple
import mss
import mss.tools

logger = logging.getLogger(__name__)


class ScreenCapture:
    """High-performance screen capture using multiple methods"""
    
    def __init__(self, method: str = "win32"):
        """
        Initialize screen capture
        
        Args:
            method: Capture method ('win32' or 'mss')
        """
        self.method = method
        self.mss_instance = None
        if method == "mss":
            self.mss_instance = mss.mss()
    
    def capture_window_win32(self, hwnd: int) -> Optional[np.ndarray]:
        """
        Capture window using Win32 BitBlt (similar to .NET implementation)
        
        Args:
            hwnd: Window handle
            
        Returns:
            Image as numpy array (BGR format for OpenCV) or None if failed
        """
        try:
            # Get window dimensions
            left, top, right, bottom = win32gui.GetWindowRect(hwnd)
            width = right - left
            height = bottom - top
            
            # Get device contexts
            hwndDC = win32gui.GetWindowDC(hwnd)
            mfcDC = win32ui.CreateDCFromHandle(hwndDC)
            saveDC = mfcDC.CreateCompatibleDC()
            
            # Create bitmap
            saveBitMap = win32ui.CreateBitmap()
            saveBitMap.CreateCompatibleBitmap(mfcDC, width, height)
            saveDC.SelectObject(saveBitMap)
            
            # Copy screen to bitmap
            result = windll.user32.PrintWindow(hwnd, saveDC.GetSafeHdc(), 3)
            
            # Alternative: BitBlt from desktop DC
            if not result:
                desktop_dc = win32gui.GetDC(0)
                desktop_mfc = win32ui.CreateDCFromHandle(desktop_dc)
                saveDC.BitBlt((0, 0), (width, height), desktop_mfc, (left, top), win32con.SRCCOPY)
                win32gui.ReleaseDC(0, desktop_dc)
            
            # Convert to numpy array
            bmpinfo = saveBitMap.GetInfo()
            bmpstr = saveBitMap.GetBitmapBits(True)
            img = np.frombuffer(bmpstr, dtype=np.uint8)
            img.shape = (height, width, 4)
            
            # Convert BGRA to BGR for OpenCV
            img = img[:, :, :3]
            
            # Cleanup
            win32gui.DeleteObject(saveBitMap.GetHandle())
            saveDC.DeleteDC()
            mfcDC.DeleteDC()
            win32gui.ReleaseDC(hwnd, hwndDC)
            
            logger.debug(f"Captured window {hwnd} using Win32: {width}x{height}")
            return img
            
        except Exception as e:
            logger.error(f"Failed to capture window using Win32: {e}")
            return None
    
    def capture_window_mss(self, hwnd: int) -> Optional[np.ndarray]:
        """
        Capture window using mss library (alternative method)
        
        Args:
            hwnd: Window handle
            
        Returns:
            Image as numpy array (BGR format for OpenCV) or None if failed
        """
        try:
            # Get window dimensions
            left, top, right, bottom = win32gui.GetWindowRect(hwnd)
            
            # Define monitor region
            monitor = {
                "left": left,
                "top": top,
                "width": right - left,
                "height": bottom - top
            }
            
            # Capture screenshot
            sct_img = self.mss_instance.grab(monitor)
            
            # Convert to numpy array (BGRA format)
            img = np.array(sct_img)
            
            # Convert BGRA to BGR for OpenCV
            img = img[:, :, :3]
            
            logger.debug(f"Captured window {hwnd} using mss: {monitor['width']}x{monitor['height']}")
            return img
            
        except Exception as e:
            logger.error(f"Failed to capture window using mss: {e}")
            return None
    
    def capture_window(self, hwnd: int) -> Optional[np.ndarray]:
        """
        Capture window using configured method
        
        Args:
            hwnd: Window handle
            
        Returns:
            Image as numpy array (BGR format for OpenCV) or None if failed
        """
        if self.method == "mss":
            return self.capture_window_mss(hwnd)
        else:
            return self.capture_window_win32(hwnd)
    
    def capture_region(self, hwnd: int, region: Tuple[int, int, int, int]) -> Optional[np.ndarray]:
        """
        Capture specific region of window
        
        Args:
            hwnd: Window handle
            region: Tuple of (x, y, width, height) relative to window
            
        Returns:
            Image as numpy array or None if failed
        """
        img = self.capture_window(hwnd)
        if img is None:
            return None
        
        x, y, w, h = region
        
        # Ensure region is within bounds
        img_h, img_w = img.shape[:2]
        x = max(0, min(x, img_w))
        y = max(0, min(y, img_h))
        w = min(w, img_w - x)
        h = min(h, img_h - y)
        
        return img[y:y+h, x:x+w]
    
    def __del__(self):
        """Cleanup mss instance"""
        if self.mss_instance:
            self.mss_instance.close()
