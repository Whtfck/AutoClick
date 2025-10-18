"""
Action Handler Module
Execute action sequences (move, click, delay)
"""
import time
import logging
from typing import List, Dict, Any, Tuple
from mouse_controller import MouseController
from image_matcher import MatchResult

logger = logging.getLogger(__name__)


class ActionHandler:
    """Handle execution of action sequences"""
    
    def __init__(self, mouse_controller: MouseController):
        """
        Initialize action handler
        
        Args:
            mouse_controller: MouseController instance
        """
        self.mouse = mouse_controller
    
    def calculate_absolute_position(self,
                                   match_result: MatchResult,
                                   window_rect: Tuple[int, int, int, int],
                                   offset: Dict[str, int]) -> Tuple[int, int]:
        """
        Calculate absolute screen position from match result and offset
        
        Args:
            match_result: Template match result
            window_rect: Window rectangle (left, top, right, bottom)
            offset: Offset dictionary with 'X' and 'Y' keys
            
        Returns:
            Tuple of (x, y) absolute screen coordinates
        """
        if not match_result.location:
            return (0, 0)
        
        # Get match location (relative to window)
        match_x, match_y = match_result.location
        
        # Get offset
        offset_x = offset.get('X', 0)
        offset_y = offset.get('Y', 0)
        
        # Calculate absolute position
        window_left, window_top, _, _ = window_rect
        abs_x = window_left + match_x + offset_x
        abs_y = window_top + match_y + offset_y
        
        logger.debug(f"Calculated position: match=({match_x}, {match_y}), "
                    f"offset=({offset_x}, {offset_y}), "
                    f"window=({window_left}, {window_top}), "
                    f"absolute=({abs_x}, {abs_y})")
        
        return (abs_x, abs_y)
    
    def execute_action(self,
                      action: Dict[str, Any],
                      match_result: MatchResult,
                      window_rect: Tuple[int, int, int, int]) -> bool:
        """
        Execute a single action
        
        Args:
            action: Action dictionary with 'Type' and other parameters
            match_result: Template match result for position reference
            window_rect: Window rectangle
            
        Returns:
            True if successful, False otherwise
        """
        action_type = action.get('Type', '').lower()
        
        try:
            if action_type == 'move':
                offset = action.get('Offset', {'X': 0, 'Y': 0})
                x, y = self.calculate_absolute_position(match_result, window_rect, offset)
                self.mouse.move(x, y)
                logger.info(f"Executed move to ({x}, {y})")
                return True
            
            elif action_type == 'click':
                offset = action.get('Offset', {'X': 0, 'Y': 0})
                x, y = self.calculate_absolute_position(match_result, window_rect, offset)
                button = action.get('Button', 'left').lower()
                self.mouse.click(x, y, button)
                logger.info(f"Executed {button} click at ({x}, {y})")
                return True
            
            elif action_type == 'delay':
                delay_ms = action.get('Delay', 0)
                delay_sec = delay_ms / 1000.0
                time.sleep(delay_sec)
                logger.debug(f"Executed delay: {delay_ms}ms")
                return True
            
            else:
                logger.warning(f"Unknown action type: {action_type}")
                return False
                
        except Exception as e:
            logger.error(f"Error executing action {action_type}: {e}")
            return False
    
    def execute_actions(self,
                       actions: List[Dict[str, Any]],
                       match_result: MatchResult,
                       window_rect: Tuple[int, int, int, int]) -> bool:
        """
        Execute a sequence of actions
        
        Args:
            actions: List of action dictionaries
            match_result: Template match result for position reference
            window_rect: Window rectangle
            
        Returns:
            True if all actions executed successfully, False otherwise
        """
        for action in actions:
            if not self.execute_action(action, match_result, window_rect):
                logger.error(f"Failed to execute action: {action}")
                return False
        
        logger.info(f"Executed {len(actions)} actions successfully")
        return True
