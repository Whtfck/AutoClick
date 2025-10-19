"""
Auto Clicker Main Module
High-performance Python game auto-clicker with image recognition
"""
import os
import sys
import time
import logging
import threading
from pathlib import Path
from typing import Optional, Dict, Any

from window_manager import WindowManager
from screen_capture import ScreenCapture
from image_matcher import ImageMatcher, MatchResult
from mouse_controller import MouseController
from action_handler import ActionHandler
from config_loader import ConfigLoader

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[
        logging.StreamHandler(),
        logging.FileHandler('auto_clicker.log', encoding='utf-8')
    ]
)

logger = logging.getLogger(__name__)


class AutoClicker:
    """Main auto-clicker application"""
    
    def __init__(self, config_path: str, capture_method: str = "win32"):
        """
        Initialize auto-clicker
        
        Args:
            config_path: Path to configuration file
            capture_method: Screen capture method ('win32' or 'mss')
        """
        self.config_path = config_path
        self.config_dir = os.path.dirname(os.path.abspath(config_path))
        self.config: Optional[Dict[str, Any]] = None
        self.process_config: Optional[Dict[str, Any]] = None
        
        self.window_manager = WindowManager()
        self.screen_capture = ScreenCapture(method=capture_method)
        self.mouse_controller = MouseController()
        self.action_handler = ActionHandler(self.mouse_controller)
        self.image_matcher: Optional[ImageMatcher] = None
        
        self.is_running = False
        self.worker_thread: Optional[threading.Thread] = None
        
        logger.info(f"AutoClicker initialized with config: {config_path}")
    
    def load_config(self, process_name: str) -> bool:
        """
        Load configuration for specific process
        
        Args:
            process_name: Name of the target process
            
        Returns:
            True if successful, False otherwise
        """
        # Load full config
        self.config = ConfigLoader.load(self.config_path)
        if not self.config:
            logger.error("Failed to load configuration")
            return False
        
        # Validate config
        if not ConfigLoader.validate_config(self.config):
            logger.error("Configuration validation failed")
            return False
        
        # Get process-specific config
        self.process_config = ConfigLoader.get_process_config(self.config, process_name)
        if not self.process_config:
            logger.error(f"No configuration found for process: {process_name}")
            return False
        
        # Initialize image matcher with threshold
        match_value = self.process_config.get('MatchValue', 0.8)
        self.image_matcher = ImageMatcher(threshold=match_value)
        logger.info(f"Image matcher initialized with threshold: {match_value}")
        
        return True
    
    def activate_target_window(self, process_name: str) -> bool:
        """
        Find and activate target window
        
        Args:
            process_name: Name of the target process
            
        Returns:
            True if successful, False otherwise
        """
        logger.info(f"Activating window for process: {process_name}")
        return self.window_manager.activate_window(process_name)
    
    def process_icon_group(self,
                          screenshot: Any,
                          icon_group: list,
                          resource_path: str) -> Optional[MatchResult]:
        """
        Process a group of icons and check if all match
        
        Args:
            screenshot: Screenshot image
            icon_group: List of icon filenames
            resource_path: Path to resource directory
            
        Returns:
            MatchResult if all icons matched, None otherwise
        """
        target_result = None
        
        for idx, icon_file in enumerate(icon_group):
            icon_path = os.path.join(self.config_dir, resource_path, icon_file)
            
            # Match template
            match_result = self.image_matcher.match_template_from_file(screenshot, icon_path)
            
            if match_result.matched:
                logger.info(f"Matched: {icon_file}, confidence: {match_result.confidence:.3f}")
                target_result = match_result
                
                # If this is the last icon in the group, all matched
                if idx == len(icon_group) - 1:
                    return target_result
            else:
                logger.debug(f"Not matched: {icon_file}, confidence: {match_result.confidence:.3f}")
                return None
        
        return None
    
    def process_task(self, task: Dict[str, Any], resource_path: str) -> bool:
        """
        Process a single task
        
        Args:
            task: Task configuration
            resource_path: Path to resource directory
            
        Returns:
            True if task executed, False otherwise
        """
        hwnd = self.window_manager.hwnd
        if not hwnd:
            logger.error("No window handle available")
            return False
        
        # Capture screenshot
        screenshot = self.screen_capture.capture_window(hwnd)
        if screenshot is None:
            logger.error("Failed to capture screenshot")
            return False
        
        # Get window rect for absolute positioning
        window_rect = self.window_manager.get_window_rect(hwnd)
        if not window_rect:
            logger.error("Failed to get window rect")
            return False
        
        # Process icon groups
        icon_groups = task.get('IconGroups', [])
        target_index = task.get('TargetIndex', 0)
        actions = task.get('Actions', [])
        
        for icon_group in icon_groups:
            if not self.is_running:
                return False
            
            # Check if all icons in group match
            target_result = self.process_icon_group(screenshot, icon_group, resource_path)
            
            if target_result:
                logger.info(f"All icons matched in group: {icon_group}")
                # Execute actions
                self.action_handler.execute_actions(actions, target_result, window_rect)
                return True
            else:
                logger.debug(f"Icon group not fully matched: {icon_group}")
        
        return False
    
    def run_tasks(self):
        """Main task execution loop"""
        try:
            process_name = self.process_config.get('ProcessName')
            resource_path = self.process_config.get('ResourcePath', 'resources')
            tasks = self.process_config.get('Tasks', [])
            
            logger.info(f"Starting task loop for process: {process_name}")
            logger.info(f"Total tasks: {len(tasks)}")
            
            while self.is_running:
                for task in tasks:
                    if not self.is_running:
                        break
                    
                    # Process task
                    self.process_task(task, resource_path)
                    
                    # Task delay
                    task_delay = task.get('Delay', 0)
                    if task_delay > 0:
                        time.sleep(task_delay / 1000.0)
                
                # Small delay between task cycles to prevent excessive CPU usage
                time.sleep(0.01)
                
        except Exception as e:
            logger.error(f"Error in task loop: {e}", exc_info=True)
        finally:
            self.is_running = False
            logger.info("Task loop stopped")
    
    def start(self, process_name: str) -> bool:
        """
        Start auto-clicker for target process
        
        Args:
            process_name: Name of the target process
            
        Returns:
            True if started successfully, False otherwise
        """
        if self.is_running:
            logger.warning("Auto-clicker is already running")
            return False
        
        # Load configuration
        if not self.load_config(process_name):
            return False
        
        # Activate target window
        if not self.activate_target_window(process_name):
            logger.error("Failed to activate target window")
            return False
        
        # Small delay to ensure window is ready
        time.sleep(0.5)
        
        # Start task loop in separate thread
        self.is_running = True
        self.worker_thread = threading.Thread(target=self.run_tasks, daemon=True)
        self.worker_thread.start()
        
        logger.info("Auto-clicker started")
        return True
    
    def stop(self):
        """Stop auto-clicker"""
        if not self.is_running:
            logger.warning("Auto-clicker is not running")
            return
        
        logger.info("Stopping auto-clicker...")
        self.is_running = False
        
        if self.worker_thread:
            self.worker_thread.join(timeout=5.0)
        
        logger.info("Auto-clicker stopped")
    
    def is_active(self) -> bool:
        """Check if auto-clicker is running"""
        return self.is_running


