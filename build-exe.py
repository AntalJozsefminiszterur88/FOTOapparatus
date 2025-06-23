# build-exe.py

import os
import shutil
import subprocess
import sys

# --- Konfiguráció ---
APP_NAME = "FOTOapp"
ENTRY_POINT = "main.py"
ICON_PATH = os.path.join("assets", "camera_icon.ico")
ASSETS_PATH = "assets"
# Meghatározzuk a projekt gyökérmappáját
PROJECT_ROOT = os.path.dirname(os.path.abspath(__file__))

def main():
    """Lefuttatja a teljes build folyamatot."""
    print(">>> Build folyamat elindítva...")

    try:
        import PyInstaller
    except ImportError:
        print("Hiba: A PyInstaller nincs telepítve.")
        print("Telepítsd: pip install pyinstaller")
        sys.exit(1)

    print(">>> Korábbi build fájlok törlése...")
    for folder in ["build", "dist"]:
        if os.path.exists(folder):
            shutil.rmtree(folder)
            print(f"    - '{folder}' mappa törölve.")
    
    spec_file = f"{APP_NAME}.spec"
    if os.path.exists(spec_file):
        os.remove(spec_file)
        print(f"    - '{spec_file}' fájl törölve.")

    # --- A VÉGLEGES MEGOLDÁS ---
    # Itt mondjuk meg a PyInstaller-nek, hogy a projekt gyökérmappáját
    # is vegye figyelembe a modulok keresésekor.
    pyinstaller_command = [
        "pyinstaller",
        f"--paths={PROJECT_ROOT}",  # <-- EZ A LEGFONTOSABB SOR!
        "--name", APP_NAME,
        "--onefile",
        # Ha már működik, ezt visszacserélheted --windowed-re
        "--windowed",
        f"--icon={ICON_PATH}",
        f"--add-data", f"{ASSETS_PATH}{os.pathsep}{ASSETS_PATH}",
        
        # A rejtett importok továbbra is fontosak lehetnek
        "--hidden-import", "PySide6.QtNetwork",
        "--hidden-import", "apscheduler.triggers.cron",
        "--hidden-import", "apscheduler.jobstores.base",
        "--hidden-import", "pygetwindow",
        "--hidden-import", "win32gui",
        "--hidden-import", "win32process",
        
        ENTRY_POINT
    ]

    print("\n>>> PyInstaller parancs futtatása...")
    print(" ".join(pyinstaller_command))
    print("(Ez eltarthat néhány percig...)\n")

    try:
        subprocess.run(pyinstaller_command, check=True)
        print("\n---------------------------\n")
        print(f"✓ SIKER! Az elkészült fájl itt található: dist\\{APP_NAME}.exe")
        
    except subprocess.CalledProcessError:
        print("\n--- HIBA A BUILD SORÁN ---")
        print(f"✗ HIBA: A PyInstaller hibával leállt.")
        sys.exit(1)

if __name__ == "__main__":
    main()
