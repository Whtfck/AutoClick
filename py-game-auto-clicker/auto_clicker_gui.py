"""
Auto Clicker GUI
Simple graphical interface for the auto-clicker
"""
import tkinter as tk
from tkinter import ttk, filedialog, messagebox, scrolledtext
import threading
import logging
import os
import sys
from auto_clicker import AutoClicker

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s'
)


class TextHandler(logging.Handler):
    """Logging handler that writes to a text widget"""
    
    def __init__(self, text_widget):
        super().__init__()
        self.text_widget = text_widget
    
    def emit(self, record):
        msg = self.format(record)
        def append():
            self.text_widget.insert(tk.END, msg + '\n')
            self.text_widget.see(tk.END)
        self.text_widget.after(0, append)


class AutoClickerGUI:
    """GUI for Auto-Clicker"""
    
    def __init__(self, root):
        self.root = root
        self.root.title("Game Auto-Clicker")
        self.root.geometry("700x600")
        
        self.clicker = None
        self.config_path = ""
        
        self.create_widgets()
        self.setup_logging()
    
    def create_widgets(self):
        """Create GUI widgets"""
        # Main frame
        main_frame = ttk.Frame(self.root, padding="10")
        main_frame.grid(row=0, column=0, sticky=(tk.W, tk.E, tk.N, tk.S))
        
        # Config file selection
        ttk.Label(main_frame, text="Configuration File:").grid(row=0, column=0, sticky=tk.W, pady=5)
        self.config_entry = ttk.Entry(main_frame, width=50)
        self.config_entry.grid(row=0, column=1, sticky=(tk.W, tk.E), pady=5)
        ttk.Button(main_frame, text="Browse", command=self.browse_config).grid(row=0, column=2, padx=5, pady=5)
        
        # Process name
        ttk.Label(main_frame, text="Process Name:").grid(row=1, column=0, sticky=tk.W, pady=5)
        self.process_entry = ttk.Entry(main_frame, width=50)
        self.process_entry.grid(row=1, column=1, sticky=(tk.W, tk.E), pady=5)
        
        # Capture method
        ttk.Label(main_frame, text="Capture Method:").grid(row=2, column=0, sticky=tk.W, pady=5)
        self.capture_var = tk.StringVar(value="win32")
        capture_frame = ttk.Frame(main_frame)
        capture_frame.grid(row=2, column=1, sticky=tk.W, pady=5)
        ttk.Radiobutton(capture_frame, text="Win32", variable=self.capture_var, value="win32").pack(side=tk.LEFT, padx=5)
        ttk.Radiobutton(capture_frame, text="MSS", variable=self.capture_var, value="mss").pack(side=tk.LEFT, padx=5)
        
        # Status
        ttk.Label(main_frame, text="Status:").grid(row=3, column=0, sticky=tk.W, pady=5)
        self.status_label = ttk.Label(main_frame, text="Stopped", foreground="red")
        self.status_label.grid(row=3, column=1, sticky=tk.W, pady=5)
        
        # Control buttons
        button_frame = ttk.Frame(main_frame)
        button_frame.grid(row=4, column=0, columnspan=3, pady=10)
        
        self.start_button = ttk.Button(button_frame, text="Start", command=self.start_clicker, width=15)
        self.start_button.pack(side=tk.LEFT, padx=5)
        
        self.stop_button = ttk.Button(button_frame, text="Stop", command=self.stop_clicker, width=15, state=tk.DISABLED)
        self.stop_button.pack(side=tk.LEFT, padx=5)
        
        # Log area
        ttk.Label(main_frame, text="Log:").grid(row=5, column=0, sticky=tk.W, pady=5)
        
        log_frame = ttk.Frame(main_frame)
        log_frame.grid(row=6, column=0, columnspan=3, sticky=(tk.W, tk.E, tk.N, tk.S), pady=5)
        
        self.log_text = scrolledtext.ScrolledText(log_frame, height=20, state=tk.NORMAL)
        self.log_text.pack(fill=tk.BOTH, expand=True)
        
        # Configure grid weights for resizing
        self.root.columnconfigure(0, weight=1)
        self.root.rowconfigure(0, weight=1)
        main_frame.columnconfigure(1, weight=1)
        main_frame.rowconfigure(6, weight=1)
    
    def setup_logging(self):
        """Setup logging to text widget"""
        text_handler = TextHandler(self.log_text)
        text_handler.setFormatter(logging.Formatter('%(asctime)s - %(levelname)s - %(message)s'))
        logging.getLogger().addHandler(text_handler)
    
    def browse_config(self):
        """Browse for configuration file"""
        filename = filedialog.askopenfilename(
            title="Select Configuration File",
            filetypes=[("JSON files", "*.json"), ("YAML files", "*.yaml *.yml"), ("All files", "*.*")]
        )
        if filename:
            self.config_entry.delete(0, tk.END)
            self.config_entry.insert(0, filename)
            self.config_path = filename
    
    def start_clicker(self):
        """Start the auto-clicker"""
        config_path = self.config_entry.get()
        process_name = self.process_entry.get()
        
        if not config_path:
            messagebox.showerror("Error", "Please select a configuration file")
            return
        
        if not process_name:
            messagebox.showerror("Error", "Please enter a process name")
            return
        
        if not os.path.exists(config_path):
            messagebox.showerror("Error", "Configuration file does not exist")
            return
        
        # Create clicker instance
        capture_method = self.capture_var.get()
        self.clicker = AutoClicker(config_path, capture_method=capture_method)
        
        # Start in separate thread
        def start_thread():
            success = self.clicker.start(process_name)
            if not success:
                self.root.after(0, lambda: messagebox.showerror("Error", "Failed to start auto-clicker"))
                self.root.after(0, self.update_stopped_state)
        
        threading.Thread(target=start_thread, daemon=True).start()
        
        # Update UI
        self.update_running_state()
    
    def stop_clicker(self):
        """Stop the auto-clicker"""
        if self.clicker:
            self.clicker.stop()
        self.update_stopped_state()
    
    def update_running_state(self):
        """Update UI for running state"""
        self.status_label.config(text="Running", foreground="green")
        self.start_button.config(state=tk.DISABLED)
        self.stop_button.config(state=tk.NORMAL)
        self.config_entry.config(state=tk.DISABLED)
        self.process_entry.config(state=tk.DISABLED)
    
    def update_stopped_state(self):
        """Update UI for stopped state"""
        self.status_label.config(text="Stopped", foreground="red")
        self.start_button.config(state=tk.NORMAL)
        self.stop_button.config(state=tk.DISABLED)
        self.config_entry.config(state=tk.NORMAL)
        self.process_entry.config(state=tk.NORMAL)
    
    def on_closing(self):
        """Handle window closing"""
        if self.clicker and self.clicker.is_active():
            if messagebox.askokcancel("Quit", "Auto-clicker is still running. Do you want to quit?"):
                self.stop_clicker()
                self.root.destroy()
        else:
            self.root.destroy()


def main():
    """Main entry point"""
    root = tk.Tk()
    app = AutoClickerGUI(root)
    root.protocol("WM_DELETE_WINDOW", app.on_closing)
    root.mainloop()


if __name__ == '__main__':
    main()
