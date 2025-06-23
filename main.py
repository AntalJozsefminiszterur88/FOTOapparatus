# main.py

import sys
import os
import logging

# ... (az elején lévő sys.path beállító rész maradhat, nem árt)
try:
    if getattr(sys, 'frozen', False):
        application_path = os.path.dirname(sys.executable)
    else:
        application_path = os.path.dirname(os.path.abspath(__file__))
    if application_path not in sys.path:
        sys.path.insert(0, application_path)
except Exception as e:
    print(f"Hiba a sys.path beállításakor: {e}", file=sys.stderr)

from PySide6.QtCore import QCoreApplication
from PySide6.QtWidgets import QApplication
from PySide6.QtNetwork import QLocalSocket, QLocalServer
from gui.main_window import MainWindow

ORG_NAME = "MyCompanyOrName"
APP_NAME = "FOTOapp"
SERVER_NAME = f"{APP_NAME}_InstanceServer_UniqueId12345"

def main():
    log_dir = os.path.join(os.path.expanduser("~"), "Documents", "UMKGL Solutions", "FOTOapp", "logs")
    os.makedirs(log_dir, exist_ok=True)
    log_file_path = os.path.join(log_dir, "fotoapp.log")
    
    logging.basicConfig(
        level=logging.INFO,
        format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
        handlers=[
            logging.FileHandler(log_file_path, encoding='utf-8'),
            logging.StreamHandler(sys.stdout)
        ]
    )
    
    QCoreApplication.setOrganizationName(ORG_NAME)
    QCoreApplication.setApplicationName(APP_NAME)

    app = QApplication(sys.argv)
    
    # --- A STÍLUS KIKÉNYSZERÍTÉSE ---
    app.setStyle("windowsvista")
    # ---------------------------------

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
    main()
