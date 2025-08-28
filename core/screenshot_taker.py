from __future__ import annotations

import os
import time
from datetime import datetime
from typing import Optional
import ctypes

from PIL import Image, ImageDraw, ImageFont, ImageGrab

import platform

if platform.system() == "Windows":
    import win32con
    import win32gui
    import win32ui
    import win32process
    import win32api

PW_RENDERFULLCONTENT = 0x00000002


def _capture_window(
    title: str,
    *,
    restore_foreground: bool = True,
    pre_action: Optional[callable] = None,
    executable: Optional[str] = None,
) -> Optional[Image.Image]:
    """Capture a window matching *title* and optionally *executable*.

    Providing ``executable`` ensures that the window belongs to the given
    process (e.g. ``discord.exe``), which helps avoid bringing the wrong
    window to the foreground.
    """
    if platform.system() != "Windows":
        return None

    hwnd = None

    def _enum(hwnd_in, lparam):
        nonlocal hwnd
        if not win32gui.IsWindowVisible(hwnd_in):
            return True

        window_text = win32gui.GetWindowText(hwnd_in)
        if title.lower() not in window_text.lower():
            return True

        if executable:
            try:
                _, pid = win32process.GetWindowThreadProcessId(hwnd_in)
                proc_handle = win32api.OpenProcess(
                    win32con.PROCESS_QUERY_INFORMATION | win32con.PROCESS_VM_READ,
                    False,
                    pid,
                )
                exe_path = win32process.GetModuleFileNameEx(proc_handle, 0)
                win32api.CloseHandle(proc_handle)
                if os.path.basename(exe_path).lower() != executable.lower():
                    return True
            except Exception:
                return True

        hwnd = hwnd_in
        return False

    win32gui.EnumWindows(_enum, None)
    if not hwnd:
        return None

    original_foreground_hwnd = win32gui.GetForegroundWindow()
    
    target_thread_id, _ = win32process.GetWindowThreadProcessId(hwnd)
    current_thread_id = ctypes.windll.kernel32.GetCurrentThreadId()
    
    user32 = ctypes.windll.user32
    user32.AttachThreadInput(current_thread_id, target_thread_id, True)

    img = None
    try:
        # Only restore the window if it is minimized. Calling SW_RESTORE on a
        # fullscreen/ maximized window would shrink it to windowed mode, which
        # is undesirable when capturing programs like Discord.
        if win32gui.IsIconic(hwnd):
            win32gui.ShowWindow(hwnd, win32con.SW_RESTORE)
        win32gui.SetForegroundWindow(hwnd)

        # Ensure the window really moves to the foreground before capturing
        for _ in range(20):
            if win32gui.GetForegroundWindow() == hwnd:
                break
            time.sleep(0.05)
            win32gui.SetForegroundWindow(hwnd)
        else:
            print(f"HIBA: A '{title}' ablak nem került az előtérbe.")
            return None

        if pre_action:
            pre_action()

        time.sleep(0.2)

        # Double-check foreground after pre_action
        if win32gui.GetForegroundWindow() != hwnd:
            print(f"HIBA: A '{title}' ablak időközben elvesztette az előtér státuszát.")
            return None

        window_rect = win32gui.GetWindowRect(hwnd)
        width = window_rect[2] - window_rect[0]
        height = window_rect[3] - window_rect[1]

        if width <= 0 or height <= 0:
            return None

        hwnd_dc = win32gui.GetWindowDC(hwnd)
        mfc_dc = win32ui.CreateDCFromHandle(hwnd_dc)
        save_dc = mfc_dc.CreateCompatibleDC()
        bitmap = win32ui.CreateBitmap()
        bitmap.CreateCompatibleBitmap(mfc_dc, width, height)
        save_dc.SelectObject(bitmap)
        result = user32.PrintWindow(hwnd, save_dc.GetSafeHdc(), PW_RENDERFULLCONTENT)

        bmpinfo = bitmap.GetInfo()
        bmpstr = bitmap.GetBitmapBits(True)
        img = Image.frombuffer("RGB", (bmpinfo["bmWidth"], bmpinfo["bmHeight"]), bmpstr, "raw", "BGRX", 0, 1)

        win32gui.DeleteObject(bitmap.GetHandle())
        save_dc.DeleteDC()
        mfc_dc.DeleteDC()
        win32gui.ReleaseDC(hwnd, hwnd_dc)

        if result != 1:
            return None

        client_rect = win32gui.GetClientRect(hwnd)
        client_left, client_top = win32gui.ClientToScreen(hwnd, (client_rect[0], client_rect[1]))
        client_right, client_bottom = win32gui.ClientToScreen(hwnd, (client_rect[2], client_rect[3]))
        
        crop_left = client_left - window_rect[0]
        crop_top = client_top - window_rect[1]
        crop_right = client_right - window_rect[0]
        crop_bottom = client_bottom - window_rect[1]

        crop_box = (crop_left, crop_top, crop_right, crop_bottom)
        
        if crop_box[0] < crop_box[2] and crop_box[1] < crop_box[3]:
            img = img.crop(crop_box)
        
        return img

    except Exception as e:
        print(f"HIBA a '{title}' ablak rögzítése közben: {e}")
        return None
    finally:
        # --- A VÉGSŐ JAVÍTÁS ---
        # Mielőtt visszaállítjuk az eredeti ablakot, ellenőrizzük, hogy még létezik-e!
        if restore_foreground and original_foreground_hwnd and win32gui.IsWindow(original_foreground_hwnd):
            try:
                win32gui.SetForegroundWindow(original_foreground_hwnd)
            except Exception:
                print("Figyelmeztetés: Az eredeti ablakot nem sikerült visszaállítani az előtérbe.")

        user32.AttachThreadInput(current_thread_id, target_thread_id, False)


