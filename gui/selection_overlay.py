# gui/selection_overlay.py

from PySide6.QtWidgets import QWidget, QApplication
from PySide6.QtGui import QPainter, QPen, QBrush, QColor, QScreen, QPainterPath, QFont
from PySide6.QtCore import Signal, Slot, Qt, QRect, QPoint, QRectF

class SelectionOverlay(QWidget):
    """
    Teljes képernyős, félig átlátszó widget a terület kijelöléséhez.
    """
    area_selected = Signal(QRect)
    selection_canceled = Signal()

    def __init__(self, parent=None):
        super().__init__(parent)
        # ... (a __init__ többi része változatlan, ahogy legutóbb küldtem) ...
        # Ablak attribútumok és flag-ek beállítása
        self.setWindowFlags(
            Qt.WindowType.FramelessWindowHint |
            Qt.WindowType.WindowStaysOnTopHint |
            Qt.WindowType.Tool
        )
        self.setAttribute(Qt.WidgetAttribute.WA_TranslucentBackground, True)
        self.setAttribute(Qt.WidgetAttribute.WA_DeleteOnClose, True)
        self.setMouseTracking(True) # Ez fontos az egérmozgás eseményekhez gombnyomás nélkül is
        self.setCursor(Qt.CursorShape.CrossCursor)

        screen = QApplication.primaryScreen()
        if screen:
             screen_geometry = screen.geometry()
             self.setGeometry(screen_geometry)
        else:
             print("Hiba: Nem sikerült lekérni az elsődleges képernyőt!")
             self.resize(800, 600)

        self.selecting = False
        self.start_point = QPoint()
        self.end_point = QPoint()
        self.current_rect = QRect()

        self.overlay_color = QColor(0, 0, 0, 100)
        self.selection_pen = QPen(QColor(255, 0, 0, 200), 1.5, Qt.PenStyle.SolidLine)
        self.selection_brush = QBrush(Qt.BrushStyle.NoBrush)
        self.text_pen = QPen(Qt.GlobalColor.white)
        self.text_font = QFont("Arial", 10)
        print("DEBUG: SelectionOverlay initialized and cursor set to Cross.") # DEBUG

    def mousePressEvent(self, event):
        """Egérgomb lenyomásának kezelése."""
        print(f"DEBUG: mousePressEvent at {event.position().toPoint()}, button: {event.button()}") # DEBUG
        if event.button() == Qt.MouseButton.LeftButton:
            self.selecting = True
            self.start_point = event.position().toPoint()
            self.end_point = self.start_point
            self.current_rect = QRect(self.start_point, self.end_point).normalized()
            print(f"DEBUG: mousePress - selecting: {self.selecting}, start: {self.start_point}, current_rect: {self.current_rect}") # DEBUG
            self.update()
        elif event.button() == Qt.MouseButton.RightButton:
            print("DEBUG: mousePress - Right button, canceling selection.") # DEBUG
            self.selection_canceled.emit()
            self.close()

    def mouseMoveEvent(self, event):
        """Egér mozgatásának kezelése."""
        # Egyszerűsített debug print, hogy lássuk, egyáltalán lefut-e
        # print(f"DEBUG: mouseMoveEvent at {event.position().toPoint()}, selecting: {self.selecting}") # Túl sok kimenetet adhat

        if self.selecting:
            self.end_point = event.position().toPoint()
            self.current_rect = QRect(self.start_point, self.end_point).normalized()
            # Csak akkor írjunk, ha tényleg változik a rect, hogy ne legyen spam
            if not hasattr(self, '_last_debug_move_rect') or self._last_debug_move_rect != self.current_rect:
                print(f"DEBUG: mouseMove (selecting) - current_rect: {self.current_rect}") # DEBUG
                self._last_debug_move_rect = self.current_rect
            self.update()
        # else:
            # Ez akkor fut, ha az egér mozog az ablak felett, de nincs lenyomva a gomb
            # print(f"DEBUG: mouseMove (NOT selecting) at {event.position().toPoint()}")


    def mouseReleaseEvent(self, event):
        """Egérgomb felengedésének kezelése."""
        print(f"DEBUG: mouseReleaseEvent at {event.position().toPoint()}, button: {event.button()}") # DEBUG
        if event.button() == Qt.MouseButton.LeftButton and self.selecting:
            self.selecting = False
            final_rect = QRect(self.start_point, self.end_point).normalized()
            print(f"DEBUG: mouseRelease - final_rect: {final_rect}, selecting was True, now: {self.selecting}") # DEBUG

            if final_rect.width() > 0 and final_rect.height() > 0:
                print(f"Kijelölt terület (valódi): {final_rect}")
                self.area_selected.emit(final_rect)
            else:
                print("Kijelölés megszakítva (nincs méret, valószínűleg csak klikk volt).")
                self.selection_canceled.emit()
            self.close()
        else:
            print(f"DEBUG: mouseRelease - not left button or was not selecting. Button: {event.button()}, Was Selecting: {self.selecting}") # DEBUG
            # Ha nem selecting módban engedték fel, akkor is zárjuk be, ha pl. jobb klikk volt, ami már megtörtént
            # De ha bal klikk, de selecting false, az fura -> nem zárjuk be ilyenkor automatikusan
            if event.button() == Qt.MouseButton.RightButton: # Ezt a press már kezeli
                 pass
            elif not self.selecting: # Ha nem voltunk selecting módban és nem bal gombot engedtek fel (vagy már false a selecting)
                 print("DEBUG: mouseRelease - nem volt aktív kijelölés, overlay nem záródik be ettől az eseménytől.")
                 # self.selection_canceled.emit() # Lehet, hogy itt is kellene egy cancel?
                 # self.close()

    def keyPressEvent(self, event):
        # ... (változatlan) ...
        print(f"DEBUG: keyPressEvent: {event.key()}") # DEBUG
        if event.key() == Qt.Key.Key_Escape:
            print("DEBUG: keyPress - ESC, canceling selection.") # DEBUG
            self.selection_canceled.emit()
            self.close()
        else:
            super().keyPressEvent(event)

    # A paintEvent maradjon az előző diagnosztikai (kék téglalapos)
    def paintEvent(self, event):
        """A widget újrarajzolása (Diagnosztikai verzió)."""
        painter = QPainter(self)
        painter.setRenderHint(QPainter.RenderHint.Antialiasing)
        painter.fillRect(self.rect(), self.overlay_color)
        if self.current_rect.isValid() and self.selecting:
            debug_selection_fill = QColor(0, 0, 255, 80)
            painter.fillRect(self.current_rect, debug_selection_fill)
            painter.setPen(self.selection_pen)
            painter.setBrush(self.selection_brush)
            painter.drawRect(self.current_rect)
            painter.setPen(self.text_pen)
            painter.setFont(self.text_font)
            text_pos = self.current_rect.bottomRight() + QPoint(5, 15)
            widget_rect = self.rect()
            text_width_estimate = 100
            if text_pos.x() + text_width_estimate > widget_rect.right():
                 text_pos.setX(widget_rect.right() - text_width_estimate - 5)
            if text_pos.y() > widget_rect.bottom():
                 text_pos.setY(self.current_rect.top() - 5)
            elif text_pos.y() < widget_rect.top() + 15:
                 text_pos.setY(self.current_rect.bottom() + 15)
            dimension_text = f"{self.current_rect.width()} x {self.current_rect.height()}"
            painter.drawText(text_pos, dimension_text)

    # ... (az __main__ rész változatlan) ...


# Egyszerű teszteléshez
if __name__ == '__main__':
    import sys

    # Kell egy QApplication a teszthez is
    app = QApplication.instance()
    if not app:
         app = QApplication(sys.argv)

    overlay = SelectionOverlay()

    # Jelek összekötése a teszteléshez
    overlay.area_selected.connect(lambda rect: print(f"TESZT: Terület kiválasztva: {rect}"))
    overlay.selection_canceled.connect(lambda: print("TESZT: Kijelölés megszakítva"))

    overlay.show() # Megjelenítjük az overlay-t

    # Mivel ez egy eseményvezérelt ablak, el kell indítani az eseményciklust a teszthez
    sys.exit(app.exec())
