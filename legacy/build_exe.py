# build.py

import os
import sys
import shutil
import platform
from pathlib import Path

try:
    import PyInstaller.__main__
except ImportError:
    print("Hiba: A PyInstaller nincs telepítve.")
    print("Kérem, telepítse a következő paranccsal: pip install pyinstaller")
    sys.exit(1)

# --- FONTOS ELŐKÉSZÜLETEK ---
# 1. Győződjön meg róla, hogy léteznek az alábbi üres fájlok:
#    - core/__init__.py
#    - gui/__init__.py
#    Ezek jelzik a Python számára, hogy a mappák importálható csomagok.
#
# 2. Ajánlott egy `requirements.txt` fájlt létrehozni és abból telepíteni
#    az összes függőséget (`pip install -r requirements.txt`).

# --- KONFIGURÁCIÓ ---
ENTRY_POINT = "main.py"
EXE_NAME = "FOTO-Apparatus"
ICON_PATH = os.path.join("assets", "camera_icon.ico")
BUILD_DIR = "build"
SPEC_FILE = f"{EXE_NAME}.spec"
DIST_DIR = "dist"

def run_pyinstaller():
    """
    Elindítja a PyInstaller folyamatot a megadott konfigurációval.
    """
    pyinstaller_args = [
        '--name', EXE_NAME,
        '--onefile',             # Egyetlen .exe fájl létrehozása.
        '--windowed',            # GUI alkalmazás, ne jelenjen meg konzolablak.
        '--icon', ICON_PATH,     # Ikon beállítása.
        '--clean',               # Törli a PyInstaller gyorsítótárát a build előtt.

        # --- JAVÍTÁS: A PROJEKT STRUKTÚRÁJÁNAK KEZELÉSE ---
        # Megmondja a PyInstallernek, hogy a projekt gyökérmappáját is
        # vegye fel a keresési útvonalba. Ez oldja meg a "No module named 'core'"
        # és "No module named 'screenshot_taker'" típusú hibákat.
        '--paths', '.',

        # Adatfájlok (pl. képek, ikonok) hozzáadása.
        f'--add-data={os.path.join("assets", "")}{os.pathsep}assets',

        # --- REJTETT IMPORTOK ---
        # Modulok, amelyeket a PyInstaller nem biztos, hogy automatikusan észlel.
        # PySide6 specifikus:
        '--hidden-import=PySide6.QtNetwork',
        # APScheduler (időzítő):
        '--hidden-import=apscheduler',
        '--hidden-import=apscheduler.schedulers.background',
        '--hidden-import=apscheduler.triggers.cron',
        '--hidden-import=apscheduler.jobstores.base',
        # Windows API hívásokhoz:
        '--hidden-import=win32gui',
        '--hidden-import=win32con',
        '--hidden-import=win32ui',
        '--hidden-import=win32process',
        '--hidden-import=win32api',
        # Egyéb, potenciálisan problémás modulok:
        '--hidden-import=pygetwindow',
        '--hidden-import=pyautogui',

        # Maga a fő szkript.
        ENTRY_POINT
    ]

    print(">>> PyInstaller indítása a következő argumentumokkal:")
    print(f"    {' '.join(pyinstaller_args)}")
    print("-" * 60)

    try:
        PyInstaller.__main__.run(pyinstaller_args)
        print("-" * 60)
        print(">>> PyInstaller sikeresen lefutott.")
        return True
    except Exception as e:
        print("-" * 60)
        print(f">>> Hiba történt a PyInstaller futtatása közben: {e}")
        return False

def cleanup():
    """
    Eltávolítja a PyInstaller által generált ideiglenes fájlokat és mappákat.
    """
    print(">>> Takarítás...")
    try:
        if os.path.isdir(BUILD_DIR):
            shutil.rmtree(BUILD_DIR)
            print(f"    - '{BUILD_DIR}' mappa törölve.")
        if os.path.exists(SPEC_FILE):
            os.remove(SPEC_FILE)
            print(f"    - '{SPEC_FILE}' fájl törölve.")
        print(">>> Takarítás befejezve.")
    except Exception as e:
        print(f"    - Hiba a takarítás során: {e}")

def main():
    """
    A fő build folyamatot vezérlő függvény.
    """
    print("=" * 60)
    print(f"'{EXE_NAME}' ALKALMAZÁS CSOMAGOLÁSA")
    print("=" * 60)

    if not Path(ENTRY_POINT).exists():
        print(f"Hiba: A belépési pont ('{ENTRY_POINT}') nem található!")
        sys.exit(1)
    if not Path(ICON_PATH).exists():
        print(f"Figyelmeztetés: Az ikonfájl ('{ICON_PATH}') nem található!")

    if platform.system() != "Windows":
        print("Figyelmeztetés: Ez a projekt Windows-specifikus kódot tartalmaz.")
        print("A csomagolás más operációs rendszeren valószínűleg nem fog működni.")

    if run_pyinstaller():
        final_exe_path = Path(DIST_DIR) / f"{EXE_NAME}.exe"
        if final_exe_path.exists():
            print("\nCsomagolás sikeres!")
            print(f"Az elkészült fájl helye: {final_exe_path.resolve()}")
        else:
            print("\nHiba: A csomagolás lefutott, de a végleges .exe fájl nem jött létre!")
    else:
        print("\nA csomagolás sikertelen volt.")

    cleanup()
    print("\nFolyamat vége.")

if __name__ == "__main__":
    main()
