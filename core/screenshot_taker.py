# core/screenshot_taker.py

import os
import sys
from datetime import datetime

from PySide6.QtWidgets import QApplication
from PySide6.QtGui import QScreen, QPainter, QFont
from PySide6.QtCore import QRect, Qt, QStandardPaths
# QStandardPaths itt nem közvetlenül használt, de a tesztblokkban igen

# A QApplication példányt a main.py hozza létre.
# Ennek a modulnak arra kell támaszkodnia.

def take_screenshot(save_directory, filename_prefix="Kép", area=None, add_timestamp=False, timestamp_position="top-left"):
    """
    Képernyőképet készít a megadott területről vagy a teljes elsődleges képernyőről.

    Args:
        save_directory (str): A könyvtár, ahova a képet menteni kell.
        filename_prefix (str, optional): A fájlnév előtagja. Alapértelmezett: "Kép".
        area (QRect, optional): A rögzítendő terület. Ha None, a teljes elsődleges
                                képernyőt rögzíti.
        add_timestamp (bool, optional): Ha True, a képre ráírja az aktuális dátumot.
        timestamp_position (str, optional): A felirat helye ('top-left', 'top-right',
                                'bottom-left', 'bottom-right').

    Returns:
        str | None: A mentett kép teljes elérési útja siker esetén, None hiba esetén.
    """
    app = QApplication.instance()
    if not app:
        print("HIBA: QApplication.instance() nem található a screenshot_taker modulban. "
              "Az alkalmazást a main.py-ból kell indítani, amely létrehozza a QApplication-t.", file=sys.stderr)
        return None

    screen = QApplication.primaryScreen()
    if not screen:
        print("HIBA: Nem sikerült lekérni az elsődleges képernyőt a screenshot_taker modulban.", file=sys.stderr)
        return None

    capture_rect = QRect()
    if area and isinstance(area, QRect) and area.isValid():
        capture_rect = area
    else:
        capture_rect = screen.geometry()

    if capture_rect.isEmpty():
         print(f"HIBA: Érvénytelen rögzítési terület a screenshot_taker-ben: {capture_rect}", file=sys.stderr)
         return None

    try:
        pixmap = screen.grabWindow(0, capture_rect.x(), capture_rect.y(),
                                   capture_rect.width(), capture_rect.height())

        if pixmap.isNull():
            print("HIBA: Nem sikerült rögzíteni a képernyőt (grabWindow üres pixmap-et adott vissza).", file=sys.stderr)
            return None

        os.makedirs(save_directory, exist_ok=True)

        if add_timestamp:
            painter = QPainter(pixmap)
            painter.setPen(Qt.GlobalColor.white)
            painter.setFont(QFont("Arial", 14))
            timestamp_text = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            alignment = Qt.AlignmentFlag.AlignLeft | Qt.AlignmentFlag.AlignTop
            if timestamp_position == "top-right":
                alignment = Qt.AlignmentFlag.AlignRight | Qt.AlignmentFlag.AlignTop
            elif timestamp_position == "bottom-left":
                alignment = Qt.AlignmentFlag.AlignLeft | Qt.AlignmentFlag.AlignBottom
            elif timestamp_position == "bottom-right":
                alignment = Qt.AlignmentFlag.AlignRight | Qt.AlignmentFlag.AlignBottom
            painter.drawText(
                pixmap.rect().adjusted(10, 10, -10, -10),
                alignment,
                timestamp_text,
            )
            painter.end()

        # --- MÓDOSÍTOTT FÁJLNÉV FORMÁTUM ---
        # Kért formátum: Kép_YYYY_MM_DD_HH-MM
        # A kettőspont használata Windows rendszeren problémát okozhat,
        # ezért az időrészeket kötőjellel választjuk el.
        timestamp_for_filename = datetime.now().strftime("%Y_%m_%d_%H-%M")
        
        filename = f"{filename_prefix}_{timestamp_for_filename}.png"
        # Ha a prefixet el szeretnéd hagyni, akkor:
        # filename = f"{timestamp_for_filename}.png"
        # ------------------------------------

        save_path = os.path.join(save_directory, filename)

        if pixmap.save(save_path, "png"):
            # Sikeres mentés esetén ez az üzenet továbbra is hasznos lehet
            print(f"Képernyőkép sikeresen elmentve: {save_path}") 
            return save_path
        else:
            print(f"HIBA: Nem sikerült elmenteni a képernyőképet ide: {save_path}", file=sys.stderr)
            return None

    except OSError as e:
        print(f"HIBA a könyvtár létrehozásakor ({save_directory}): {e}", file=sys.stderr)
        return None
    except Exception as e:
        print(f"Váratlan HIBA történt a képernyőkép készítésekor: {e}", file=sys.stderr)
        import traceback
        traceback.print_exc(file=sys.stderr) # Részletesebb hiba kiírása
        return None

# --- Tesztelési rész ---
if __name__ == "__main__":
    print("\n--- Képernyőkép Készítő Modul Teszt ---")
    print("Figyelem: Ezt a modult önállóan futtatva a take_screenshot funkció nem fog működni,")
    print("mivel QApplication példányra van szüksége, amit a fő alkalmazás (main.py) hoz létre.")
    
    # Példa, hogyan nézne ki egy generált fájlnév (QApplication nélkül ez a rész nem fut le helyesen)
    try:
        # Csak a fájlnév generálási logika tesztelése (nem készít képet)
        prefix_test = "test_prefix"
        ts_test = datetime.now().strftime("%Y_%m_%d_%H-%M")
        fn_test = f"{prefix_test}_{ts_test}.png"
        print(f"\nPélda generált fájlnév ({prefix_test} prefixszel): {fn_test}")

        # pictures_location = QStandardPaths.writableLocation(QStandardPaths.StandardLocation.PicturesLocation)
        # if pictures_location:
        #      test_dir = os.path.join(pictures_location, "FotoApp_ScreenshotTaker_Tests")
        #      print(f"Tesztkönyvtár lenne (ha működne QApplication-nel): {test_dir}")
        #      # Itt lehetne egy dummy QApplication-t létrehozni csak a teszthez, ha nagyon szükséges lenne
        #      # pl. app_test = QApplication.instance(); if not app_test: app_test = QApplication([])
        #      # de a modul fő célja nem az önálló futtatás.
        # else:
        #      print("Nem sikerült lekérni a Képek mappát (QApplication hiányzik?).")
    except Exception as e:
        print(f"Hiba a tesztblokkban (valószínűleg QApplication hiánya miatt): {e}")

    print("\n--- Teszt vége ---")
