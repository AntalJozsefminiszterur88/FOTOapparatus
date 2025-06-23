@echo off
echo FOTOapp EXE keszitese PyInstaller segitsegevel...
echo.
REM Gyozodj meg rola, hogy a PyInstaller telepítve van a Python kornyezetedben:
REM > pip install pyinstaller
echo.
REM Gyozodj meg rola, hogy az "assets" mappa letezik es tartalmazza a "camera_icon.ico" fajlt.
echo.

REM Korabbi build maradvanyok torlese (ha vannak)
echo Korabbi build maradvanyok torlese...
IF EXIST build RMDIR /S /Q build
IF EXIST dist RMDIR /S /Q dist
IF EXIST FOTOapp.spec DEL FOTOapp.spec /Q
echo.

echo PyInstaller inditasa...
echo Ez eltarthat egy ideig, kulonosen a --onefile opcioval.
echo.

REM === Fo PyInstaller parancs ===
pyinstaller ^
    --name FOTOapp ^
    --noconfirm ^
    --onefile ^
    --console ^
    --icon="assets/camera_icon.ico" ^
    --add-data "assets;assets" ^
    main.py

REM === MEGJEGYZES A POTENCIALISAN SZUKSEGES TOVABBI OPCIOKROL ===
REM Ha az .exe inditasakor "No module named ..." vagy mas futasi hibat kapsz,
REM akkor lehet, hogy a --hidden-import vagy --collect-data opciokat kell hasznalnod.
REM Ezeket a fenti 'pyinstaller' parancsblokkba kellene beszurni, pl. a main.py elé,
REM mindegyik sort '^' jellel zárva, kivéve az utolsó opciót a main.py előtt.
REM
REM Pelda rejtett importokra (vedd ki a REM-et a sor elejerol, ha szukseges):
REM     --hidden-import "apscheduler.triggers.cron" ^
REM     --hidden-import "apscheduler.schedulers.background" ^
REM     --hidden-import "PySide6.QtSvg" ^
REM     --hidden-import "PySide6.QtXml" ^
REM     --hidden-import "pkg_resources.py2_warn" ^
REM     --hidden-import "win32timezone" ^
REM
REM Pelda adatgyujtesre Qt pluginokhoz:
REM     --collect-data "PySide6.Qt.plugins.platforms" ^
REM === MEGJEGYZES VEGE ===

echo.

IF EXIST dist\FOTOapp.exe (
    echo SIKERES BUILD!
    echo Az FOTOapp.exe fajl a 'dist' mappaban talalhato (dist\FOTOapp.exe).
) ELSE (
    echo HIBA A BUILD SORAN!
    echo Ellenorizd a fenti PyInstaller uzeneteket a hiba okának felderítéséhez.
)

echo.
pause