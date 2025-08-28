from PySide6.QtWidgets import (
    QDialog,
    QVBoxLayout,
    QHBoxLayout,
    QCheckBox,
    QSpinBox,
    QLabel,
    QPushButton
)

from .window_selector_widget import WindowSelectorWidget


class DiscordSettingsDialog(QDialog):
    """Egyszerű beállító ablak a Discord módhoz."""

    def __init__(self, settings: dict | None = None, parent=None):
        super().__init__(parent)
        self.setWindowTitle("Discord mód beállításai")
        self.setModal(True)

        settings = settings or {}

        layout = QVBoxLayout(self)

        self.window_selector = WindowSelectorWidget()
        layout.addWidget(self.window_selector)
        self.window_selector.set_selected_title(settings.get("window_title", ""))

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
        self.ok_button = QPushButton("OK")
        cancel_button = QPushButton("Mégse")
        self.ok_button.clicked.connect(self.accept)
        cancel_button.clicked.connect(self.reject)
        button_layout.addWidget(self.ok_button)
        button_layout.addWidget(cancel_button)
        layout.addLayout(button_layout)

        self.ok_button.setEnabled(bool(self.window_selector.get_selected_title().strip()))
        self.window_selector.selection_changed.connect(self._update_ok)

    def _update_ok(self, text: str):
        self.ok_button.setEnabled(bool(text.strip()))

    def get_settings(self) -> dict:
        return {
            "stay_foreground": self.stay_foreground_cb.isChecked(),
            "use_hotkey": self.use_hotkey_cb.isChecked(),
            "hotkey_number": self.hotkey_spin.value(),
            "window_title": self.window_selector.get_selected_title(),
        }
