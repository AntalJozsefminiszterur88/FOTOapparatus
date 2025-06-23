# build-exe.py

import os
import shutil
import subprocess
import sys
import PySide6

# --- Konfiguráció ---
APP_NAME = "FOTOapp"
ENTRY_POINT = "main.py"
ICON_PATH = os.path.join("assets", "camera_icon.ico")
ASSETS_PATH = "assets"
PROJECT_ROOT = os.path.dirname(os.path.abspath(__file__))

def main():
    print(">>> Build folyamat elindítva...")

    # Stílusok és a qt.conf útvonalának megkeresése
    try:
        pyside_path = os.path.dirname(PySide6.__file__)
        pyside_plugins_path = os.path.join(pyside_path, "plugins")
        if not os.path.exists(pyside_plugins_path): raise FileNotFoundError
        print(f"    - Plugins mappa megtalálva: {pyside_plugins_path}")
        
        qt_conf_path = os.path.join(PROJECT_ROOT, "qt.conf")
        if not os.path.exists(qt_conf_path):
             print("HIBA: A 'qt.conf' fájl nem található a projekt gyökerében!")
             sys.exit(1)

    except (ImportError, FileNotFoundError):
        print("Hiba: A PySide6 vagy a 'plugins' mappa nem található.")
        sys.exit(1)

    # Takarítás
    print(">>> Korábbi build fájlok törlése...")
    for folder in ["build", "dist"]:
        if os.path.exists(folder): shutil.rmtree(folder)
    if os.path.exists(f"{APP_NAME}.spec"): os.remove(f"{APP_NAME}.spec")

    # --- A VÉGSŐ MEGOLDÁS: Nem egy fájlba, hanem egy mappába csomagolunk! ---
    # Ez a legstabilabb módja a komplex alkalmazások terjesztésének.
    pyinstaller_command = [
        "pyinstaller",
        # --onefile HELYETT a --onedir-t használjuk
        "--noconfirm", # Ne kérdezzen rá a törlésre
        "--name", APP_NAME,
        "--windowed",
        f"--icon={ICON_PATH}",
        
        # Hozzáadjuk a qt.conf fájlt a gyökérbe
        f"--add-data", f"{qt_conf_path}{os.pathsep}.",
        
        # Hozzáadjuk a teljes plugins mappát
        f"--add-data", f"{pyside_plugins_path}{os.pathsep}PySide6/plugins",
        
        # Hozzáadjuk az assets mappát
        f"--add-data", f"{ASSETS_PATH}{os.pathsep}assets",
        
        # A rejtett importok továbbra is fontosak
        "--hidden-import", "PySide6.QtNetwork",
        "--hidden-import", "apscheduler.triggers.cron",
        "--hidden-import", "apscheduler.jobstores.base",
        "--hidden-import", "pygetwindow",
        "--hidden-import", "win32gui",
        "--hidden-import", "win32process",
        
        ENTRY_POINT
    ]

    print("\n>>> PyInstaller parancs futtatása (mappa mód)...")
    print(" ".join(pyinstaller_command))

    try:
        subprocess.run(pyinstaller_command, check=True)
        print("\n---------------------------\n")
        print(f"✓ SIKER! Az elkészült program a 'dist\\{APP_NAME}' mappában található.")
        print(f"Az indítófájl: 'dist\\{APP_NAME}\\{APP_NAME}.exe'")
        
    except subprocess.CalledProcessError:
        print("\n--- HIBA A BUILD SORÁN ---")
        sys.exit(1)

if __name__ == "__main__":
    main()
