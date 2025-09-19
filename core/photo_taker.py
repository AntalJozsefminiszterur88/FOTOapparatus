# core/photo_taker.py

import logging
import os
from datetime import datetime

import cv2


logger = logging.getLogger(__name__)


def take_photo(save_directory, filename_prefix="Foto"):
    """Készít egy fényképet az alapértelmezett webkamerával."""
    cap = cv2.VideoCapture(0)
    success, frame = cap.read()
    cap.release()
    if not success:
        logger.error("Nem sikerült képet készíteni a webkameráról.")
        return None

    os.makedirs(save_directory, exist_ok=True)
    timestamp = datetime.now().strftime("%Y_%m_%d_%H-%M")
    filename = f"{filename_prefix}_{timestamp}.png"
    save_path = os.path.join(save_directory, filename)
    if cv2.imwrite(save_path, frame):
        logger.info("Fotó elmentve: %s", save_path)
        return save_path
    logger.error("Nem sikerült elmenteni a fotót ide: %s", save_path)
    return None
