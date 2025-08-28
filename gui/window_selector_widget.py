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
import re

_import_error = False
gw = None

if platform.system() == "Windows":
    try:
        import pygetwindow as gw  # type: ignore
    except Exception:
        _import_error = True


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
            if _import_error:
                message = "Hiányzik a pygetwindow modul"
            else:
                message = "Nem támogatott rendszer"
            self.info_label = QLabel(message)
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

    def _simplify_title(self, title: str) -> str:
        parts = re.split(r" - |\|", title)
        return parts[-1].strip()

    def refresh_list(self):
        if self.combo is None or gw is None:
            return
        current = self.get_selected_title()
        self.combo.blockSignals(True)
        self.combo.clear()
        self.combo.addItem("", "")
        try:
            windows = [w for w in gw.getAllWindows() if w.title.strip()]
            for w in windows:
                simple = self._simplify_title(w.title)
                self.combo.addItem(w.title, simple)
        except Exception:
            pass
        self.combo.blockSignals(False)
        if current:
            self.set_selected_title(current)

    def _emit_change(self, _):
        self.selection_changed.emit(self.get_selected_title())

    def get_selected_title(self):
        if self.combo is None:
            return ""
        data = self.combo.currentData()
        if data:
            return data
        return self.combo.currentText()

    def set_selected_title(self, title):
        if self.combo is None:
            return
        index = self.combo.findData(title)
        if index == -1:
            index = self.combo.findText(title)
        self.combo.setCurrentIndex(index if index >= 0 else 0)
