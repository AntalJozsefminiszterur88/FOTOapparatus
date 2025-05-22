# gui/main_window.py

import sys
import os
import traceback
import logging

try:
    from PySide6.QtCore import Slot, QRect, Qt, QCoreApplication, QStandardPaths, QRectF
    from PySide6.QtWidgets import (QMainWindow, QWidget, QVBoxLayout, QHBoxLayout, QPushButton,
                                 QFileDialog, QMessageBox, QLabel, QCheckBox, QSpacerItem, QSizePolicy,
                                 QApplication, QSystemTrayIcon, QMenu, QStyle)
    from PySide6.QtGui import QIcon, QAction
    from PySide6.QtNetwork import QLocalServer, QLocalSocket

    from .screenshot_size_widget import ScreenshotSizeWidget
    from .timer_list_widget import TimerListWidget
    from .selection_overlay import SelectionOverlay
    from core.config_manager import ConfigManager
    from core.scheduler import Scheduler
    from core import autostart_manager
    # from PySide6.QtGui import QPainter, QPen, QBrush, QColor, QScreen, QPainterPath, QFont # Már importálva

except ImportError as e:
     print("--- KRITIKUS IMPORTÁLÁSI HIBA (gui/main_window.py) ---", file=sys.stderr)
     print(f"Hibaüzenet: {e}", file=sys.stderr)
     print("\nRészletes hiba:", file=sys.stderr)
     traceback.print_exc(file=sys.stderr)
     try:
         _app_present = QApplication.instance()
         if not _app_present: _app_fallback = QApplication([])
         else: _app_fallback = _app_present
         _error_box = QMessageBox()
         _error_box.setIcon(QMessageBox.Icon.Critical)
         _error_box.setWindowTitle("Indítási hiba")
         _error_box.setText(f"Kritikus importálási hiba:\n{e}\n\nRészletek a konzolon.\nAz alkalmazás bezárul.")
         _error_box.exec()
     except Exception: pass
     sys.exit(1)

logger = logging.getLogger(__name__)

ICON_FILENAME = "camera_icon.ico"
try:
    _current_script_dir = os.path.dirname(os.path.abspath(__file__))
    _project_root_dir = os.path.dirname(_current_script_dir)
    ICON_PATH = os.path.join(_project_root_dir, "assets", ICON_FILENAME)
except NameError:
    ICON_PATH = os.path.join("assets", ICON_FILENAME)


