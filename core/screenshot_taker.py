# core/screenshot_taker.py

"""Screenshot utility using Pillow and pywin32.

This module provides ``take_screenshot`` which can capture either the full
screen (or a custom region) or the content of a specific application window
without activating it.  On Windows the ``PrintWindow`` API is used so that
background windows are captured correctly.  The function still saves the
image to disk like the old implementation but returns the resulting
``PIL.Image.Image`` object for further processing if needed.
"""

from __future__ import annotations

import os
from datetime import datetime
from typing import Optional

from PIL import Image, ImageDraw, ImageFont, ImageGrab

# Only import pywin32 modules on Windows
import platform

if platform.system() == "Windows":
    import win32con
    import win32gui
    import win32ui


def _capture_window(title: str) -> Optional[Image.Image]:
    """Capture a window by title using the PrintWindow API.

    Parameters
    ----------
    title: str
        Title of the window to capture.  The first matching visible window is
        used.  Matching is case-insensitive and substring based.

    Returns
    -------
    PIL.Image.Image or None
        The captured image or ``None`` if the window was not found or
        ``PrintWindow`` failed.
    """
    if platform.system() != "Windows":
        return None

    hwnd = None

    def _enum(hwnd_in, lparam):
        nonlocal hwnd
        if win32gui.IsWindowVisible(hwnd_in):
            window_text = win32gui.GetWindowText(hwnd_in)
            if title.lower() in window_text.lower():
                hwnd = hwnd_in
                return False  # stop enumeration
        return True

    win32gui.EnumWindows(_enum, None)
    if not hwnd:
        return None

    left, top, right, bottom = win32gui.GetWindowRect(hwnd)
    width = right - left
    height = bottom - top

    hwnd_dc = win32gui.GetWindowDC(hwnd)
    mfc_dc = win32ui.CreateDCFromHandle(hwnd_dc)
    save_dc = mfc_dc.CreateCompatibleDC()

    bitmap = win32ui.CreateBitmap()
    bitmap.CreateCompatibleBitmap(mfc_dc, width, height)
    save_dc.SelectObject(bitmap)

    result = win32gui.PrintWindow(hwnd, save_dc.GetSafeHdc(), 0)

    bmpinfo = bitmap.GetInfo()
    bmpstr = bitmap.GetBitmapBits(True)
    img = Image.frombuffer(
        "RGB",
        (bmpinfo["bmWidth"], bmpinfo["bmHeight"]),
        bmpstr,
        "raw",
        "BGRX",
        0,
        1,
    )

    win32gui.DeleteObject(bitmap.GetHandle())
    save_dc.DeleteDC()
    mfc_dc.DeleteDC()
    win32gui.ReleaseDC(hwnd, hwnd_dc)

    if result != 1:
        return None
    return img


def _capture_screen(region: Optional[tuple[int, int, int, int]] = None) -> Image.Image:
    """Capture the full screen or a region using Pillow."""
    return ImageGrab.grab(bbox=region)


def _add_timestamp(img: Image.Image, position: str) -> None:
    """Draw timestamp onto the image in-place."""
    draw = ImageDraw.Draw(img)
    timestamp_text = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    try:
        font = ImageFont.truetype("arial.ttf", 14)
    except Exception:
        font = ImageFont.load_default()

    text_w, text_h = draw.textsize(timestamp_text, font=font)
    margin = 10
    x = margin
    y = margin
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
    """Capture a screenshot or a program window.

    Parameters
    ----------
    save_directory: str
        Directory where the image will be saved.
    filename_prefix: str
        Prefix for the generated filename.
    area: QRect or None
        Region to capture when ``capture_type`` is ``"screenshot"``.  When using
        PySide6 the caller usually provides a ``QRect``.  The coordinates are
        interpreted in screen coordinates.
    add_timestamp: bool
        Whether to draw the current timestamp onto the image.
    timestamp_position: str
        One of ``"top-left"``, ``"top-right"``, ``"bottom-left"`` or
        ``"bottom-right"``.
    window_title: str
        Title of the window to capture when ``capture_type`` is ``"program"``.
    capture_type: str
        Either ``"screenshot"`` or ``"program"``.  ``window_title`` can still be
        used for backwards compatibility – if given, ``capture_type`` is treated
        as ``"program"``.

    Returns
    -------
    PIL.Image.Image or None
        The captured image, or ``None`` on error.
    """

    if window_title and capture_type != "program":
        capture_type = "program"

    region = None
    if capture_type == "screenshot" and area is not None:
        try:
            from PySide6.QtCore import QRect

            if isinstance(area, QRect):
                region = (area.x(), area.y(), area.x() + area.width(), area.y() + area.height())
            else:
                # assume tuple-like (x, y, w, h)
                region = (int(area[0]), int(area[1]), int(area[0]) + int(area[2]), int(area[1]) + int(area[3]))
        except Exception:
            pass

    if capture_type == "program":
        img = _capture_window(window_title)
        if img is None:
            # Fallback to full screen capture if window not found
            img = _capture_screen(None)
    else:
        img = _capture_screen(region)

    if img is None:
        return None

    if add_timestamp:
        _add_timestamp(img, timestamp_position)

    os.makedirs(save_directory, exist_ok=True)
    timestamp_for_filename = datetime.now().strftime("%Y_%m_%d_%H-%M")
    filename = f"{filename_prefix}_{timestamp_for_filename}.png"
    save_path = os.path.join(save_directory, filename)

    try:
        img.save(save_path)
        print(f"Képernyőkép sikeresen elmentve: {save_path}")
    except Exception as exc:  # pragma: no cover - logging only
        print(f"HIBA: Nem sikerült elmenteni a képernyőképet ide: {save_path} - {exc}")
        return None

    return img


if __name__ == "__main__":
    # Simple manual test when running this file directly
    out_dir = os.path.join(os.path.expanduser("~"), "Desktop")
    take_screenshot(out_dir, capture_type="screenshot")
