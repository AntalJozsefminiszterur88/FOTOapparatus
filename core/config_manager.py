# core/config_manager.py

import json
import os
import sys
from PySide6.QtCore import QStandardPaths, QCoreApplication # QCoreApplication is nem árt itt
from PySide6.QtWidgets import QApplication

class ConfigManager:
    """Kezeli az alkalmazás beállításainak mentését és betöltését."""

    # A cég és alkalmazás nevét itt is definiálhatjuk a konzisztencia érdekében,
    # vagy hagyatkozhatunk a QCoreApplication-ben beállított értékekre.
    # A QCoreApplication.organizationName() és applicationName() a main.py-ben van beállítva.
    COMPANY_NAME = "UMKGL Solutions" # Ezt használjuk a Dokumentumok mappában
    APP_SUBFOLDER_NAME = "FOTOapp"   # Az alkalmazás almappája a céges mappán belül

    def __init__(self, config_filename="fotoapp_config.json"):
        """
        Inicializálja a ConfigManager-t.
        """
        self.config_filename = config_filename
        # Az _get_config_path most már a Dokumentumok mappába fog mutatni
        self.config_path = self._get_config_path()
        print(f"Konfigurációs fájl kívánt helye: {self.config_path}")
        self._ensure_config_dir_exists() # Ez létrehozza a Dokumentumok/UMKGL Solutions/FOTOapp mappát is

    def _get_config_path(self):
        """
        Meghatározza a konfigurációs fájl helyét a Dokumentumok mappában.
        Dokumentumok / COMPANY_NAME / APP_SUBFOLDER_NAME / config_filename
        """
        documents_location = QStandardPaths.writableLocation(QStandardPaths.StandardLocation.DocumentsLocation)

        if not documents_location:
            # Nagyon ritka vészhelyzet: ha a Dokumentumok mappa nem elérhető
            print("Figyelmeztetés: Dokumentumok mappa nem található! A konfigurációs fájl a program mellé kerül.", file=sys.stderr)
            # Ebben az esetben a program futtatási könyvtárát használjuk
            # egy almappával, hogy ne legyen összevisszaság.
            fallback_dir = os.path.join(os.path.abspath("."), self.COMPANY_NAME, self.APP_SUBFOLDER_NAME)
            # Nem ideális, de jobb, mint a semmi.
            return os.path.join(fallback_dir, self.config_filename)

        # Standard hely: Dokumentumok/Cégnév/Appnév/config.json
        config_dir = os.path.join(documents_location, self.COMPANY_NAME, self.APP_SUBFOLDER_NAME)
        return os.path.join(config_dir, self.config_filename)

    def _ensure_config_dir_exists(self):
        """Biztosítja, hogy a konfigurációs fájlt tartalmazó könyvtár létezzen."""
        dir_path = os.path.dirname(self.config_path)
        if not os.path.exists(dir_path):
            try:
                os.makedirs(dir_path, exist_ok=True)
                print(f"Létrehozva a konfigurációs fájl mappája: {dir_path}")
            except OSError as e:
                print(f"Hiba: Nem sikerült létrehozni a konfigurációs fájl mappáját ({dir_path}): {e}", file=sys.stderr)

    def get_default_settings(self):
        """Visszaadja az alapértelmezett beállításokat."""
        
        # --- KÉPERNYŐKÉPEK ALAPÉRTELMEZETT MENTÉSI HELYE ---
        pictures_location = QStandardPaths.writableLocation(QStandardPaths.StandardLocation.PicturesLocation)
        screenshots_app_folder = "FOTOapp_Screenshots" # Külön mappa a Képeken belül

        if not pictures_location:
            print("Figyelmeztetés: Képek mappa nem található, a program mappájában lesz a screenshotok mentési helye.", file=sys.stderr)
            default_screenshot_save_path = screenshots_app_folder # Program mellett
        else:
            default_screenshot_save_path = os.path.join(pictures_location, screenshots_app_folder)
        
        try:
            if not os.path.exists(default_screenshot_save_path):
                 os.makedirs(default_screenshot_save_path, exist_ok=True)
                 print(f"Alapértelmezett screenshot mentési mappa létrehozva: {default_screenshot_save_path}")
        except OSError as e:
             print(f"Hiba az alapértelmezett screenshot mappa ({default_screenshot_save_path}) létrehozásakor: {e}. Relatív útvonal lesz használva.", file=sys.stderr)
             # Ha a Képek mappába nem sikerült, akkor a program mellé próbáljuk
             default_screenshot_save_path = screenshots_app_folder
             if not os.path.exists(default_screenshot_save_path):
                 try:
                     os.makedirs(default_screenshot_save_path, exist_ok=True)
                 except OSError:
                     pass # Végső esetben a take_screenshot panaszkodik majd

        return {
            # Ez most a Képek mappába mutat alapértelmezetten
            "save_path": default_screenshot_save_path,
            "screenshot_mode": "fullscreen",
            "custom_area": {"x": 0, "y": 0, "width": 100, "height": 100},
            "schedules": []
        }

    def load_settings(self):
        if not os.path.exists(self.config_path):
            print(f"Konfigurációs fájl nem található itt: {self.config_path}. Alapértelmezett beállítások használata.")
            return self.get_default_settings()
        try:
            with open(self.config_path, 'r', encoding='utf-8') as f:
                settings = json.load(f)
                print(f"Beállítások sikeresen betöltve innen: {self.config_path}")
                default_settings = self.get_default_settings()
                for key, value in default_settings.items():
                    if key not in settings:
                        settings[key] = value
                    elif key == "custom_area" and not all(k in settings.get(key, {}) for k in ["x","y","width","height"]):
                        settings[key] = value # Ha a custom_area struktúra hibás/hiányos, visszaállítjuk
                return settings
        except (FileNotFoundError, json.JSONDecodeError, Exception) as e:
            print(f"Hiba a konfigurációs fájl ({self.config_path}) betöltésekor: {e}", file=sys.stderr)
            print("Alapértelmezett beállítások használata.")
            return self.get_default_settings()

    def save_settings(self, settings):
        try:
            self._ensure_config_dir_exists() # Biztosítja a Dokumentumok/UMKGL Solutions/FOTOapp mappa létezését
            with open(self.config_path, 'w', encoding='utf-8') as f:
                json.dump(settings, f, ensure_ascii=False, indent=4)
            print(f"Beállítások sikeresen elmentve ide: {self.config_path}")
            return True
        except (IOError, TypeError, Exception) as e:
            print(f"Hiba a konfigurációs fájl ({self.config_path}) mentésekor: {e}", file=sys.stderr)
            return False

