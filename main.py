# main.py

import sys
import os
import logging

# --- ÚJ, JAVÍTOTT RÉSZ KEZDETE ---
# Ez a blokk biztosítja, hogy a program (.py-ként és .exe-ként futtatva is)
# megtalálja a saját moduljait (core, gui).
# A sys.path-hoz hozzáadja a program gyökérmappáját.
try:
    if getattr(sys, 'frozen', False):
        # Ha exe-ként fut (PyInstaller)
        application_path = os.path.dirname(sys.executable)
    else:
        # Ha .py-ként fut
        application_path = os.path.dirname(os.path.abspath(__file__))
    
    # Csak akkor adjuk hozzá, ha még nincs benne a keresési útvonalakban
    if application_path not in sys.path:
        sys.path.insert(0, application_path)
except Exception as e:
    # Ha valamiért hiba történne, kiírjuk és megpróbálunk tovább futni
    print(f"Hiba a sys.path beállításakor: {e}", file=sys.stderr)
# --- ÚJ, JAVÍTOTT RÉSZ VÉGE ---


try:
    from PySide6.QtCore import QCoreApplication
    from PySide6.QtWidgets import QApplication
    from PySide6.QtNetwork import QLocalSocket, QLocalServer
except ImportError as exc:
    print(
        "HIBA: A PySide6 csomag nem található. "
        "Telepítse a következő paranccsal: pip install PySide6",
        file=sys.stderr,
    )
    raise SystemExit(1) from exc

try:
    from gui.main_window import MainWindow
except ImportError as e:
    print(f"Hiba: Nem sikerült importálni a MainWindow-t a gui.main_window-ból.")
    print(f"Részletek: {e}")
    # Adjunk egy tippet a felhasználónak a sys.path-ról
    print("\nPython keresési útvonalai (sys.path):")
    for p in sys.path:
        print(f"- {p}")
    sys.exit(1)

ORG_NAME = "MyCompanyOrName"
APP_NAME = "FOTOapp"
SERVER_NAME = f"{APP_NAME}_InstanceServer_UniqueId12345"

def main():
    # Logging beállítása: egy fájlba is menthetjük a logokat, ami EXE esetén nagyon hasznos
    log_dir = os.path.join(os.path.expanduser("~"), "Documents", "UMKGL Solutions", "FOTOapp", "logs")
    os.makedirs(log_dir, exist_ok=True)
    log_file_path = os.path.join(log_dir, "fotoapp.log")
    
    logging.basicConfig(
        level=logging.INFO,
        format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
        handlers=[
            logging.FileHandler(log_file_path, encoding='utf-8'),
            logging.StreamHandler(sys.stdout) # Hogy a konzolon is lássuk
        ]
    )
    
    QCoreApplication.setOrganizationName(ORG_NAME)
    QCoreApplication.setApplicationName(APP_NAME)

    app = QApplication(sys.argv)
    app.setQuitOnLastWindowClosed(False)

    socket = QLocalSocket()
    socket.connectToServer(SERVER_NAME)

    if socket.waitForConnected(500):
        logging.info("Már fut egy FOTOapp példány. Jelet küldünk neki az előtérbe hozáshoz.")
        socket.write("show_yourself\n".encode('utf-8'))
        socket.waitForBytesWritten(500)
        socket.disconnectFromServer()
        socket.deleteLater()
        sys.exit(0)
    else:
        logging.info("Nem fut másik példány, vagy a meglévő nem válaszol. Ez az instancia indul el szerverként.")

    start_hidden = "--start-hidden" in sys.argv
    if start_hidden:
        logging.info("Alkalmazás indítása rejtett módban (tálcára).")

    main_window = MainWindow(start_hidden=start_hidden, server_name=SERVER_NAME)

    if not start_hidden:
        main_window.show()

    sys.exit(app.exec())

if __name__ == "__main__":
    # A könyvtár létrehozó rész itt már nem szükséges, mert a ConfigManager és a logging is kezeli
    main()