def _capture_screen(region: Optional[tuple[int, int, int, int]] = None) -> Image.Image:
    return ImageGrab.grab(bbox=region)


def _press_ctrl_number(number: int) -> None:
    if platform.system() != "Windows":
        return
    number = max(0, min(9, int(number)))
    vk_code = ord(str(number))
    win32api.keybd_event(win32con.VK_CONTROL, 0, 0, 0)
    win32api.keybd_event(vk_code, 0, 0, 0)
    # hagyjunk egy kis időt, hogy a rendszer érzékelje a kombinációt
    time.sleep(0.05)
    win32api.keybd_event(vk_code, 0, win32con.KEYEVENTF_KEYUP, 0)
    win32api.keybd_event(win32con.VK_CONTROL, 0, win32con.KEYEVENTF_KEYUP, 0)


def _add_timestamp(img: Image.Image, position: str) -> None:
    draw = ImageDraw.Draw(img)
    timestamp_text = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    try:
        font = ImageFont.truetype("arial.ttf", 14)
    except Exception:
        font = ImageFont.load_default()
    try:
        bbox = draw.textbbox((0, 0), timestamp_text, font=font)
        text_w = bbox[2] - bbox[0]
        text_h = bbox[3] - bbox[1]
    except AttributeError:
        text_w, text_h = draw.textsize(timestamp_text, font=font)
    margin = 10
    x, y = margin, margin
    if position == "top-right":
        x = img.width - text_w - margin
    elif position == "bottom-left":
        y = img.height - text_h - margin
    elif position == "bottom-right":
        x = img.width - text_w - margin
        y = img.height - text_h - margin
    draw.text((x, y), timestamp_text, fill="white", font=font)


def take_screenshot(
    save_directory: str,
    filename_prefix: str = "Kép",
    area: Optional[object] = None,
    add_timestamp: bool = False,
    timestamp_position: str = "top-left",
    window_title: str = "",
    capture_type: str = "screenshot",
) -> Optional[Image.Image]:
    if window_title and capture_type != "program":
        capture_type = "program"

    region = None
    if capture_type == "screenshot" and area is not None:
        try:
            from PySide6.QtCore import QRect
            if isinstance(area, QRect):
                region = (area.x(), area.y(), area.x() + area.width(), area.y() + area.height())
            else:
                region = (int(area[0]), int(area[1]), int(area[0]) + int(area[2]), int(area[1]) + int(area[3]))
        except Exception:
            pass

    img = None
    if capture_type == "program":
        img = _capture_window(window_title)
        if img is None:
            print(f"HIBA: A '{window_title}' ablak nem található, vagy a fókusz-kikényszerítés ellenére sem sikerült képet készíteni.")
    else:
        img = _capture_screen(region)

    if img is None:
        return None

    if add_timestamp:
        _add_timestamp(img, timestamp_position)

    os.makedirs(save_directory, exist_ok=True)
    timestamp_for_filename = datetime.now().strftime("%Y_%m_%d_%H-%M-%S")
    filename = f"{filename_prefix}_{timestamp_for_filename}.png"
    save_path = os.path.join(save_directory, filename)

    try:
        img.save(save_path)
        print(f"Képernyőkép sikeresen elmentve: {save_path}")
    except Exception as exc:
        print(f"HIBA: Nem sikerült elmenteni a képernyőképet ide: {save_path} - {exc}")
        return None

    return img


def take_discord_screenshot(
    save_directory: str,
    filename_prefix: str = "Kép",
    add_timestamp: bool = False,
    timestamp_position: str = "top-left",
    stay_foreground: bool = False,
    use_hotkey: bool = False,
    hotkey_number: int = 1,
    window_title: str = "Discord",
) -> Optional[Image.Image]:
    pre_action = None
    if use_hotkey:
        def pre_action():
            # Bring Discord to the foreground (handled by _capture_window),
            # wait a bit, press the hotkey and wait again before capturing
            time.sleep(2)
            _press_ctrl_number(hotkey_number)
            time.sleep(2)

    img = _capture_window(
        window_title or "Discord",
        pre_action=pre_action,
        restore_foreground=not stay_foreground,
        executable="discord.exe",
    )
    if img is None:
        return None
    if add_timestamp:
        _add_timestamp(img, timestamp_position)

    os.makedirs(save_directory, exist_ok=True)
    timestamp_for_filename = datetime.now().strftime("%Y_%m_%d_%H-%M-%S")
    filename = f"{filename_prefix}_{timestamp_for_filename}.png"
    save_path = os.path.join(save_directory, filename)
    try:
        img.save(save_path)
        print(f"Képernyőkép sikeresen elmentve: {save_path}")
    except Exception as exc:
        print(f"HIBA: Nem sikerült elmenteni a képernyőképet ide: {save_path} - {exc}")
        return None
    return img


if __name__ == "__main__":
    out_dir = os.path.join(os.path.expanduser("~"), "Desktop")
    print("3 másodperc múlva képkészítés egy háttérben lévő ablakról...")
    time.sleep(3)
    take_screenshot(
        out_dir, 
        capture_type="program",
        window_title="Jegyzettömb"
    )
