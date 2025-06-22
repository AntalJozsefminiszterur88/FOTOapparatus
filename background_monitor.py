import sys
import time
from datetime import datetime
from PySide6.QtWidgets import QApplication
from PySide6.QtCore import QRect

from core.config_manager import ConfigManager
from core.screenshot_taker import take_screenshot

DAY_MAP = {"H": 0, "K": 1, "Sze": 2, "Cs": 3, "P": 4, "Szo": 5, "V": 6}

def should_capture(now, rule):
    try:
        rule_time = rule.get("time")
        days = rule.get("days", [])
        if not rule_time or not days:
            return False
        if now.strftime("%H:%M") != rule_time:
            return False
        return any(DAY_MAP.get(d) == now.weekday() for d in days)
    except Exception:
        return False

def main():
    app = QApplication(sys.argv)
    cfg = ConfigManager()
    settings = cfg.load_settings()
    save_path = settings.get("save_path", ".")
    mode = settings.get("screenshot_mode", "fullscreen")
    capture_type = settings.get("capture_type", "screenshot")
    custom_area = settings.get("custom_area")
    schedules = settings.get("schedules", [])
    include_timestamp = settings.get("include_timestamp", True)
    timestamp_position = settings.get("timestamp_position", "top-left")
    executed = set()
    print("Idozito monitor elindult. Ctrl+C a leallitasahoz.")
    try:
        while True:
            now = datetime.now()
            for idx, rule in enumerate(schedules):
                if should_capture(now, rule):
                    key = (idx, now.strftime("%Y%m%d%H%M"))
                    if key in executed:
                        continue
                    rect = None
                    if mode == "custom" and custom_area:
                        rect = QRect(
                            custom_area.get("x", 0),
                            custom_area.get("y", 0),
                            custom_area.get("width", 0),
                            custom_area.get("height", 0),
                        )
                    take_screenshot(
                        save_path,
                        "Kép",
                        rect,
                        include_timestamp,
                        timestamp_position,
                        settings.get("target_window", ""),
                        capture_type,
                    )
                    executed.add(key)
            time.sleep(60)
    except KeyboardInterrupt:
        print("Monitor leallva.")

if __name__ == "__main__":
    main()
