from PySide6.QtCore import Qt
from PySide6.QtWidgets import (
    QApplication,
    QHBoxLayout,
    QLabel,
    QMainWindow,
    QPushButton,
    QVBoxLayout,
    QWidget,
)


class MainWindow(QMainWindow):
    """Minimal főablak Discord stílusban."""

    def __init__(self, parent: QWidget | None = None) -> None:
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
        sidebar_layout.addWidget(btn_home)
        sidebar_layout.addWidget(btn_settings)
        sidebar_layout.addStretch()

        # Tartalom terület
        self.content = QLabel("Üdv a megújult felületen!")
        self.content.setAlignment(Qt.AlignmentFlag.AlignCenter)

        main_layout.addWidget(self.sidebar)
        main_layout.addWidget(self.content, 1)

        self._apply_style()

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


if __name__ == "__main__":
    app = QApplication([])
    w = MainWindow()
    w.show()
    app.exec()
