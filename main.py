# main.py

import sys
import os
from PySide6.QtCore import QCoreApplication
from PySide6.QtWidgets import QApplication
# --- ÚJ IMPORT ---
from PySide6.QtNetwork import QLocalSocket, QLocalServer
# -----------------
import logging

try:
    from gui.main_window import MainWindow
except ImportError as e:
    print(f"Hiba: Nem sikerült importálni a MainWindow-t a gui.main_window-ból.")
    print(f"Részletek: {e}")
    sys.exit(1)

ORG_NAME = "MyCompanyOrName"
APP_NAME = "FOTOapp"
# --- ÚJ: Egyedi szervernév a példánykezeléshez ---
# Fontos, hogy ez egyedi legyen a rendszeren.
# Az APP_NAME-ből és esetleg egy UUID-ből vagy fix stringből generálhatjuk.
SERVER_NAME = f"{APP_NAME}_InstanceServer_UniqueId12345"
# -------------------------------------------------

def main():
    logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(name)s - %(levelname)s - %(message)s')
    
    QCoreApplication.setOrganizationName(ORG_NAME)
    QCoreApplication.setApplicationName(APP_NAME)

    app = QApplication(sys.argv)
    app.setQuitOnLastWindowClosed(False)

    # --- EGYPÉLDÁNYOSÍTÁS ELLENŐRZÉS KEZDETE ---
    socket = QLocalSocket()
    socket.connectToServer(SERVER_NAME)

    # Próbálunk csatlakozni a szerverhez egy rövid időkorláttal
    if socket.waitForConnected(500): # 500 ms timeout
        logging.info("Már fut egy FOTOapp példány. Jelet küldünk neki az előtérbe hozáshoz.")
        # Küldünk egy üzenetet a már futó példánynak
        socket.write("show_yourself\n".encode('utf-8'))
        socket.waitForBytesWritten(500)
        socket.disconnectFromServer()
        socket.deleteLater() # Socket erőforrás felszabadítása
        sys.exit(0) # Az új példány kilép
    else:
        # Ha nem sikerült csatlakozni, akkor ez az első példány.
        # Töröljük a szerverfájlt, ha esetleg beragadt egy korábbi futásból (csak Unix-szerű rendszereken releváns,
        # Windows-on a named pipe-ok másképp működnek, de a QLocalServer ezt kezeli).
        # A QLocalServer.removeServer() hasznos lehet, ha a listen() hibát ad.
        # Ezt a MainWindow-ban fogjuk kezelni a szerver indításánál.
        logging.info("Nem fut másik példány, vagy a meglévő nem válaszol. Ez az instancia indul el szerverként.")
    # Az socket objektum itt már nem kell, felszabadul.
    # --- EGYPÉLDÁNYOSÍTÁS ELLENŐRZÉS VÉGE ---


    start_hidden = "--start-hidden" in sys.argv
    if start_hidden:
        logging.info("Alkalmazás indítása rejtett módban (tálcára).")

    # Átadjuk a szerver nevét a MainWindow-nak, hogy elindíthassa a szervert, ha ez az első példány
    main_window = MainWindow(start_hidden=start_hidden, server_name=SERVER_NAME)

    if not start_hidden:
        main_window.show()

    sys.exit(app.exec())

if __name__ == "__main__":
    required_dirs = ["gui", "core", "assets"]
    # ... (könyvtár létrehozó rész változatlan) ...
    main()
