import os

from PySide6.QtCore import Qt, QStandardPaths
from PySide6.QtNetwork import QLocalServer
from PySide6.QtWidgets import (
    QApplication,
    QHBoxLayout,
    QLabel,
    QMainWindow,
    QPushButton,
    QVBoxLayout,
    QWidget,
)

from core import photo_taker


class MainWindow(QMainWindow):
    """Minimal főablak Discord stílusban."""

    def __init__(
        self,
        start_hidden: bool = False,
        server_name: str | None = None,
        parent: QWidget | None = None,
    ) -> None:
        """Create the main window.

        Parameters
        ----------
        start_hidden:
            Whether the window should start hidden. The main module is
            responsible for not calling :func:`show` when this flag is
            ``True``; it is accepted here to keep the constructor signature in
            sync with that expectation.
        server_name:
            Name of the :class:`QLocalServer` used for single instance
            detection. When provided a server is created that listens for
            ``show_yourself`` messages to restore the window.
        parent:
            Optional parent widget.
        """

        super().__init__(parent)
        self.setWindowTitle("FOTO-Apparátus")
        self.resize(800, 600)

        central = QWidget(self)
        self.setCentralWidget(central)

        main_layout = QHBoxLayout(central)
        main_layout.setContentsMargins(0, 0, 0, 0)

        # Bal oldali menüsáv
        self.sidebar = QWidget()
        self.sidebar.setObjectName("sidebar")
        sidebar_layout = QVBoxLayout(self.sidebar)
        sidebar_layout.setContentsMargins(0, 0, 0, 0)
        sidebar_layout.setSpacing(0)

        btn_home = QPushButton("Kezdőlap")
        btn_settings = QPushButton("Beállítások")
        btn_photo = QPushButton("Fotó készítése")
        sidebar_layout.addWidget(btn_home)
        sidebar_layout.addWidget(btn_settings)
        sidebar_layout.addWidget(btn_photo)
        sidebar_layout.addStretch()

        # Tartalom terület
        self.content = QLabel("Üdv a megújult felületen!")
        self.content.setAlignment(Qt.AlignmentFlag.AlignCenter)

        main_layout.addWidget(self.sidebar)
        main_layout.addWidget(self.content, 1)

        self._apply_style()

        btn_photo.clicked.connect(self._take_photo)

        # Server for single-instance handling
        self._server: QLocalServer | None = None
        if server_name:
            self._server = QLocalServer(self)
            # In case a stale server exists from a crash, remove it first
            if not self._server.listen(server_name):
                QLocalServer.removeServer(server_name)
                self._server.listen(server_name)
            self._server.newConnection.connect(self._handle_connection)

    def _apply_style(self) -> None:
        self.setStyleSheet(
            """
            QMainWindow { background-color: #36393f; color: #ffffff; }
            QWidget#sidebar { background-color: #2f3136; }
            QPushButton {
                background-color: #2f3136;
                color: #dcddde;
                border: none;
                padding: 10px;
                text-align: left;
            }
            QPushButton:hover { background-color: #40444b; }
            """
        )

    def _take_photo(self) -> None:
        save_dir = QStandardPaths.writableLocation(
            QStandardPaths.StandardLocation.PicturesLocation
        )
        if not save_dir:
            save_dir = os.path.expanduser("~")
        path = photo_taker.take_photo(save_dir)
        if path:
            self.content.setText(f"Fotó elmentve:\n{path}")
        else:
            self.content.setText("Hiba történt a fotó készítése közben.")

    def _handle_connection(self) -> None:
        """Handle a message from a secondary instance.

        When another process connects and sends ``show_yourself`` the main
        window is shown and activated.
        """

        if not self._server:
            return

        socket = self._server.nextPendingConnection()
        if not socket:
            return

        def read_and_show() -> None:
            data = bytes(socket.readAll()).decode("utf-8").strip()
            if data == "show_yourself":
                self.show()
                self.raise_()
                self.activateWindow()
            socket.disconnectFromServer()

        socket.readyRead.connect(read_and_show)


if __name__ == "__main__":
    app = QApplication([])
    w = MainWindow()
    w.show()
    app.exec()
