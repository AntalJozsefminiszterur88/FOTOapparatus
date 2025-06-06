# gui/timestamp_position_widget.py

from PySide6.QtWidgets import (
    QWidget,
    QVBoxLayout,
    QGroupBox,
    QCheckBox,
    QGridLayout,
    QRadioButton
)
from PySide6.QtCore import Signal, Slot

class TimestampPositionWidget(QWidget):
    """Widget a dátum megjelenítésének pozíciójához és engedélyezéséhez."""

    position_changed = Signal(str)
    include_changed = Signal(bool)

    def __init__(self, parent=None):
        super().__init__(parent)

        main_layout = QVBoxLayout(self)
        main_layout.setContentsMargins(0, 0, 0, 0)

        group_box = QGroupBox("Dátum beállítások")
        main_layout.addWidget(group_box)

        group_layout = QVBoxLayout(group_box)

        self.include_checkbox = QCheckBox("Dátum kiírása")
        group_layout.addWidget(self.include_checkbox)

        grid = QGridLayout()
        self.rb_tl = QRadioButton("Bal felső")
        self.rb_tr = QRadioButton("Jobb felső")
        self.rb_bl = QRadioButton("Bal alsó")
        self.rb_br = QRadioButton("Jobb alsó")
        grid.addWidget(self.rb_tl, 0, 0)
        grid.addWidget(self.rb_tr, 0, 1)
        grid.addWidget(self.rb_bl, 1, 0)
        grid.addWidget(self.rb_br, 1, 1)
        group_layout.addLayout(grid)

        self.rb_tl.setChecked(True)

        self.include_checkbox.toggled.connect(self.include_changed)
        for rb in (self.rb_tl, self.rb_tr, self.rb_bl, self.rb_br):
            rb.toggled.connect(self._on_position_toggled)

    @Slot(bool)
    def _on_position_toggled(self, checked):
        if not checked:
            return
        mapping = {
            self.rb_tl: "top-left",
            self.rb_tr: "top-right",
            self.rb_bl: "bottom-left",
            self.rb_br: "bottom-right",
        }
        self.position_changed.emit(mapping[self.sender()])

    def get_settings(self):
        include = self.include_checkbox.isChecked()
        if self.rb_tr.isChecked():
            pos = "top-right"
        elif self.rb_bl.isChecked():
            pos = "bottom-left"
        elif self.rb_br.isChecked():
            pos = "bottom-right"
        else:
            pos = "top-left"
        return include, pos

    def set_settings(self, include, position):
        self.include_checkbox.blockSignals(True)
        self.include_checkbox.setChecked(include)
        self.include_checkbox.blockSignals(False)

        mapping = {
            "top-left": self.rb_tl,
            "top-right": self.rb_tr,
            "bottom-left": self.rb_bl,
            "bottom-right": self.rb_br,
        }
        rb = mapping.get(position, self.rb_tl)
        for r in mapping.values():
            r.blockSignals(True)
        rb.setChecked(True)
        for r in mapping.values():
            r.blockSignals(False)

