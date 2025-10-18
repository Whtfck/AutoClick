"""
Config Loader Module
Load and parse configuration files (JSON/YAML)
"""
import json
import yaml
import os
import logging
from typing import Optional, Dict, Any
from pathlib import Path

logger = logging.getLogger(__name__)


class ConfigLoader:
    """Configuration file loader supporting JSON and YAML"""
    
    @staticmethod
    def load_json(filepath: str) -> Optional[Dict[str, Any]]:
        """
        Load JSON configuration file
        
        Args:
            filepath: Path to JSON file
            
        Returns:
            Configuration dictionary or None if failed
        """
        try:
            with open(filepath, 'r', encoding='utf-8') as f:
                config = json.load(f)
            logger.info(f"Loaded JSON config from: {filepath}")
            return config
        except FileNotFoundError:
            logger.error(f"Config file not found: {filepath}")
        except json.JSONDecodeError as e:
            logger.error(f"Invalid JSON format in {filepath}: {e}")
        except Exception as e:
            logger.error(f"Error loading JSON config: {e}")
        return None
    
    @staticmethod
    def load_yaml(filepath: str) -> Optional[Dict[str, Any]]:
        """
        Load YAML configuration file
        
        Args:
            filepath: Path to YAML file
            
        Returns:
            Configuration dictionary or None if failed
        """
        try:
            with open(filepath, 'r', encoding='utf-8') as f:
                config = yaml.safe_load(f)
            logger.info(f"Loaded YAML config from: {filepath}")
            return config
        except FileNotFoundError:
            logger.error(f"Config file not found: {filepath}")
        except yaml.YAMLError as e:
            logger.error(f"Invalid YAML format in {filepath}: {e}")
        except Exception as e:
            logger.error(f"Error loading YAML config: {e}")
        return None
    
    @staticmethod
    def load(filepath: str) -> Optional[Dict[str, Any]]:
        """
        Load configuration file (auto-detect format from extension)
        
        Args:
            filepath: Path to configuration file
            
        Returns:
            Configuration dictionary or None if failed
        """
        if not os.path.exists(filepath):
            logger.error(f"Config file does not exist: {filepath}")
            return None
        
        ext = Path(filepath).suffix.lower()
        
        if ext == '.json':
            return ConfigLoader.load_json(filepath)
        elif ext in ['.yaml', '.yml']:
            return ConfigLoader.load_yaml(filepath)
        else:
            logger.error(f"Unsupported config file format: {ext}")
            return None
    
    @staticmethod
    def validate_config(config: Dict[str, Any]) -> bool:
        """
        Validate configuration structure
        
        Args:
            config: Configuration dictionary
            
        Returns:
            True if valid, False otherwise
        """
        try:
            if 'ProcessList' not in config:
                logger.error("Config missing 'ProcessList'")
                return False
            
            process_list = config['ProcessList']
            if not isinstance(process_list, list) or len(process_list) == 0:
                logger.error("'ProcessList' must be a non-empty list")
                return False
            
            for idx, process in enumerate(process_list):
                if 'ProcessName' not in process:
                    logger.error(f"Process {idx} missing 'ProcessName'")
                    return False
                
                if 'Tasks' not in process:
                    logger.error(f"Process {process['ProcessName']} missing 'Tasks'")
                    return False
                
                if not isinstance(process['Tasks'], list):
                    logger.error(f"Process {process['ProcessName']} 'Tasks' must be a list")
                    return False
                
                for task_idx, task in enumerate(process['Tasks']):
                    if 'IconGroups' not in task:
                        logger.error(f"Task {task_idx} in {process['ProcessName']} missing 'IconGroups'")
                        return False
                    
                    if 'Actions' not in task:
                        logger.error(f"Task {task_idx} in {process['ProcessName']} missing 'Actions'")
                        return False
            
            logger.info("Config validation passed")
            return True
            
        except Exception as e:
            logger.error(f"Error validating config: {e}")
            return False
    
    @staticmethod
    def get_process_config(config: Dict[str, Any], process_name: str) -> Optional[Dict[str, Any]]:
        """
        Get configuration for specific process
        
        Args:
            config: Full configuration dictionary
            process_name: Name of the process
            
        Returns:
            Process configuration or None if not found
        """
        process_list = config.get('ProcessList', [])
        
        for process in process_list:
            if process.get('ProcessName', '').lower() == process_name.lower():
                logger.info(f"Found config for process: {process_name}")
                return process
        
        logger.warning(f"No config found for process: {process_name}")
        return None
