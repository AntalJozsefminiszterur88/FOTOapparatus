# gui/window_selector_widget.py

from PySide6.QtWidgets import (
    QWidget,
    QVBoxLayout,
    QHBoxLayout,
    QGroupBox,
    QComboBox,
    QPushButton,
    QLabel,
)
from PySide6.QtCore import Signal

import platform

try:
    if platform.system() == "Windows":
        import pygetwindow as gw
    else:
        gw = None
except Exception:
    gw = None

class WindowSelectorWidget(QWidget):
    """Widget a futó alkalmazások ablakainak kiválasztásához."""

    selection_changed = Signal(str)

    def __init__(self, parent=None):
        super().__init__(parent)

        main_layout = QVBoxLayout(self)
        main_layout.setContentsMargins(0, 0, 0, 0)

        group_box = QGroupBox("Alkalmazás kiválasztása")
        main_layout.addWidget(group_box)

        group_layout = QVBoxLayout(group_box)

        if gw is None:
            self.info_label = QLabel("Nem támogatott rendszer")
            group_layout.addWidget(self.info_label)
            self.combo = None
            return

        combo_layout = QHBoxLayout()
        self.combo = QComboBox()
        self.refresh_button = QPushButton("Frissítés")
        combo_layout.addWidget(self.combo)
        combo_layout.addWidget(self.refresh_button)
        group_layout.addLayout(combo_layout)

        self.refresh_button.clicked.connect(self.refresh_list)
        self.combo.currentTextChanged.connect(self._emit_change)

        self.refresh_list()

    def refresh_list(self):
        if self.combo is None or gw is None:
            return
        self.combo.blockSignals(True)
        self.combo.clear()
        self.combo.addItem("")  # üres választás
        try:
            titles = [t for t in gw.getAllTitles() if t.strip()]
            for title in titles:
                self.combo.addItem(title)
        except Exception:
            pass
        self.combo.blockSignals(False)

    def _emit_change(self, text):
        self.selection_changed.emit(text)

    def get_selected_title(self):
        if self.combo is None:
            return ""
        return self.combo.currentText()

    def set_selected_title(self, title):
        if self.combo is None:
            return
        index = self.combo.findText(title)
        if index >= 0:
            self.combo.setCurrentIndex(index)
        else:
            self.combo.setCurrentIndex(0)
