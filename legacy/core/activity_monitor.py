# core/activity_monitor.py

import sys
import ctypes


def get_idle_duration() -> float:
    if not sys.platform.startswith("win"):
        return 0.0

    class LASTINPUTINFO(ctypes.Structure):
        _fields_ = [("cbSize", ctypes.c_uint), ("dwTime", ctypes.c_uint)]

    last_input_info = LASTINPUTINFO()
    last_input_info.cbSize = ctypes.sizeof(LASTINPUTINFO)

    if not ctypes.windll.user32.GetLastInputInfo(ctypes.byref(last_input_info)):
        return 0.0

    tick_count = ctypes.windll.kernel32.GetTickCount()
    idle_ms = tick_count - last_input_info.dwTime
    return max(0.0, idle_ms / 1000.0)
