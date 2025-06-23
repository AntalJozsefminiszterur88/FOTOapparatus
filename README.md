# FOTOapparatus

Ez a projekt egy egyszeru Python alkalmazas, amellyel kepernyokepeket vagy webkameras fenykepet keszithetsz. A grafikus feluletet a PySide6 biztosítja.

## Fo funkciok

- Teljes kepernyo vagy egyedi terulet rogzitese
- Programablak kepernyokepe (Windows rendszeren)
- Idopont alapjan automatizalt kepernyokep keszites APScheduler segitsegevel
- Datszalag megjelenitese a kepernyokepeken
- Beallitasok mentese a felhasznalo Dokumentumok mappajaba
- Windows inditasakor automatikus futtatas (registry alapjan)
- Talcaikon, rejtett inditasi lehetoseg

## Futtatas

1. Telepitsd a fuggosegeket a `requirements.txt` alapjan:

```bash
pip install -r requirements.txt
```

2. Inditsd el a fo alkalmazast:

```bash
python main.py
```

A `start.bat` egy egyszeru Windows parancsfajl a fejlesztoi inditashoz. A `build_exe.bat` a PyInstaller alapjan keszit futtathato EXE-t. A script a `--console` opcióval építi a programot, így indításkor megjelenik a konzol ablak, ami segíti a hibák felderítését.

## Mappak

- `core/` – logika (kepernyokep keszites, utemezo, konfiguracio)
- `gui/` – PySide6 felulet komponensek
- `assets/` – ikonok es egyeb statikus allomanyok

## Konfiguracio

A beallitasokat a `ConfigManager` kezeli, amely alapertelmezetten a felhasznalo Dokumentumok/UMKGL Solutions/FOTOapp mappaban tarolja a `fotoapp_config.json` fajlt. A kepernyokepek a Képek/FOTOapp_Screenshots mappaban jonnek letre.

## Rendszerkovetelmenyek

- Python 3.11 vagy ujabb
- Windows rendszeren a program funkcioinak teljes koru hasznalatahoz szukseg van a `pywin32` csomagra.