def main():
    """Main entry point"""
    import argparse
    
    parser = argparse.ArgumentParser(description='Game Auto-Clicker with Image Recognition')
    parser.add_argument('--config', '-c', required=True, help='Path to configuration file')
    parser.add_argument('--process', '-p', required=True, help='Target process name')
    parser.add_argument('--capture', choices=['win32', 'mss'], default='win32',
                       help='Screen capture method (default: win32)')
    parser.add_argument('--duration', '-d', type=int, default=0,
                       help='Auto-stop after duration in seconds (0 = run indefinitely)')
    
    args = parser.parse_args()
    
    # Check if config file exists
    if not os.path.exists(args.config):
        logger.error(f"Config file not found: {args.config}")
        sys.exit(1)
    
    # Create auto-clicker
    clicker = AutoClicker(args.config, capture_method=args.capture)
    
    # Start auto-clicker
    if not clicker.start(args.process):
        logger.error("Failed to start auto-clicker")
        sys.exit(1)
    
    try:
        # Run for specified duration or indefinitely
        if args.duration > 0:
            logger.info(f"Running for {args.duration} seconds...")
            time.sleep(args.duration)
            clicker.stop()
        else:
            logger.info("Running indefinitely. Press Ctrl+C to stop.")
            while clicker.is_active():
                time.sleep(1)
    except KeyboardInterrupt:
        logger.info("Keyboard interrupt received")
        clicker.stop()
    
    logger.info("Program terminated")


if __name__ == '__main__':
    main()
