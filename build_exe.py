"""Helper script to build the FOTOapp executable with PyInstaller.

The script wraps the ``pyinstaller`` CLI so building the Windows
executable can be done from a Python environment as well.
"""

from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parent
SPEC_FILE = PROJECT_ROOT / "FOTOapp.spec"
DIST_DIR = PROJECT_ROOT / "dist"
BUILD_DIR = PROJECT_ROOT / "build"


def ensure_pyinstaller_available() -> None:
    """Ensure that PyInstaller can be imported.

    The helper prefers the module form (``python -m PyInstaller``) because it
    uses the same interpreter that runs this script.
    """

    if shutil.which("pyinstaller") is None:
        try:
            __import__("PyInstaller")
        except ModuleNotFoundError as exc:  # pragma: no cover - import guard
            raise SystemExit(
                "PyInstaller is not installed. Install it with 'pip install PyInstaller'."
            ) from exc


def run_pyinstaller(args: argparse.Namespace) -> None:
    """Invoke PyInstaller with the project's spec file."""

    if not SPEC_FILE.exists():
        raise SystemExit(f"Spec file not found: {SPEC_FILE}")

    command = [sys.executable, "-m", "PyInstaller", str(SPEC_FILE)]

    if args.clean:
        command.append("--clean")
    if args.noconfirm:
        command.append("--noconfirm")

    subprocess.run(command, check=True)


def parse_arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Build the FOTOapp executable")
    parser.add_argument(
        "--clean",
        action="store_true",
        help="Remove PyInstaller temporary files before building.",
    )
    parser.add_argument(
        "--noconfirm",
        action="store_true",
        help="Replace output directories without asking for confirmation.",
    )
    parser.add_argument(
        "--clear-output",
        action="store_true",
        help="Delete previous build and dist directories before running PyInstaller.",
    )
    return parser.parse_args()


def clear_output_directories() -> None:
    for directory in (DIST_DIR, BUILD_DIR):
        if directory.exists():
            shutil.rmtree(directory)


def main() -> None:
    args = parse_arguments()
    ensure_pyinstaller_available()

    if args.clear_output:
        clear_output_directories()

    run_pyinstaller(args)


if __name__ == "__main__":
    main()
