"""
Window Manager Module
Handles process finding and window management using Win32 APIs
"""
import win32gui
import win32process
import win32con
import psutil
import logging
from typing import Optional, Tuple

logger = logging.getLogger(__name__)


class WindowManager:
    """Manages window operations like finding, activating, and restoring windows"""
    
    def __init__(self):
        self.hwnd: Optional[int] = None
        self.process_name: Optional[str] = None
        
    def find_process_by_name(self, process_name: str) -> Optional[int]:
        """
        Find a running process by name
        
        Args:
            process_name: Name of the process (without .exe extension)
            
        Returns:
            Process ID if found, None otherwise
        """
        process_name_lower = process_name.lower()
        for proc in psutil.process_iter(['pid', 'name']):
            try:
                proc_name = proc.info['name'].lower()
                # Match with or without .exe extension
                if proc_name == process_name_lower or proc_name == f"{process_name_lower}.exe":
                    logger.info(f"Found process: {proc.info['name']} (PID: {proc.info['pid']})")
                    return proc.info['pid']
            except (psutil.NoSuchProcess, psutil.AccessDenied):
                continue
        
        logger.warning(f"Process '{process_name}' not found")
        return None
    
    def get_window_by_process_name(self, process_name: str) -> Optional[int]:
        """
        Get window handle by process name
        
        Args:
            process_name: Name of the process
            
        Returns:
            Window handle (HWND) if found, None otherwise
        """
        self.process_name = process_name
        target_pid = self.find_process_by_name(process_name)
        
        if not target_pid:
            return None
        
        def callback(hwnd, hwnds):
            if win32gui.IsWindowVisible(hwnd):
                _, pid = win32process.GetWindowThreadProcessId(hwnd)
                if pid == target_pid:
                    hwnds.append(hwnd)
            return True
        
        hwnds = []
        win32gui.EnumWindows(callback, hwnds)
        
        if hwnds:
            self.hwnd = hwnds[0]
            logger.info(f"Found window handle: {self.hwnd}")
            return self.hwnd
        
        logger.warning(f"No window found for process '{process_name}'")
        return None
    
    def restore_window(self, hwnd: Optional[int] = None) -> bool:
        """
        Restore a minimized window
        
        Args:
            hwnd: Window handle (uses stored hwnd if not provided)
            
        Returns:
            True if successful, False otherwise
        """
        hwnd = hwnd or self.hwnd
        if not hwnd:
            logger.error("No window handle available")
            return False
        
        try:
            win32gui.ShowWindow(hwnd, win32con.SW_RESTORE)
            logger.debug(f"Window {hwnd} restored")
            return True
        except Exception as e:
            logger.error(f"Failed to restore window: {e}")
            return False
    
    def set_foreground(self, hwnd: Optional[int] = None) -> bool:
        """
        Bring window to foreground
        
        Args:
            hwnd: Window handle (uses stored hwnd if not provided)
            
        Returns:
            True if successful, False otherwise
        """
        hwnd = hwnd or self.hwnd
        if not hwnd:
            logger.error("No window handle available")
            return False
        
        try:
            win32gui.SetForegroundWindow(hwnd)
            logger.debug(f"Window {hwnd} brought to foreground")
            return True
        except Exception as e:
            logger.error(f"Failed to set foreground: {e}")
            return False
    
    def activate_window(self, process_name: str) -> bool:
        """
        Find, restore, and activate a window by process name
        
        Args:
            process_name: Name of the process
            
        Returns:
            True if successful, False otherwise
        """
        hwnd = self.get_window_by_process_name(process_name)
        if not hwnd:
            return False
        
        # Restore if minimized
        self.restore_window(hwnd)
        
        # Bring to foreground
        return self.set_foreground(hwnd)
    
    def get_window_rect(self, hwnd: Optional[int] = None) -> Optional[Tuple[int, int, int, int]]:
        """
        Get window rectangle coordinates
        
        Args:
            hwnd: Window handle (uses stored hwnd if not provided)
            
        Returns:
            Tuple of (left, top, right, bottom) or None if failed
        """
        hwnd = hwnd or self.hwnd
        if not hwnd:
            logger.error("No window handle available")
            return None
        
        try:
            rect = win32gui.GetWindowRect(hwnd)
            logger.debug(f"Window rect: {rect}")
            return rect
        except Exception as e:
            logger.error(f"Failed to get window rect: {e}")
            return None
    
    def is_foreground(self, hwnd: Optional[int] = None) -> bool:
        """
        Check if window is in foreground
        
        Args:
            hwnd: Window handle (uses stored hwnd if not provided)
            
        Returns:
            True if window is in foreground, False otherwise
        """
        hwnd = hwnd or self.hwnd
        if not hwnd:
            return False
        
        return win32gui.GetForegroundWindow() == hwnd
