# main.py

import sys
import os
import logging

# Sys.path módosítás a biztonság kedvéért (főleg EXE-hez)
try:
    if getattr(sys, 'frozen', False):
        application_path = os.path.dirname(sys.executable)
    else:
        application_path = os.path.dirname(os.path.abspath(__file__))
    if application_path not in sys.path:
        sys.path.insert(0, application_path)
except Exception:
    pass

from PySide6.QtCore import QCoreApplication
from PySide6.QtWidgets import QApplication
from PySide6.QtNetwork import QLocalSocket, QLocalServer
from discord_gui.main_window import MainWindow

ORG_NAME = "UMKGL Solutions"
APP_NAME = "FOTOapp"
SERVER_NAME = f"{APP_NAME}_InstanceServer_UniqueId12345"

def main():
    # A QApplication példányosítása a legelső dolog!
    # Ez kulcsfontosságú a stílusok helyes betöltéséhez.
    app = QApplication(sys.argv)

    # Logging beállítása CSAK az app létrehozása után
    log_dir = os.path.join(os.path.expanduser("~"), "Documents", ORG_NAME, APP_NAME, "logs")
    os.makedirs(log_dir, exist_ok=True)
    log_file_path = os.path.join(log_dir, "fotoapp.log")
    
    logging.basicConfig(
        level=logging.INFO,
        format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
        handlers=[logging.FileHandler(log_file_path, encoding='utf-8'), logging.StreamHandler(sys.stdout)]
    )
    
    QCoreApplication.setOrganizationName(ORG_NAME)
    QCoreApplication.setApplicationName(APP_NAME)
    app.setQuitOnLastWindowClosed(False)

    # Egypéldányosítás ellenőrzése
    socket = QLocalSocket()
    socket.connectToServer(SERVER_NAME)

    if socket.waitForConnected(500):
        logging.info("Már fut egy FOTOapp példány.")
        socket.write("show_yourself\n".encode('utf-8'))
        socket.waitForBytesWritten(500)
        sys.exit(0)
    
    logging.info("Ez az első példány, szerver indul.")
    
    # Rejtett indítás ellenőrzése
    start_hidden = "--start-hidden" in sys.argv
    if start_hidden:
        logging.info("Indítás rejtett módban.")

    # A főablak létrehozása és futtatás
    main_window = MainWindow(start_hidden=start_hidden, server_name=SERVER_NAME)
    if not start_hidden:
        main_window.show()

    sys.exit(app.exec())

if __name__ == "__main__":
    main()