# --- Tesztelési rész ---
if __name__ == '__main__':
    # A QStandardPaths használatához kell QApplication
    # (Ez a main.py-ben már létezik, amikor az alkalmazás fut)
    _app = QApplication.instance()
    if not _app:
        _app = QApplication(sys.argv)
    
    # QCoreApplication adatok beállítása a teszthez, ha a ConfigManager használná őket
    # (a jelenlegi _get_config_path még nem használja, de a jövőben hasznos lehet)
    if not QCoreApplication.organizationName():
        QCoreApplication.setOrganizationName("TestOrg")
    if not QCoreApplication.applicationName():
        QCoreApplication.setApplicationName("TestAppForConfig")

    manager = ConfigManager("test_settings.json") # Egyedi fájlnév a teszthez

    print("\n--- ConfigManager Teszt (új útvonalakkal) ---")
    print(f"Konfigurációs fájl helye: {manager.config_path}")

    defaults = manager.get_default_settings()
    print("\nAlapértelmezett beállítások (default screenshot save path):")
    print(json.dumps(defaults, indent=4, ensure_ascii=False))

    print("\nBeállítások betöltése (ha létezik a tesztfájl)...")
    loaded_settings = manager.load_settings()
    print("Betöltött beállítások:")
    print(json.dumps(loaded_settings, indent=4, ensure_ascii=False))

    loaded_settings["schedules"].append({"time": "18:00", "days": ["Szo"]})
    loaded_settings["screenshot_mode"] = "custom"
    print("\nBeállítások mentése...")
    success = manager.save_settings(loaded_settings)
    if success:
        print("Mentés sikeres.")
        print("\nÚjratöltés ellenőrzéshez...")
        reloaded_settings = manager.load_settings()
        print("Újratöltött beállítások:")
        print(json.dumps(reloaded_settings, indent=4, ensure_ascii=False))
    else:
        print("Mentés sikertelen.")
    
    print(f"\nEllenőrizd a {manager.config_path} fájlt és a {defaults['save_path']} mappát.")
