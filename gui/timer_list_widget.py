# gui/timer_list_widget.py

from PySide6.QtWidgets import (
    QWidget,
    QVBoxLayout,
    QHBoxLayout,
    QPushButton,
    QScrollArea,
    QSizePolicy,
    QLabel,
    QSpacerItem
)
from PySide6.QtCore import Signal, Slot, Qt
from .timer_row_widget import TimerRowWidget # Importáljuk az egy sort kezelő widgetet

class TimerListWidget(QWidget):
    """
    Widget az időzítő sorok listájának kezeléséhez (+/- gombokkal).
    """
    # Jelzés, ha a lista megváltozik (sor hozzáadva vagy eltávolítva)
    list_changed = Signal()

    def __init__(self, parent=None):
        """Inicializálja a widgetet."""
        super().__init__(parent)

        # Fő függőleges elrendezés
        self.main_layout = QVBoxLayout(self)
        self.main_layout.setContentsMargins(0, 0, 0, 0)

        # --- Fejléc (Címke és Hozzáadás gomb) ---
        header_layout = QHBoxLayout()
        header_label = QLabel("Időzítők")
        header_layout.addWidget(header_label)
        header_layout.addStretch(1) # Helykitöltő
        self.add_button = QPushButton("+ Új időzítő")
        self.add_button.setToolTip("Új időzítési szabály hozzáadása")
        header_layout.addWidget(self.add_button)
        self.main_layout.addLayout(header_layout)

        # --- Gördíthető terület a soroknak ---
        self.scroll_area = QScrollArea()
        self.scroll_area.setWidgetResizable(True) # Fontos, hogy a belső widget méreteződjön
        self.scroll_area.setHorizontalScrollBarPolicy(Qt.ScrollBarPolicy.ScrollBarAlwaysOff)
        self.scroll_area.setVerticalScrollBarPolicy(Qt.ScrollBarPolicy.ScrollBarAsNeeded)
        self.scroll_area.setSizePolicy(QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Expanding)

        # Konténer widget a görgethető területen belül
        self.scroll_content_widget = QWidget()
        # Elrendezés a konténer widgeten belül, ide kerülnek a sorok
        self.rows_layout = QVBoxLayout(self.scroll_content_widget)
        self.rows_layout.setContentsMargins(5, 5, 5, 5)
        self.rows_layout.setSpacing(5)
        self.rows_layout.addStretch(1) # Biztosítja, hogy a sorok felülről töltődjenek

        self.scroll_content_widget.setLayout(self.rows_layout)
        self.scroll_area.setWidget(self.scroll_content_widget) # Beállítjuk a görgethető tartalmat

        self.main_layout.addWidget(self.scroll_area) # Hozzáadjuk a görgethető területet a fő layout-hoz

        # Belső lista a TimerRowWidget példányok nyomon követésére (opcionális, de hasznos)
        self.timer_rows = []

        # Jel összekötése
        self.add_button.clicked.connect(self._add_row)

    @Slot()
    def _add_row(self, settings=None):
        """
        Hozzáad egy új időzítő sort a listához.

        Args:
            settings (dict, optional): A létrehozandó sor kezdeti beállításai.
                                       Ha None, alapértelmezett értékekkel jön létre.
        """
        print(f"Új sor hozzáadása, beállítások: {settings}") # Debug
        new_row = TimerRowWidget() # Létrehozzuk az új sort
        if settings and isinstance(settings, dict):
            new_row.set_settings(settings) # Beállítjuk a kapott adatokkal

        # Összekötjük az új sor 'remove_requested' jelét a mi '_remove_row' slotunkkal
        new_row.remove_requested.connect(self._remove_row)
        # Összekötjük az új sor 'settings_changed' jelét, hogy továbbítsuk a változást
        new_row.settings_changed.connect(self.list_changed)

        # Beszúrjuk az új sort az elrendezésbe, de a 'stretch' elé
        insert_index = self.rows_layout.count() - 1 # Az utolsó elem (stretch) előtti index
        self.rows_layout.insertWidget(insert_index, new_row)

        self.timer_rows.append(new_row) # Hozzáadjuk a belső listánkhoz is

        self.list_changed.emit() # Jelezzük, hogy a lista megváltozott

    @Slot(QWidget)
    def _remove_row(self, row_widget):
        """
        Eltávolít egy megadott időzítő sort a listából.

        Args:
            row_widget (TimerRowWidget): Az eltávolítandó widget példány.
        """
        if row_widget in self.timer_rows:
            print(f"Sor eltávolítása: {row_widget}") # Debug
            # Jelek szétkapcsolása (biztonsági okokból)
            try:
                row_widget.remove_requested.disconnect(self._remove_row)
                row_widget.settings_changed.disconnect(self.list_changed)
            except RuntimeError as e:
                 # Előfordulhat, ha a jel már nem volt összekötve, nem feltétlen hiba.
                 print(f"Figyelmeztetés a jel szétkapcsolásakor: {e}")


            # Eltávolítás az elrendezésből
            self.rows_layout.removeWidget(row_widget)
            # Eltávolítás a belső listából
            self.timer_rows.remove(row_widget)
            # Widget biztonságos törlése (az eseményciklus végén)
            row_widget.deleteLater()

            self.list_changed.emit() # Jelezzük, hogy a lista megváltozott
        else:
            print(f"Hiba: Eltávolítandó sor nem található a listában: {row_widget}")

    def clear_rows(self):
        """Eltávolítja az összes időzítő sort a listából."""
        print("Összes sor törlése...") # Debug
        # Hátulról kezdjük a törlést, hogy ne zavarjuk meg az indexelést
        while self.timer_rows:
            row_to_remove = self.timer_rows[-1] # Utolsó elem
            self._remove_row(row_to_remove)
        # Biztosítjuk, hogy a stretch megmaradjon, ha véletlenül az is törlődne
        if self.rows_layout.count() == 0:
             self.rows_layout.addStretch(1)


    # --- Publikus metódusok ---

    def get_all_settings(self):
        """
        Visszaadja az összes sor beállítását egy listában.

        Returns:
            list[dict]: Lista szótárakkal, ahol minden szótár egy sor beállításait tartalmazza.
        """
        all_settings = [row.get_settings() for row in self.timer_rows]
        # print(f"Összes beállítás lekérdezve: {all_settings}") # Debug
        return all_settings

    def set_all_settings(self, settings_list):
        """
        Beállítja az időzítő sorokat a megadott lista alapján. A meglévő sorokat törli.

        Args:
            settings_list (list[dict]): A beállítandó sorok listája.
        """
        self.clear_rows() # Először töröljük a meglévő sorokat

        print(f"Sorok beállítása a listából: {settings_list}") # Debug
        if isinstance(settings_list, list):
            for settings_dict in settings_list:
                self._add_row(settings=settings_dict) # Hozzáadjuk az új sorokat a kapott adatokkal
        else:
            print("Hiba: set_all_settings érvénytelen listát kapott.")


# Egyszerű teszteléshez
if __name__ == '__main__':
    from PySide6.QtWidgets import QApplication
    import sys

    app = QApplication(sys.argv)

    test_widget = TimerListWidget()

    # Teszt adatok
    initial_data = [
        {'time': '09:00', 'days': ['H', 'K', 'Sze', 'Cs', 'P']},
        {'time': '18:30', 'days': ['Szo', 'V']}
    ]

    # Beállítások beállítása
    print("\nKezdeti adatok beállítása:")
    test_widget.set_all_settings(initial_data)

    # Visszaolvasás
    print("\nJelenlegi beállítások:")
    print(test_widget.get_all_settings())

    # Csatlakozás a jelzéshez
    test_widget.list_changed.connect(lambda: print("--> TimerListWidget: Lista megváltozott!"))

    test_widget.resize(400, 300) # Méret a jobb láthatóságért
    test_widget.show()

    # Új sor hozzáadása gombnyomásra (szimulálva)
    # print("\nÚj sor hozzáadása programból...")
    # test_widget._add_row({'time': '12:00', 'days': ['H']})
    # print(test_widget.get_all_settings())


    sys.exit(app.exec())
