# -*- mode: python ; coding: utf-8 -*-
import os


a = Analysis(
    ['main.py'],
    # Use the directory containing this spec file so the project can be built
    # from any location
    pathex=[os.path.dirname(os.path.abspath(__file__))],
    binaries=[],
    datas=[('assets', 'assets')],
    hiddenimports=['PySide6.QtNetwork', 'apscheduler.triggers.cron', 'apscheduler.jobstores.base', 'pygetwindow', 'win32gui', 'win32process'],
    hookspath=[],
    hooksconfig={},
    runtime_hooks=[],
    excludes=[],
    noarchive=False,
    optimize=0,
)
pyz = PYZ(a.pure)

exe = EXE(
    pyz,
    a.scripts,
    a.binaries,
    a.datas,
    [],
    name='FOTOapp',
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    upx_exclude=[],
    runtime_tmpdir=None,
    console=False,
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
    icon=['assets\\camera_icon.ico'],
)
