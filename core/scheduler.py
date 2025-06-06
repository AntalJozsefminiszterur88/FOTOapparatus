# core/scheduler.py

import logging
from datetime import datetime

# APScheduler importok
from apscheduler.schedulers.background import BackgroundScheduler
from apscheduler.triggers.cron import CronTrigger
from apscheduler.jobstores.base import JobLookupError

# Saját modulok importálása
# Figyelem a relatív importra, ha csomagként használjuk
try:
    from .screenshot_taker import take_screenshot
    # ConfigManager itt technikailag nem kell, azt a MainWindow példányosítja
    # és a beállításokat átadja a schedulernek, vagy a scheduler kap egy referenciát rá.
    # Egyszerűbb, ha a MainWindow tölti be a configot és adja át az adatokat.
    # De a rugalmasság kedvéért kaphat egy config_load_func-ot.
except ImportError:
    # Ha önállóan futtatjuk teszteléshez
    from screenshot_taker import take_screenshot

# PySide6 import a QRect-hez (ha a terület konverziót itt végezzük)
from PySide6.QtCore import QRect

# Logging beállítása
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(name)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

class Scheduler:
    """Kezeli a képernyőképek időzített készítését."""

    # Napok magyar rövidítésének megfeleltetése az APScheduler CronTrigger formátumához (angol 3 betűs)
    DAY_MAP = {
        "H": "mon", "K": "tue", "Sze": "wed", "Cs": "thu",
        "P": "fri", "Szo": "sat", "V": "sun"
    }

    def __init__(self):
        """Inicializálja az ütemezőt."""
        # BackgroundScheduler: külön szálon fut, nem blokkolja a fő szálat
        # daemon=True: a szál automatikusan leáll, ha a fő program kilép
        self.scheduler = BackgroundScheduler(daemon=True, timezone='Europe/Budapest')
        self.current_settings = None # Itt tároljuk az aktuális beállításokat
        logger.info("Scheduler inicializálva (Timezone: Europe/Budapest).")

    def _schedule_jobs(self):
        """
        Beállítja az időzítési feladatokat az aktuálisan tárolt beállítások alapján.
        A meglévő feladatokat először eltávolítja.
        """
        if self.current_settings is None:
            logger.warning("Nincsenek beállítások betöltve, nem lehet feladatokat ütemezni.")
            return

        # Összes korábbi feladat eltávolítása
        try:
            self.scheduler.remove_all_jobs()
            logger.info("Minden korábbi időzítési feladat eltávolítva.")
        except Exception as e:
            logger.error(f"Hiba a korábbi feladatok eltávolítása közben: {e}")
            # Folytatjuk az újak hozzáadásával

        schedules = self.current_settings.get("schedules", [])
        save_path = self.current_settings.get("save_path", ".") # Alapértelmezett: aktuális mappa
        mode = self.current_settings.get("screenshot_mode", "fullscreen")
        custom_area_dict = self.current_settings.get("custom_area", None)

        logger.info(f"Feladatok ütemezése {len(schedules)} szabály alapján. Mentési hely: {save_path}, Mód: {mode}")

        area_arg = None
        if mode == "custom" and custom_area_dict:
            try:
                area_arg = QRect(custom_area_dict['x'], custom_area_dict['y'],
                                 custom_area_dict['width'], custom_area_dict['height'])
                if not area_arg.isValid():
                     logger.warning(f"Érvénytelen 'custom_area' a beállításokban: {custom_area_dict}, teljes képernyő lesz használva.")
                     area_arg = None # Visszaállunk None-ra, ha érvénytelen
            except (KeyError, TypeError) as e:
                logger.error(f"Hiba a 'custom_area' feldolgozásakor: {e}. Teljes képernyő lesz használva.")
                area_arg = None

        for i, schedule_item in enumerate(schedules):
            try:
                time_str = schedule_item.get("time")
                days_list = schedule_item.get("days", [])

                if not time_str or not days_list:
                    logger.warning(f"Hiányos ütemezési szabály kihagyva: {schedule_item}")
                    continue

                # Idő feldolgozása
                hour, minute = map(int, time_str.split(':'))

                # Napok feldolgozása APScheduler formátumra (pl. "mon,tue,wed")
                mapped_days = [self.DAY_MAP[day] for day in days_list if day in self.DAY_MAP]
                if not mapped_days:
                     logger.warning(f"Nincsenek érvényes napok az ütemezési szabályban: {schedule_item}")
                     continue
                days_str = ",".join(mapped_days)

                # Cron trigger létrehozása
                trigger = CronTrigger(day_of_week=days_str, hour=hour, minute=minute)

                # Feladat hozzáadása az ütemezőhöz
                job_id = f"screenshot_job_{i}"
                self.scheduler.add_job(
                    take_screenshot,
                    trigger=trigger,
                    args=[save_path, "scheduled_screenshot", area_arg, True],  # area_arg itt QRect vagy None
                    id=job_id,
                    name=f"Screenshot at {time_str} on {days_str}",
                    replace_existing=True  # Felülírja, ha véletlenül létezne már ilyen ID
                )
                logger.info(f"Feladat hozzáadva (ID: {job_id}): Idő={time_str}, Napok={days_str}, Terület={area_arg if area_arg else 'Fullscreen'}")

            except (ValueError, KeyError, Exception) as e:
                logger.error(f"Hiba az ütemezési szabály feldolgozása közben: {schedule_item} - Hiba: {e}")

        # Ütemezett feladatok kiírása (opcionális)
        try:
             self.scheduler.print_jobs()
        except Exception as e:
             logger.warning(f"Nem sikerült kiírni az ütemezett feladatokat: {e}")


    def start(self, settings):
        """
        Elindítja az ütemezőt a megadott beállításokkal.

        Args:
            settings (dict): Az alkalmazás beállításai, amiket a ConfigManager betöltött.
        """
        if self.scheduler.running:
            logger.warning("Az ütemező már fut.")
            # Lehet, hogy csak újra kellene tölteni a jobokat? Vagy hiba?
            # Egyelőre csak figyelmeztetünk. A biztonság kedvéért leállítjuk és újraindítjuk.
            self.stop()

        logger.info("Ütemező indítása...")
        self.current_settings = settings # Elmentjük a kapott beállításokat
        self._schedule_jobs() # Betöltjük és ütemezzük a feladatokat

        try:
            self.scheduler.start()
            logger.info("Ütemező sikeresen elindítva.")
        except Exception as e:
            logger.error(f"Hiba az ütemező indításakor: {e}")


    def stop(self):
        """Leállítja az ütemezőt."""
        if self.scheduler.running:
            logger.info("Ütemező leállítása...")
            try:
                self.scheduler.shutdown() # Graceful shutdown
                logger.info("Ütemező sikeresen leállítva.")
            except Exception as e:
                logger.error(f"Hiba az ütemező leállításakor: {e}")
        else:
            logger.info("Az ütemező nem futott, nincs mit leállítani.")

    def reload_jobs(self, settings):
        """
        Újratölti az időzítési feladatokat a megadott (frissített) beállítások alapján.
        Az ütemezőnek futnia kell ehhez.

        Args:
            settings (dict): A frissített alkalmazás beállítások.
        """
        if not self.scheduler.running:
             logger.warning("Az ütemező nem fut, a feladatok újratöltése nem lehetséges. Indítsa el először.")
             # Opcionálisan: elindíthatnánk itt? self.start(settings)
             return

        logger.info("Időzítési feladatok újratöltése...")
        self.current_settings = settings # Frissítjük a tárolt beállításokat
        self._schedule_jobs() # Ez eltávolítja a régieket és hozzáadja az újakat


