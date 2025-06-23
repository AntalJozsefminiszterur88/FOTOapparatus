# build-exe.py

import os
import shutil
import subprocess
import sys
import PySide6  # Importáljuk a csomagot, hogy megtaláljuk az útvonalát

# --- Konfiguráció ---
APP_NAME = "FOTOapp"
ENTRY_POINT = "main.py"
ICON_PATH = os.path.join("assets", "camera_icon.ico")
ASSETS_PATH = "assets"
PROJECT_ROOT = os.path.dirname(os.path.abspath(__file__))

def main():
    """Lefuttatja a teljes, automatizált build folyamatot."""
    print(">>> Build folyamat elindítva...")

    # 1. A PySide6 stílusfájlok útvonalának automatikus megkeresése
    print(">>> PySide6 stílusfájlok keresése...")
    try:
        pyside_path = os.path.dirname(PySide6.__file__)
        pyside_styles_path = os.path.join(pyside_path, "plugins", "styles")
        if not os.path.exists(pyside_styles_path):
            raise FileNotFoundError
        print(f"    - Stílusok megtalálva itt: {pyside_styles_path}")
    except (ImportError, FileNotFoundError):
        print("Hiba: A PySide6 vagy a 'styles' mappa nem található.")
        print("Ellenőrizd, hogy a PySide6 helyesen van-e telepítve.")
        sys.exit(1)

    # 2. Takarítás: Korábbi build maradványok törlése
    print(">>> Korábbi build fájlok törlése...")
    for folder in ["build", "dist"]:
        if os.path.exists(folder):
            shutil.rmtree(folder)
            print(f"    - '{folder}' mappa törölve.")
    spec_file = f"{APP_NAME}.spec"
    if os.path.exists(spec_file):
        os.remove(spec_file)
        print(f"    - '{spec_file}' fájl törölve.")

    # 3. A PyInstaller parancs összeállítása a dinamikus útvonalakkal
    pyinstaller_command = [
        "pyinstaller",
        # Megmondjuk a PyInstaller-nek, hol keresse a saját moduljainkat (gui, core)
        f"--paths={PROJECT_ROOT}",
        "--name", APP_NAME,
        "--onefile",
        # A program kész, nem kell a konzolablak
        "--windowed",
        f"--icon={ICON_PATH}",
        
        # Hozzáadja az 'assets' mappát
        f"--add-data", f"{ASSETS_PATH}{os.pathsep}{ASSETS_PATH}",
        
        # Hozzáadja a stílusfájlokat, hogy minden gépen jól nézzen ki
        f"--add-data", f"{pyside_styles_path}{os.pathsep}PySide6/plugins/styles",
        
        # Fontos "rejtett" importok
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
    print("(Ez eltarthat néhány percig, légy türelemmel...)\n")

    # 4. A parancs lefuttatása
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
