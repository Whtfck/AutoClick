"""
Image Matcher Module
Template matching using OpenCV
"""
import cv2
import numpy as np
import logging
from typing import Optional, Tuple, Dict
from dataclasses import dataclass

logger = logging.getLogger(__name__)


@dataclass
class MatchResult:
    """Result of template matching operation"""
    matched: bool
    confidence: float
    location: Optional[Tuple[int, int]] = None  # (x, y)
    template_size: Optional[Tuple[int, int]] = None  # (width, height)
    min_val: float = 0.0
    max_val: float = 0.0
    min_loc: Optional[Tuple[int, int]] = None
    max_loc: Optional[Tuple[int, int]] = None


class ImageMatcher:
    """Image matching using OpenCV template matching"""
    
    def __init__(self, threshold: float = 0.8):
        """
        Initialize image matcher
        
        Args:
            threshold: Matching confidence threshold (0.0-1.0)
        """
        self.threshold = threshold
        self.template_cache: Dict[str, np.ndarray] = {}
    
    def load_template(self, template_path: str, use_cache: bool = True) -> Optional[np.ndarray]:
        """
        Load template image from file
        
        Args:
            template_path: Path to template image
            use_cache: Whether to cache loaded templates
            
        Returns:
            Template image as numpy array or None if failed
        """
        if use_cache and template_path in self.template_cache:
            logger.debug(f"Using cached template: {template_path}")
            return self.template_cache[template_path]
        
        try:
            template = cv2.imread(template_path, cv2.IMREAD_COLOR)
            if template is None:
                logger.error(f"Failed to load template: {template_path}")
                return None
            
            if use_cache:
                self.template_cache[template_path] = template
                logger.debug(f"Template cached: {template_path}")
            
            return template
        except Exception as e:
            logger.error(f"Error loading template {template_path}: {e}")
            return None
    
    def match_template(self, 
                      source: np.ndarray, 
                      template: np.ndarray,
                      method: int = cv2.TM_CCOEFF_NORMED) -> MatchResult:
        """
        Perform template matching
        
        Args:
            source: Source image (screenshot)
            template: Template image to find
            method: OpenCV matching method
            
        Returns:
            MatchResult object
        """
        try:
            # Convert to grayscale for faster matching
            if len(source.shape) == 3:
                source_gray = cv2.cvtColor(source, cv2.COLOR_BGR2GRAY)
            else:
                source_gray = source
            
            if len(template.shape) == 3:
                template_gray = cv2.cvtColor(template, cv2.COLOR_BGR2GRAY)
            else:
                template_gray = template
            
            # Perform template matching
            result = cv2.matchTemplate(source_gray, template_gray, method)
            
            # Find best match location
            min_val, max_val, min_loc, max_loc = cv2.minMaxLoc(result)
            
            # For TM_CCOEFF_NORMED and TM_CCORR_NORMED, use max_val and max_loc
            # For TM_SQDIFF_NORMED, use min_val and min_loc
            if method in [cv2.TM_SQDIFF, cv2.TM_SQDIFF_NORMED]:
                confidence = 1.0 - min_val
                location = min_loc
            else:
                confidence = max_val
                location = max_loc
            
            matched = confidence >= self.threshold
            
            template_h, template_w = template_gray.shape
            
            result_obj = MatchResult(
                matched=matched,
                confidence=confidence,
                location=location,
                template_size=(template_w, template_h),
                min_val=min_val,
                max_val=max_val,
                min_loc=min_loc,
                max_loc=max_loc
            )
            
            logger.debug(f"Match result: confidence={confidence:.3f}, matched={matched}, location={location}")
            return result_obj
            
        except Exception as e:
            logger.error(f"Error during template matching: {e}")
            return MatchResult(matched=False, confidence=0.0)
    
    def match_template_from_file(self,
                                source: np.ndarray,
                                template_path: str,
                                method: int = cv2.TM_CCOEFF_NORMED) -> MatchResult:
        """
        Perform template matching with template loaded from file
        
        Args:
            source: Source image (screenshot)
            template_path: Path to template image
            method: OpenCV matching method
            
        Returns:
            MatchResult object
        """
        template = self.load_template(template_path)
        if template is None:
            return MatchResult(matched=False, confidence=0.0)
        
        return self.match_template(source, template, method)
    
    def match_multiple(self,
                      source: np.ndarray,
                      template_paths: list,
                      method: int = cv2.TM_CCOEFF_NORMED) -> Dict[str, MatchResult]:
        """
        Match multiple templates against source image
        
        Args:
            source: Source image (screenshot)
            template_paths: List of template image paths
            method: OpenCV matching method
            
        Returns:
            Dictionary mapping template path to MatchResult
        """
        results = {}
        for template_path in template_paths:
            result = self.match_template_from_file(source, template_path, method)
            results[template_path] = result
        return results
    
    def draw_match(self,
                  image: np.ndarray,
                  match_result: MatchResult,
                  color: Tuple[int, int, int] = (0, 255, 0),
                  thickness: int = 2) -> np.ndarray:
        """
        Draw rectangle around matched region
        
        Args:
            image: Image to draw on
            match_result: Match result containing location and size
            color: Rectangle color (BGR)
            thickness: Rectangle line thickness
            
        Returns:
            Image with drawn rectangle
        """
        if not match_result.matched or not match_result.location or not match_result.template_size:
            return image
        
        x, y = match_result.location
        w, h = match_result.template_size
        
        img_copy = image.copy()
        cv2.rectangle(img_copy, (x, y), (x + w, y + h), color, thickness)
        
        return img_copy
    
    def clear_cache(self):
        """Clear template cache"""
        self.template_cache.clear()
        logger.info("Template cache cleared")
