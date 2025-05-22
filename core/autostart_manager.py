# core/autostart_manager.py

import sys
import os
import logging

_IS_WINDOWS = sys.platform == "win32"

if _IS_WINDOWS:
    import winreg
else:
    winreg = None

logger = logging.getLogger(__name__)
APP_NAME = "FOTOapp"
REG_PATH = r"Software\Microsoft\Windows\CurrentVersion\Run"
START_HIDDEN_ARG = "--start-hidden" # Kapcsoló a rejtett induláshoz

def _get_executable_path_or_command():
    """
    Meghatározza a futtatható fájl teljes elérési útját (ha .exe)
    vagy a teljes futtató parancsot (ha .py szkript), HOZZÁADVA A --start-hidden KAPCSOLÓT.
    """
    base_command = ""
    if getattr(sys, 'frozen', False): 
        executable = sys.executable
        base_command = f'"{executable}"'
        logger.debug(f"Autostart: Fagyasztott alkalmazás, alap parancs: {base_command}")
    else: 
        python_executable = sys.executable
        script_path = os.path.abspath(sys.argv[0]) # main.py útvonala
        base_command = f'"{python_executable}" "{script_path}"'
        logger.debug(f"Autostart: Szkriptként fut, alap parancs: {base_command}")
    
    full_command = f"{base_command} {START_HIDDEN_ARG}"
    logger.debug(f"Autostart: Teljes parancs (--start-hidden kapcsolóval): {full_command}")
    return full_command


def is_autostart_enabled(app_name=APP_NAME):
    if not _IS_WINDOWS:
        return False

    expected_command = _get_executable_path_or_command()
    if not expected_command:
        logger.error("Nem sikerült meghatározni a várt futtatási parancsot az autostart ellenőrzéshez.")
        return False

    try:
        key = winreg.OpenKey(winreg.HKEY_CURRENT_USER, REG_PATH, 0, winreg.KEY_READ)
        stored_command, reg_type = winreg.QueryValueEx(key, app_name)
        winreg.CloseKey(key)

        if stored_command == expected_command:
            logger.info(f"Autostart bejegyzés '{app_name}' néven megtalálható és parancsa egyezik: \"{stored_command}\"")
            return True
        else:
            logger.warning(f"Autostart bejegyzés '{app_name}' létezik, de a parancsa ELTÉR. "
                           f"Tárolt: \"{stored_command}\", Várt: \"{expected_command}\". "
                           "Letiltottnak tekintjük.")
            return False
    except FileNotFoundError:
        logger.info(f"Autostart bejegyzés '{app_name}' néven nem található.")
        return False
    except OSError as e:
        logger.error(f"Hiba az autostart állapotának ellenőrzésekor (OSError): {e}")
        return False
    except Exception as e:
        logger.error(f"Váratlan hiba az autostart állapotának ellenőrzésekor: {e}")
        return False


def enable_autostart(app_name=APP_NAME):
    if not _IS_WINDOWS:
        logger.error("Autostart engedélyezése csak Windows rendszeren támogatott.")
        return False

    command_to_run = _get_executable_path_or_command()
    if not command_to_run:
        logger.error("Nem sikerült meghatározni a futtatható parancsot az autostarthoz.")
        return False

    try:
        key = winreg.OpenKey(winreg.HKEY_CURRENT_USER, REG_PATH, 0, winreg.KEY_WRITE)
        winreg.SetValueEx(key, app_name, 0, winreg.REG_SZ, command_to_run)
        winreg.CloseKey(key)
        logger.info(f"Autostart sikeresen engedélyezve '{app_name}' néven. Parancs: \"{command_to_run}\"")
        return True
    except OSError as e:
        logger.error(f"Hiba az autostart engedélyezésekor (OSError, lehet jogosultsági probléma?): {e}")
        return False
    except Exception as e:
        logger.error(f"Váratlan hiba az autostart engedélyezésekor: {e}")
        return False


def disable_autostart(app_name=APP_NAME):
    if not _IS_WINDOWS:
        logger.error("Autostart letiltása csak Windows rendszeren támogatott.")
        return False

    try:
        key = winreg.OpenKey(winreg.HKEY_CURRENT_USER, REG_PATH, 0, winreg.KEY_WRITE)
        winreg.DeleteValue(key, app_name)
        winreg.CloseKey(key)
        logger.info(f"Autostart bejegyzés '{app_name}' sikeresen eltávolítva.")
        return True
    except FileNotFoundError:
        logger.info(f"Autostart bejegyzés '{app_name}' nem létezett, nincs mit eltávolítani.")
        return True
    except OSError as e:
        logger.error(f"Hiba az autostart letiltásakor (OSError): {e}")
        return False
    except Exception as e:
        logger.error(f"Váratlan hiba az autostart letiltásakor: {e}")
        return False

if __name__ == "__main__":
    if _IS_WINDOWS:
        print("\n--- Autostart Manager Teszt (Windows) ---")
        # ... (a tesztblokk változatlan, de a módosított függvényeket használja)
        TEST_APP_NAME_REG = "FOTOapp_TestEntry"
        print(f"Várt parancs: {_get_executable_path_or_command()}")
        print(f"\n1. Ellenőrzés '{TEST_APP_NAME_REG}'...")
        is_enabled = is_autostart_enabled(TEST_APP_NAME_REG)
        print(f"   Autostart engedélyezve? {'Igen' if is_enabled else 'Nem'}")
        print(f"\n2. Engedélyezés '{TEST_APP_NAME_REG}'...")
        success_enable = enable_autostart(TEST_APP_NAME_REG)
        print(f"   Engedélyezés sikeres? {'Igen' if success_enable else 'Nem'}")
        print(f"\n3. Ellenőrzés újra '{TEST_APP_NAME_REG}'...")
        is_enabled = is_autostart_enabled(TEST_APP_NAME_REG)
        print(f"   Autostart engedélyezve? {'Igen' if is_enabled else 'Nem'}")
        print(f"\n4. Letiltás '{TEST_APP_NAME_REG}'...")
        success_disable = disable_autostart(TEST_APP_NAME_REG)
        print(f"   Letiltás sikeres? {'Igen' if success_disable else 'Nem'}")
        print(f"\n5. Ellenőrzés utoljára '{TEST_APP_NAME_REG}'...")
        is_enabled = is_autostart_enabled(TEST_APP_NAME_REG)
        print(f"   Autostart engedélyezve? {'Igen' if is_enabled else 'Nem'}")
        print("\n--- Teszt vége ---")
    else:
        print("Autostart manager teszt csak Windows-on futtatható.")
