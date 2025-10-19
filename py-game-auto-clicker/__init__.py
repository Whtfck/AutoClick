"""
Python Game Auto-Clicker
High-performance game automation with image recognition
"""

__version__ = "1.0.0"
__author__ = "Auto-Clicker Development Team"

from .auto_clicker import AutoClicker
from .window_manager import WindowManager
from .screen_capture import ScreenCapture
from .image_matcher import ImageMatcher, MatchResult
from .mouse_controller import MouseController
from .action_handler import ActionHandler
from .config_loader import ConfigLoader

__all__ = [
    'AutoClicker',
    'WindowManager',
    'ScreenCapture',
    'ImageMatcher',
    'MatchResult',
    'MouseController',
    'ActionHandler',
    'ConfigLoader',
]
