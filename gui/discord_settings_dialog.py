from PySide6.QtWidgets import (
    QDialog,
    QVBoxLayout,
    QHBoxLayout,
    QCheckBox,
    QSpinBox,
    QLabel,
    QPushButton
)


class DiscordSettingsDialog(QDialog):
    """Egyszerű beállító ablak a Discord módhoz."""

    def __init__(self, settings: dict | None = None, parent=None):
        super().__init__(parent)
        self.setWindowTitle("Discord mód beállításai")
        self.setModal(True)

        settings = settings or {}

        layout = QVBoxLayout(self)

        self.stay_foreground_cb = QCheckBox("Discord maradjon az előtérben")
        self.stay_foreground_cb.setChecked(settings.get("stay_foreground", False))
        layout.addWidget(self.stay_foreground_cb)

        self.use_hotkey_cb = QCheckBox("Ctrl + szám lenyomása fotózás előtt")
        self.use_hotkey_cb.setChecked(settings.get("use_hotkey", False))
        layout.addWidget(self.use_hotkey_cb)

        hotkey_layout = QHBoxLayout()
        hotkey_layout.addWidget(QLabel("Szám:"))
        self.hotkey_spin = QSpinBox()
        self.hotkey_spin.setRange(0, 9)
        self.hotkey_spin.setValue(settings.get("hotkey_number", 1))
        hotkey_layout.addWidget(self.hotkey_spin)
        hotkey_layout.addStretch()
        layout.addLayout(hotkey_layout)

        button_layout = QHBoxLayout()
        button_layout.addStretch()
        ok_button = QPushButton("OK")
        cancel_button = QPushButton("Mégse")
        ok_button.clicked.connect(self.accept)
        cancel_button.clicked.connect(self.reject)
        button_layout.addWidget(ok_button)
        button_layout.addWidget(cancel_button)
        layout.addLayout(button_layout)

    def get_settings(self) -> dict:
        return {
            "stay_foreground": self.stay_foreground_cb.isChecked(),
            "use_hotkey": self.use_hotkey_cb.isChecked(),
            "hotkey_number": self.hotkey_spin.value(),
        }