# --- Tesztelési rész ---
if __name__ == "__main__":
    import time
    import os
    from PySide6.QtCore import QStandardPaths # Csak a teszt mentési helyhez

    logger.info("\n--- Scheduler Teszt ---")

    # Hova mentsünk a tesztben?
    try:
        pictures_location = QStandardPaths.writableLocation(QStandardPaths.StandardLocation.PicturesLocation)
        if not pictures_location:
             pictures_location = os.path.join(os.path.expanduser("~"), "fotoapp_scheduler_tests")
        else:
             pictures_location = os.path.join(pictures_location, "FotoApp_Scheduler_Tests")
        logger.info(f"Teszt mentési hely: {pictures_location}")
    except Exception as e:
        logger.error(f"Hiba a teszt mentési hely meghatározásakor: {e}")
        pictures_location = "fotoapp_scheduler_tests"


    # Teszt beállítások
    test_settings = {
        "save_path": pictures_location,
        "screenshot_mode": "fullscreen",
        "custom_area": {"x": 10, "y": 10, "width": 200, "height": 150}, # Ezt csak akkor használja, ha a mód "custom"
        "schedules": [
            # Ez a feladat le fog futni a következő percben, ha az páros
            {"time": datetime.now().strftime("%H:%M"), "days": ["H", "K", "Sze", "Cs", "P", "Szo", "V"]},
             # Ez a feladat csak hétfőn 08:00-kor futna le
            {"time": "08:00", "days": ["H"]},
            # Ez egy hibás/hiányos szabály
            {"time": "10:00", "days": []}
        ]
    }
    # Dinamikusan beállítjuk az első szabály idejét a következő percre
    now = datetime.now()
    next_minute_time = (now + timedelta(minutes=1)).strftime("%H:%M")
    test_settings["schedules"][0]["time"] = next_minute_time
    logger.info(f"Az első teszt feladat ideje beállítva a következő percre: {next_minute_time}")


    scheduler_instance = Scheduler()

    # Indítás a teszt beállításokkal
    scheduler_instance.start(test_settings)

    # Várjunk egy kicsit, hogy lássuk, lefut-e a feladat
    logger.info("Várakozás a feladatok lefutására (kb. 70 másodperc)... Nyomjon Ctrl+C-t a kilépéshez.")
    try:
        # Várjunk eleget, hogy a következő perc biztosan elérkezzen
        time.sleep(70)
    except KeyboardInterrupt:
        logger.info("Felhasználói megszakítás (Ctrl+C).")

    # Teszteljük az újratöltést (módváltással)
    logger.info("\nBeállítások módosítása (mód: custom) és újratöltés...")
    test_settings["screenshot_mode"] = "custom"
    # Adjunk hozzá egy új szabályt is
    later_time = (datetime.now() + timedelta(minutes=2)).strftime("%H:%M")
    test_settings["schedules"].append({"time": later_time, "days": ["H", "K", "Sze", "Cs", "P", "Szo", "V"]})
    logger.info(f"Új feladat ideje: {later_time}")

    scheduler_instance.reload_jobs(test_settings)

    logger.info("Újabb várakozás (kb. 70 másodperc)... Nyomjon Ctrl+C-t a kilépéshez.")
    try:
        # Várjunk eleget az új feladatnak is
        time.sleep(70)
    except KeyboardInterrupt:
        logger.info("Felhasználói megszakítás (Ctrl+C).")


    # Leállítás
    scheduler_instance.stop()

    logger.info("\n--- Scheduler Teszt Vége ---")