class MainWindow(QMainWindow):
    BASE_WINDOW_TITLE = "FOTOapp"

    def __init__(self, parent=None, start_hidden=False, server_name=None):
        super().__init__(parent)
        logger.info(f"MainWindow inicializálása... Start hidden: {start_hidden}, Server name: {server_name}")
        logger.debug(f"Keresett egyedi ikon útvonal: {ICON_PATH}")

        if os.path.exists(ICON_PATH):
            self.app_icon = QIcon(ICON_PATH)
            if self.app_icon.isNull():
                logger.error(f"Ikonfájl ({ICON_PATH}) létezik, de nem sikerült betölteni. Fallback ikon használata.")
                self.app_icon = QIcon(self.style().standardIcon(QStyle.StandardPixmap.SP_ComputerIcon))
        else:
            logger.warning(f"Ikonfájl nem található: {ICON_PATH}. Fallback ikon használata.")
            self.app_icon = QIcon(self.style().standardIcon(QStyle.StandardPixmap.SP_ComputerIcon))
            if self.app_icon.isNull():
                 logger.error("Standard fallback ikon betöltése sem sikerült. Üres ikon használata.")
                 self.app_icon = QIcon()
        self.setWindowIcon(self.app_icon)

        self.setWindowTitle(self.BASE_WINDOW_TITLE)
        self.resize(550, 500)

        self.config_manager = ConfigManager()
        self.scheduler = Scheduler()
        self.settings = {}
        self.is_dirty = False
        self.selection_overlay = None
        self.local_server = None

        self._load_settings()

        central_widget = QWidget(self)
        self.setCentralWidget(central_widget)
        self.setStyleSheet("QMainWindow { background-color: pink; }")
        self.main_layout = QVBoxLayout(central_widget)
        central_widget.setLayout(self.main_layout)

        self._setup_ui()
        self._connect_signals()
        self._update_ui_from_settings()
        
        self._create_tray_icon()

        if server_name:
            self._start_local_server(server_name)

        try:
             self.scheduler.start(self.settings)
        except Exception as e:
             logger.exception("Hiba a scheduler indításakor:")
             QMessageBox.critical(self, "Scheduler Hiba", f"Nem sikerült elindítani az időzítőt:\n{e}")
        
        if start_hidden and self.tray_icon.isVisible():
            logger.info("Alkalmazás rejtve indul, üzenet a tálcán.")
            self.tray_icon.showMessage(
                "FOTOapp Indult", "Az alkalmazás a háttérben fut.",
                QSystemTrayIcon.MessageIcon.Information, 3000
            )
        
        self.statusBar().showMessage("Alkalmazás betöltve.", 3000)
        logger.info("MainWindow inicializálása befejeződött.")

    def _start_local_server(self, server_name):
        self.local_server = QLocalServer(self)
        self.local_server.newConnection.connect(self._handle_new_instance_connection)
        if not self.local_server.listen(server_name):
            logger.warning(f"Nem sikerült elindítani a helyi szervert '{server_name}' néven.")
            if QLocalServer.removeServer(server_name): 
                logger.info(f"Sikeresen eltávolítva a korábbi szerverfájl ('{server_name}'). Újrapróbálkozás...")
                if not self.local_server.listen(server_name):
                    logger.error(f"Az újbóli szerverindítási kísérlet is sikertelen ('{server_name}').")
                    QMessageBox.warning(self, "Szerver Hiba",f"Nem sikerült elindítani a belső kommunikációs szervert ({server_name}).")
                else:
                    logger.info(f"Helyi szerver sikeresen elindítva '{server_name}' néven a takarítás után.")
            else: 
                logger.error(f"Nem sikerült eltávolítani a korábbi szerverfájlt ('{server_name}'), vagy nem volt mit eltávolítani, és a listen() továbbra is sikertelen.")
                QMessageBox.warning(self, "Szerver Hiba", f"Nem sikerült elindítani a belső kommunikációs szervert ({server_name}).")
        else:
            logger.info(f"Helyi szerver sikeresen elindítva '{server_name}' néven.")

    @Slot()
    def _handle_new_instance_connection(self):
        logger.info("Új példány csatlakozási kísérlete érzékelve a szerveren.")
        socket = self.local_server.nextPendingConnection()
        if socket:
            socket.readyRead.connect(lambda: self._process_instance_message(socket))
            socket.disconnected.connect(socket.deleteLater)
            if not socket.waitForReadyRead(200): 
                logger.warning("Nem érkezett olvasható adat az új példány socketjéről (200ms). Ablak előtérbe hozása.")
                self.show_window_from_tray() 
    
    def _process_instance_message(self, socket):
        if not socket or not socket.isValid() or not socket.canReadLine():
            if socket: socket.disconnectFromServer()
            return
        message = ""
        try:
            line = bytes(socket.readLine()).decode('utf-8').strip()
            message = line
        except Exception as e:
            logger.error(f"Hiba az üzenet olvasása közben az új példánytól: {e}")
            socket.disconnectFromServer()
            return
        logger.debug(f"Bejövő üzenet az új példánytól: '{message}'")
        if message == "show_yourself":
            logger.info("Parancs: 'show_yourself'. Ablak előtérbe hozása.")
            self.show_window_from_tray()
        socket.disconnectFromServer()

    def _create_tray_icon(self):
        logger.debug("Tálca ikon létrehozása...")
        self.tray_icon = QSystemTrayIcon(self)
        self.tray_icon.setIcon(self.app_icon) 
        self.tray_icon.setToolTip("FOTOapp - A háttérben fut")
        tray_menu = QMenu(self)
        show_action = QAction("Megnyitás", self)
        show_action.triggered.connect(self.show_window_from_tray)
        tray_menu.addAction(show_action)
        tray_menu.addSeparator()
        quit_action = QAction("Kilépés", self)
        quit_action.triggered.connect(self.quit_application)
        tray_menu.addAction(quit_action)
        self.tray_icon.setContextMenu(tray_menu)
        self.tray_icon.activated.connect(self._handle_tray_icon_activation)
        self.tray_icon.show()
        logger.info("Tálca ikon sikeresen létrehozva és megjelenítve.")

    @Slot(QSystemTrayIcon.ActivationReason)
    def _handle_tray_icon_activation(self, reason):
        if reason == QSystemTrayIcon.ActivationReason.Trigger or \
           reason == QSystemTrayIcon.ActivationReason.DoubleClick:
            logger.debug("Tálca ikon aktiválva. Ablak megjelenítése.")
            self.show_window_from_tray()

    @Slot()
    def show_window_from_tray(self):
        if self.isHidden() or self.isMinimized():
            self.showNormal()
        self.show()
        self.activateWindow()
        self.raise_()

    @Slot()
    def quit_application(self):
        logger.info("Kilépés parancs a tálca menüből.")
        if self.is_dirty:
            reply = QMessageBox.question(
                self, 'Nem mentett változások',
                "Vannak nem mentett beállítások. Szeretné menteni őket kilépés előtt?",
                QMessageBox.StandardButton.Save | QMessageBox.StandardButton.Discard | QMessageBox.StandardButton.Cancel,
                QMessageBox.StandardButton.Cancel )
            if reply == QMessageBox.StandardButton.Save:
                self.save_settings()
                if self.is_dirty: logger.warning("Mentés sikertelen a kilépésnél, kilépés megszakítva."); return
            elif reply == QMessageBox.StandardButton.Cancel: logger.info("Kilépés megszakítva."); return
        logger.info("Felhasználó megerősítette a kilépést / nem voltak nem mentett adatok.")
        self.statusBar().showMessage("Alkalmazás bezárása...", 2000)
        if self.local_server and self.local_server.isListening():
            logger.info("Helyi szerver leállítása kilépéskor..."); self.local_server.close()
        if self.scheduler and self.scheduler.scheduler.running:
            logger.debug("Scheduler leállítása..."); self.scheduler.stop()
        if self.tray_icon: logger.debug("Tálca ikon elrejtése..."); self.tray_icon.hide()
        logger.info("QApplication.quit() hívása."); QApplication.instance().quit()
        
    def _setup_ui(self):
        logger.debug("UI elemek beállítása...")
        self.size_widget = ScreenshotSizeWidget()
        self.main_layout.addWidget(self.size_widget)
        self.timer_list = TimerListWidget()
        self.timer_list.setSizePolicy(QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Expanding)
        self.main_layout.addWidget(self.timer_list)
        bottom_layout = QHBoxLayout()
        bottom_layout.setContentsMargins(5,5,5,5)
        self.folder_button = QPushButton("Mentési Mappa...")
        bottom_layout.addWidget(self.folder_button)
        self.folder_label = QLabel("Mappa: -"); self.folder_label.setSizePolicy(QSizePolicy.Policy.MinimumExpanding, QSizePolicy.Policy.Preferred)
        bottom_layout.addWidget(self.folder_label)
        bottom_layout.addStretch(1)
        self.autostart_checkbox = QCheckBox("Indítás a Windows-zal")
        if autostart_manager._IS_WINDOWS: bottom_layout.addWidget(self.autostart_checkbox)
        else: self.autostart_checkbox.setVisible(False)
        self.save_button = QPushButton("Mentés"); self.save_button.setDefault(True)
        bottom_layout.addWidget(self.save_button)
        self.main_layout.addLayout(bottom_layout)
        logger.debug("_setup_ui Befejeződött.")

    def _connect_signals(self):
        logger.debug("Jel-slot kapcsolatok beállítása...")
        self.size_widget.mode_changed.connect(self._handle_mode_change)
        self.size_widget.select_area_requested.connect(self._start_area_selection)
        self.timer_list.list_changed.connect(self._mark_dirty)
        self.save_button.clicked.connect(self.save_settings)
        self.folder_button.clicked.connect(self.select_save_folder)
        if autostart_manager._IS_WINDOWS and hasattr(self, 'autostart_checkbox') and self.autostart_checkbox:
            try:
                self.autostart_checkbox.stateChanged.connect(self._handle_autostart_change)
                logger.debug("Autostart checkbox 'stateChanged' sikeresen csatlakoztatva.")
            except Exception as e: logger.exception("Hiba az autostart_checkbox csatlakoztatásakor:")
        logger.debug("_connect_signals Befejeződött.")

    def _update_ui_from_settings(self):
        logger.info("Metódus hívás: _update_ui_from_settings. Betöltött self.settings:")
        logger.info(self.settings)
        if not self.settings: logger.warning("Nincsenek beállítások a UI frissítéséhez."); return
        try:
            mode_loaded = self.settings.get("screenshot_mode", "fullscreen")
            custom_area_loaded = self.settings.get("custom_area", {})
            logger.info(f"UI Update -> SizeWidget: mód='{mode_loaded}', terület='{custom_area_loaded}'")
            self.size_widget.set_mode(mode_loaded, custom_area_loaded)
        except Exception as e: logger.exception("Hiba a méret widget UI beállításakor:")
        try:
            schedules_loaded = self.settings.get("schedules", [])
            logger.info(f"UI Update -> TimerList: ütemezések='{schedules_loaded}'")
            self.timer_list.set_all_settings(schedules_loaded)
        except Exception as e: logger.exception("Hiba az időzítő lista UI beállításakor:")
        save_path_loaded = self.settings.get("save_path", "")
        logger.info(f"UI Update -> FolderLabel: mentési útvonal='{save_path_loaded}'")
        self._update_folder_label(save_path_loaded)
        if autostart_manager._IS_WINDOWS and hasattr(self, 'autostart_checkbox') and self.autostart_checkbox:
            autostart_preferred = self.settings.get("autostart_preferred", False)
            logger.info(f"UI Update -> Autostart: JSON preferencia = {autostart_preferred}")
            actual_registry = autostart_manager.is_autostart_enabled(autostart_manager.APP_NAME)
            logger.info(f"Autostart (UI Update): Aktuális Registry állapot = {actual_registry}")
            if autostart_preferred and not actual_registry:
                logger.info("Autostart szink.: Registry engedélyezése...")
                if not autostart_manager.enable_autostart(autostart_manager.APP_NAME): logger.warning("Autostart engedélyezés sikertelen (Registry).")
                else: logger.info("Autostart sikeresen engedélyezve a Registry-ben."); actual_registry = True
            elif not autostart_preferred and actual_registry:
                logger.info("Autostart szinkron.: Registry letiltása...")
                if not autostart_manager.disable_autostart(autostart_manager.APP_NAME): logger.warning("Autostart letiltás sikertelen (Registry).")
                else: logger.info("Autostart sikeresen letiltva a Registry-ben."); actual_registry = False
            logger.info(f"Autostart (UI Update): Checkbox beállítása erre: {actual_registry}")
            self.autostart_checkbox.blockSignals(True)
            self.autostart_checkbox.setChecked(actual_registry)
            self.autostart_checkbox.blockSignals(False)
        self.is_dirty = False; self._update_window_title()
        logger.info("_update_ui_from_settings Befejeződött.")

    def _update_folder_label(self, path):
        if path:
            max_len = 40; display_path = path if len(path) <= max_len else f"...{path[-max_len+3:]}"
            self.folder_label.setText(f"Mappa: {display_path}"); self.folder_label.setToolTip(f"Mentési hely: {path}")
        else: self.folder_label.setText("Mappa: Nincs kiválasztva"); self.folder_label.setToolTip("Nincs kiválasztva")

    @Slot()
    def _mark_dirty(self):
        if not self.is_dirty: logger.debug("Beállítások módosultak (dirty=True)."); self.is_dirty = True; self._update_window_title()

    def _update_window_title(self): title = self.BASE_WINDOW_TITLE; self.setWindowTitle(title + " *" if self.is_dirty else title)

    @Slot(str)
    def _handle_mode_change(self, mode): logger.info(f"Screenshot mód: {mode}"); self._mark_dirty()

    @Slot()
    def _start_area_selection(self):
        logger.debug("Területkijelölés indítása...");
        if not self.selection_overlay: self._show_overlay()
        else: logger.warning("Kijelölő overlay már aktív.")

    def _show_overlay(self):
        try:
            self.selection_overlay = SelectionOverlay()
            self.selection_overlay.area_selected.connect(self._handle_area_selected)
            self.selection_overlay.selection_canceled.connect(self._handle_selection_canceled)
            self.selection_overlay.show()
        except Exception as e: logger.exception("Hiba SelectionOverlay létrehozásakor:"); QMessageBox.critical(self, "Hiba", f"Hiba:\n{e}"); self.selection_overlay = None

    @Slot(QRect)
    def _handle_area_selected(self, rect): logger.info(f"Kiválasztott terület: {rect}"); self.size_widget.update_custom_area(rect); self._mark_dirty(); self._cleanup_overlay()
    @Slot()
    def _handle_selection_canceled(self): logger.debug("Területkijelölés megszakítva."); self._cleanup_overlay()
    def _cleanup_overlay(self): self.selection_overlay = None

    @Slot()
    def select_save_folder(self):
        logger.debug("Mentési mappa választó...")
        current_path = self.settings.get("save_path", QStandardPaths.writableLocation(QStandardPaths.StandardLocation.PicturesLocation) or os.path.expanduser("~"))
        new_path = QFileDialog.getExistingDirectory(self, "Mentési mappa", current_path)
        if new_path and new_path != self.settings.get("save_path"):
            logger.info(f"Új mentési mappa: {new_path}"); self._update_folder_label(new_path)
            self.settings["save_path"] = new_path; self._mark_dirty()

    @Slot(int)
    def _handle_autostart_change(self, state_int):
        logger.info(f"_handle_autostart_change: state_int={state_int}")
        if not autostart_manager._IS_WINDOWS: return
        is_checked = (state_int == Qt.CheckState.Checked.value)
        logger.info(f"Autostart checkbox: {'Bekapcsolva' if is_checked else 'Kikapcsolva'}")
        success = autostart_manager.enable_autostart(autostart_manager.APP_NAME) if is_checked else autostart_manager.disable_autostart(autostart_manager.APP_NAME)
        logger.debug(f"Registry művelet eredménye: {success}")
        if success:
            self.statusBar().showMessage(f"Autostart {'engedélyezve' if is_checked else 'letiltva'}.", 3000)
            if self.settings.get("autostart_preferred") != is_checked:
                self.settings["autostart_preferred"] = is_checked
                logger.info(f"JSON preferencia frissítve: {is_checked}"); self._mark_dirty()
        else:
            QMessageBox.warning(self, "Hiba", "Autostart rendszerbeállítás módosítása sikertelen."); logger.warning("Registry művelet sikertelen, checkbox visszaállítása.")
            self.autostart_checkbox.blockSignals(True)
            self.autostart_checkbox.setChecked(autostart_manager.is_autostart_enabled(autostart_manager.APP_NAME))
            self.autostart_checkbox.blockSignals(False)

    @Slot()
    def save_settings(self):
        logger.info("Beállítások mentése...")
        save_path = self.settings.get("save_path", "")
        if not save_path: QMessageBox.warning(self, "Hiányzó adat", "Nincs mentési mappa!"); return
        
        mode = self.size_widget.get_mode()
        custom_rect_obj = self.size_widget.get_custom_rect()
        
        logger.info(f"Mentéshez használt mód: '{mode}'")
        logger.info(f"Mentéshez custom_rect a size_widget-ből: {custom_rect_obj}, isValid? {custom_rect_obj.isValid()}")

        custom_area_dict_to_save = {}
        if custom_rect_obj.isValid(): # Egy QRect(0,0,0,0) is valid, de a defaultunk (0,0,100,100)
            custom_area_dict_to_save = {
                "x": custom_rect_obj.x(), "y": custom_rect_obj.y(),
                "width": custom_rect_obj.width(), "height": custom_rect_obj.height()
            }
        else: # Ha valamiért érvénytelen lenne a widgetből (nem jellemző)
            default_cfg = self.config_manager.get_default_settings()
            custom_area_dict_to_save = self.settings.get("custom_area", default_cfg["custom_area"])
            logger.warning(f"Érvénytelen custom_rect a widgetből mentéskor. Fallback: {custom_area_dict_to_save}")
        
        logger.info(f"Mentésre kerülő custom_area_dict: {custom_area_dict_to_save}")

        new_settings = {
            "save_path": save_path,
            "screenshot_mode": mode,
            "custom_area": custom_area_dict_to_save,
            "schedules": self.timer_list.get_all_settings(),
            "autostart_preferred": self.settings.get("autostart_preferred", False)
        }
        logger.info(f"Teljes mentendő new_settings: {new_settings}")
        try:
            if self.config_manager.save_settings(new_settings):
                self.settings = new_settings; self.is_dirty = False; self._update_window_title()
                self.statusBar().showMessage("Beállítások sikeresen elmentve.", 3000)
                logger.info("Beállítások mentve, scheduler újratöltése...")
                try: self.scheduler.reload_jobs(self.settings)
                except Exception as e: logger.exception("Hiba scheduler újratöltésekor:"); QMessageBox.critical(self, "Scheduler Hiba", f"{e}")
            else: QMessageBox.critical(self, "Mentési Hiba", "Nem sikerült menteni a konfigurációs fájlba.")
        except Exception as e: logger.exception("Váratlan hiba mentéskor:"); QMessageBox.critical(self, "Mentési Hiba", f"{e}")

    def _load_settings(self):
        logger.debug("Beállítások betöltése...")
        try:
            self.settings = self.config_manager.load_settings()
        except Exception as e:
             logger.exception("Hiba a beállítások betöltésekor:")
             QMessageBox.warning(self, "Figyelmeztetés", f"Hiba: {e}\nAlapértelmezett értékek lesznek használva.")
             self.settings = self.config_manager.get_default_settings()
        logger.info(f"Betöltött beállítások (_load_settings végén): {self.settings}")

    def closeEvent(self, event):
        logger.debug("Bezárási esemény (closeEvent) - Tálcára helyezés.")
        if self.is_dirty:
            reply = QMessageBox.question(self, 'Nem mentett változások', "Mentetlen beállítások. Menti tálcára helyezés előtt?",
                QMessageBox.StandardButton.Save | QMessageBox.StandardButton.Discard | QMessageBox.StandardButton.Cancel, QMessageBox.StandardButton.Cancel)
            if reply == QMessageBox.StandardButton.Save: self.save_settings()
            if self.is_dirty and reply != QMessageBox.StandardButton.Discard : event.ignore(); return # Ha mentés sikertelen vagy cancel
            if reply == QMessageBox.StandardButton.Discard: self.is_dirty = False; self._update_window_title()
            elif reply == QMessageBox.StandardButton.Cancel: event.ignore(); return
        event.ignore(); self.hide()
        self.tray_icon.showMessage("FOTOapp", "Az alkalmazás a háttérben fut.", QSystemTrayIcon.MessageIcon.Information, 2000)
        logger.info("Ablak elrejtve, alkalmazás a tálcán fut tovább.")
