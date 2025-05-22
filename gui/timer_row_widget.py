# gui/timer_row_widget.py

from PySide6.QtWidgets import (
    QWidget,
    QHBoxLayout,
    QTimeEdit,
    QCheckBox,
    QPushButton,
    QLabel,
    QSpacerItem,
    QSizePolicy
)
from PySide6.QtCore import Signal, Slot, QTime

class TimerRowWidget(QWidget):
    """
    Egy sort reprezentáló widget az időzítő beállításához (időpont, napok).
    """
    # Jelek (Signals)
    # Jelez, ha a felhasználó ennek a sornak az eltávolítását kéri. Átadja önmagát.
    remove_requested = Signal(QWidget)
    # Jelez, ha a sor beállításai (idő vagy napok) megváltoztak.
    settings_changed = Signal()

    # Napok rövidítései és sorrendje
    DAYS = ["H", "K", "Sze", "Cs", "P", "Szo", "V"]

    def __init__(self, initial_time=None, initial_days=None, parent=None):
        """
        Inicializálja az időzítő sor widgetet.

        Args:
            initial_time (QTime, optional): Kezdeti időpont. Ha None, az aktuális időt használja.
            initial_days (list, optional): Kezdetben kiválasztott napok listája (pl. ["H", "Sze", "P"]).
                                           Ha None, egy nap sincs kiválasztva.
            parent (QWidget, optional): Szülő widget.
        """
        super().__init__(parent)

        # Fő vízszintes elrendezés
        self.main_layout = QHBoxLayout(self)
        self.main_layout.setContentsMargins(0, 5, 0, 5) # Kis függőleges margó

        # Időpont választó
        self.time_edit = QTimeEdit()
        self.time_edit.setDisplayFormat("HH:mm")
        if initial_time and isinstance(initial_time, QTime):
            self.time_edit.setTime(initial_time)
        else:
            self.time_edit.setTime(QTime.currentTime()) # Alapértelmezett: aktuális idő
        self.main_layout.addWidget(self.time_edit)

        # Elválasztó
        self.main_layout.addSpacing(15)

        # Nap választó jelölőnégyzetek
        self.day_checkboxes = {} # Szótár a checkboxok tárolására
        for day_abbr in self.DAYS:
            checkbox = QCheckBox(day_abbr)
            self.day_checkboxes[day_abbr] = checkbox
            self.main_layout.addWidget(checkbox)
            # Kezdeti állapot beállítása
            if initial_days and day_abbr in initial_days:
                 checkbox.setChecked(True)
            # Jelzés összekötése
            checkbox.stateChanged.connect(self._on_settings_changed)

        # Térkitöltő, hogy a törlés gomb jobbra tolódjon
        self.main_layout.addStretch(1)

        # Törlés gomb
        self.remove_button = QPushButton("-")
        self.remove_button.setFixedSize(25, 25) # Kis, négyzetes gomb
        self.remove_button.setToolTip("Sor eltávolítása")
        self.main_layout.addWidget(self.remove_button)

        # Jelek összekötése
        self.time_edit.timeChanged.connect(self._on_settings_changed)
        self.remove_button.clicked.connect(self._on_remove_clicked)

        self.setLayout(self.main_layout)

    @Slot()
    def _on_settings_changed(self):
        """Akkor hívódik meg, ha az idő vagy egy nap checkbox megváltozik."""
        # print(f"Sor beállításai megváltoztak: {self.get_settings()}") # Debug
        self.settings_changed.emit() # Jelezzük a szülőnek a változást

    @Slot()
    def _on_remove_clicked(self):
        """Akkor hívódik meg, ha a törlés gombra kattintanak."""
        self.remove_requested.emit(self) # Elküldjük magát a widgetet azonosítóként

    # --- Publikus metódusok ---

    def get_settings(self):
        """
        Visszaadja a sor aktuális beállításait egy szótárban.

        Returns:
            dict: A beállításokat tartalmazó szótár, pl.:
                  {'time': '14:35', 'days': ['H', 'Sze', 'P']}
        """
        selected_time = self.time_edit.time().toString("HH:mm")
        selected_days = [day for day, checkbox in self.day_checkboxes.items() if checkbox.isChecked()]
        return {"time": selected_time, "days": selected_days}

    def set_settings(self, settings_dict):
        """
        Beállítja a sor widget állapotát egy beállítás szótár alapján.

        Args:
            settings_dict (dict): A beállításokat tartalmazó szótár,
                                  pl. {'time': '09:00', 'days': ['Szo', 'V']}
        """
        try:
            # Idő beállítása
            time_str = settings_dict.get("time", "00:00")
            qtime = QTime.fromString(time_str, "HH:mm")
            if qtime.isValid():
                self.time_edit.setTime(qtime)
            else:
                print(f"Figyelmeztetés: Érvénytelen időformátum a set_settings-ben: {time_str}")
                self.time_edit.setTime(QTime(0, 0)) # Alapértelmezettre állítás

            # Napok beállítása
            selected_days = settings_dict.get("days", [])
            for day_abbr, checkbox in self.day_checkboxes.items():
                checkbox.setChecked(day_abbr in selected_days)

            # A beállítások programatikus megváltoztatása is kiváltja a changed jeleket,
            # de ez általában nem probléma. Ha mégis, akkor a jelek átmeneti blokkolása/feloldása
            # (self.time_edit.blockSignals(True/False)) lehet egy megoldás.

        except Exception as e:
            print(f"Hiba a TimerRowWidget beállításakor: {e}")


# Egyszerű teszteléshez
if __name__ == '__main__':
    from PySide6.QtWidgets import QApplication
    import sys

    app = QApplication(sys.argv)

    # Teszt adatok
    test_settings1 = {'time': '10:30', 'days': ['H', 'K', 'P']}
    test_settings2 = {'time': '22:00', 'days': ['Szo', 'V']}

    # Widgetek létrehozása
    widget1 = TimerRowWidget() # Alapértelmezett
    widget2 = TimerRowWidget(initial_time=QTime(8, 0), initial_days=["Sze"])
    widget3 = TimerRowWidget()
    widget3.set_settings(test_settings1)
    widget4 = TimerRowWidget()
    widget4.set_settings(test_settings2)

    # Visszaolvasás tesztelése
    print("Widget 1 beállítások:", widget1.get_settings())
    print("Widget 2 beállítások:", widget2.get_settings())
    print("Widget 3 beállítások:", widget3.get_settings())
    print("Widget 4 beállítások:", widget4.get_settings())

    # Események szimulálása (manuális)
    widget1.remove_requested.connect(lambda w: print(f"Eltávolítási kérés érkezett: {w}"))
    widget1.settings_changed.connect(lambda: print(f"Widget 1 beállításai megváltoztak: {widget1.get_settings()}"))

    # Layout a teszt widgetek megjelenítéséhez
    test_container = QWidget()
    layout = QVBoxLayout(test_container)
    layout.addWidget(QLabel("Teszt Időzítő Sorok:"))
    layout.addWidget(widget1)
    layout.addWidget(widget2)
    layout.addWidget(widget3)
    layout.addWidget(widget4)
    test_container.show()

    sys.exit(app.exec())
