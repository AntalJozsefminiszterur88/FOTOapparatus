# core/config_manager.py

import json
import os
import sys

# Figyelem: A PySide6 importokat eltávolítottuk innen, hogy ne fussanak le túl korán!

class ConfigManager:
    COMPANY_NAME = "UMKGL Solutions"
    APP_SUBFOLDER_NAME = "FOTOapp"

    def __init__(self, config_filename="fotoapp_config.json"):
        self.config_filename = config_filename
        self.config_path = self._get_config_path()
        self._ensure_config_dir_exists()

    def _get_qstandard_path(self, location_enum):
        """Késleltetett importálással éri el a QStandardPaths-t."""
        from PySide6.QtCore import QStandardPaths
        return QStandardPaths.writableLocation(location_enum)

    def _get_config_path(self):
        from PySide6.QtCore import QStandardPaths
        documents_location = self._get_qstandard_path(QStandardPaths.StandardLocation.DocumentsLocation)
        if not documents_location:
            fallback_dir = os.path.join(os.path.abspath("."), self.COMPANY_NAME, self.APP_SUBFOLDER_NAME)
            return os.path.join(fallback_dir, self.config_filename)
        config_dir = os.path.join(documents_location, self.COMPANY_NAME, self.APP_SUBFOLDER_NAME)
        return os.path.join(config_dir, self.config_filename)

    def _ensure_config_dir_exists(self):
        dir_path = os.path.dirname(self.config_path)
        if not os.path.exists(dir_path):
            try:
                os.makedirs(dir_path, exist_ok=True)
            except OSError as e:
                print(f"Hiba a config mappa létrehozásakor ({dir_path}): {e}", file=sys.stderr)

    def get_default_settings(self):
        from PySide6.QtCore import QStandardPaths
        pictures_location = self._get_qstandard_path(QStandardPaths.StandardLocation.PicturesLocation)
        screenshots_app_folder = "FOTOapp_Screenshots"

        if pictures_location:
            default_save_path = os.path.join(pictures_location, screenshots_app_folder)
        else:
            default_save_path = screenshots_app_folder

        # Itt már nem kell mappát létrehozni, azt a take_screenshot megteszi.
        # Ez a függvény csak az alapértelmezett útvonalat adja vissza.
        return {
            "save_path": default_save_path,
            "capture_type": "screenshot",
            "screenshot_mode": "fullscreen",
            "custom_area": {"x": 0, "y": 0, "width": 100, "height": 100},
            "schedules": [],
            "target_window": "",
            "include_timestamp": True,
            "timestamp_position": "top-left",
            "discord_settings": {
                "stay_foreground": False,
                "use_hotkey": False,
                "hotkey_number": 1,
            },
        }

    def load_settings(self):
        if not os.path.exists(self.config_path):
            return self.get_default_settings()
        try:
            with open(self.config_path, 'r', encoding='utf-8') as f:
                settings = json.load(f)
            # Biztosítjuk, hogy minden kulcs létezzen
            default_settings = self.get_default_settings()
            for key, value in default_settings.items():
                if isinstance(value, dict):
                    settings.setdefault(key, {})
                    for sub_key, sub_val in value.items():
                        settings[key].setdefault(sub_key, sub_val)
                else:
                    settings.setdefault(key, value)
            return settings
        except (FileNotFoundError, json.JSONDecodeError, Exception) as e:
            print(f"Hiba a config betöltésekor ({self.config_path}): {e}", file=sys.stderr)
            return self.get_default_settings()

    def save_settings(self, settings):
        try:
            self._ensure_config_dir_exists()
            with open(self.config_path, 'w', encoding='utf-8') as f:
                json.dump(settings, f, ensure_ascii=False, indent=4)
            return True
        except (IOError, TypeError, Exception) as e:
            print(f"Hiba a config mentésekor ({self.config_path}): {e}", file=sys.stderr)
            return False

# Teszt rész eltávolítva a felesleges Qt importok miatt
