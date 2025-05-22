# gui/screenshot_size_widget.py

from PySide6.QtWidgets import (
    QWidget,
    QVBoxLayout,
    QHBoxLayout,
    QGroupBox,
    QRadioButton,
    QPushButton,
    QLabel
)
from PySide6.QtCore import Signal, Slot, QRect

class ScreenshotSizeWidget(QWidget):
    """
    Widget a képernyőkép méretének kiválasztásához (Teljes képernyő / Egyéni).
    """
    mode_changed = Signal(str)
    select_area_requested = Signal()

    def __init__(self, parent=None):
        super().__init__(parent)

        self._current_mode = "fullscreen" # Kezdeti belső állapot
        self._custom_rect = QRect(0, 0, 100, 100)

        main_layout = QVBoxLayout(self)
        main_layout.setContentsMargins(0, 0, 0, 0)

        group_box = QGroupBox("Screenshot Mérete")
        main_layout.addWidget(group_box)

        group_layout = QVBoxLayout(group_box)

        self.radio_fullscreen = QRadioButton("Teljes képernyő")
        self.radio_custom = QRadioButton("Egyéni terület")
        group_layout.addWidget(self.radio_fullscreen)
        group_layout.addWidget(self.radio_custom)

        custom_area_layout = QHBoxLayout()
        self.btn_select_area = QPushButton("Terület Kijelölése...")
        self.lbl_custom_area_info = QLabel("Méret: -")
        
        custom_area_layout.addWidget(self.btn_select_area)
        custom_area_layout.addWidget(self.lbl_custom_area_info)
        custom_area_layout.addStretch()

        custom_area_container = QWidget()
        custom_area_container_layout = QVBoxLayout(custom_area_container)
        custom_area_container_layout.setContentsMargins(20, 0, 0, 0)
        custom_area_container_layout.addLayout(custom_area_layout)
        group_layout.addWidget(custom_area_container)

        # --- JAVÍTÁS: Mindkét rádiógomb 'toggled' jelét bekötjük ---
        self.radio_fullscreen.toggled.connect(self._on_mode_toggled)
        self.radio_custom.toggled.connect(self._on_mode_toggled) # Ez is legyen bekötve!

        self.btn_select_area.clicked.connect(self._on_select_area_clicked)

        # Kezdeti állapot beállítása (ez kiváltja a _on_mode_toggled-et)
        self.radio_fullscreen.setChecked(True)
        # A gomb és címke állapotát az _on_mode_toggled fogja beállítani
        # a setChecked(True) hívás után.
        self._update_custom_area_label_text()


    @Slot(bool)
    def _on_mode_toggled(self, checked):
        """
        Akkor hívódik meg, ha valamelyik rádiógomb állapota megváltozik.
        A 'checked' paraméter a jelet küldő rádiógomb ÚJ állapotát jelzi.
        """
        # Csak akkor foglalkozunk vele, ha a jelet küldő gomb BE lett kapcsolva.
        # (Mivel a másik gomb kikapcsolásakor is lefutna 'checked=False' értékkel)
        sender = self.sender()
        if not checked and (sender == self.radio_fullscreen or sender == self.radio_custom) :
            return # Ha egy gomb KIkapcsolódott, nem csinálunk semmit itt

        new_effective_mode = None
        if self.radio_custom.isChecked():
            new_effective_mode = "custom"
        elif self.radio_fullscreen.isChecked():
            new_effective_mode = "fullscreen"
        
        if new_effective_mode is None: # Nem szabadna előfordulnia
            return

        is_custom_mode_active = (new_effective_mode == "custom")
        self.btn_select_area.setEnabled(is_custom_mode_active)
        self.lbl_custom_area_info.setEnabled(is_custom_mode_active)
        
        # print(f"DEBUG ScreenshotSizeWidget: _on_mode_toggled - new_effective_mode: {new_effective_mode}, btn_enabled: {is_custom_mode_active}")


        # Csak akkor frissítjük a belső _current_mode-ot és küldünk jelet,
        # ha a ténylegesen kiválasztott mód megváltozott.
        if new_effective_mode != self._current_mode:
            self._current_mode = new_effective_mode
            print(f"Mód váltás (belső): {self._current_mode}") # DEBUG
            self.mode_changed.emit(self._current_mode)

    @Slot()
    def _on_select_area_clicked(self):
        print("Terület kijelölése gomb megnyomva.")
        self.select_area_requested.emit()

    def _update_custom_area_label_text(self):
        if self._custom_rect.isValid():
             text = f"Méret: {self._custom_rect.width()} x {self._custom_rect.height()} (X:{self._custom_rect.x()}, Y:{self._custom_rect.y()})"
        else:
             text = "Méret: -" # Vagy a mentett érték, ha van
        self.lbl_custom_area_info.setText(text)

    def set_mode(self, mode, custom_rect_dict=None):
        """
        Beállítja a widget módját (és az egyéni területet, ha releváns).
        Ezt a metódust a MainWindow hívja a beállítások betöltésekor.
        """
        print(f"ScreenshotSizeWidget: külső set_mode hívás: mód={mode}") # DEBUG

        # A setChecked kiváltja a 'toggled' jelet, ami futtatja az _on_mode_toggled-et.
        # Az _on_mode_toggled fogja beállítani a gomb engedélyezettségét és a _current_mode-ot.
        if mode == "custom":
            self.radio_custom.setChecked(True)
        else: # "fullscreen" vagy bármi más -> fullscreen
            self.radio_fullscreen.setChecked(True)
        
        # Az egyéni terület adatainak frissítése, ha van és custom módban vagyunk
        if mode == "custom":
            if custom_rect_dict:
                try:
                    self._custom_rect = QRect(custom_rect_dict['x'], custom_rect_dict['y'],
                                              custom_rect_dict['width'], custom_rect_dict['height'])
                except (KeyError, TypeError) as e:
                     print(f"Hiba: Érvénytelen custom_rect_dict a set_mode-ban: {e}")
                     # Hiba esetén az alapértelmezett _custom_rect marad
            #else:
                # Ha custom_rect_dict None, de a mód "custom", akkor a meglévő _custom_rect-et használjuk
                # vagy egy alapértelmezettet, ha az is érvénytelen lenne.
        
        self._update_custom_area_label_text() # Frissítjük a címkét

    def get_mode(self):
        return self._current_mode

    def get_custom_rect(self):
        return self._custom_rect

    def update_custom_area(self, rect):
        if isinstance(rect, QRect) and rect.isValid():
            self._custom_rect = rect
            self._update_custom_area_label_text()
            print(f"Egyéni terület frissítve a ScreenshotSizeWidget-ben: {rect}")
        else:
            print(f"Hiba: Érvénytelen QRect objektum az update_custom_area-ban: {rect}")
