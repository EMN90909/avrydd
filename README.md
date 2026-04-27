# AVRYD — Accessibility Screen Reader

**AVRYD** is a Windows desktop screen reader built in Python. It runs permanently
in the background, detects foreground window changes, scrapes readable text via
Windows UI Automation, and speaks it aloud through Windows SAPI using pyttsx3.

---

## Features

| Feature | Detail |
|---|---|
| **Speech** | Windows SAPI via pyttsx3 — choose voice, rate, volume |
| **UI Scraping** | uiautomation walks the active window's UI tree up to 10 levels deep |
| **Focus Tracking** | win32gui detects foreground window changes in real time |
| **Hotkeys** | Global `Ctrl+Alt+S` (read), `Ctrl+Alt+P` (pause), `Ctrl+Alt+G` (show GUI) |
| **GUI** | Clean Tkinter control panel — all settings, no code editing needed |
| **System Tray** | Runs quietly in tray; right-click for quick actions |
| **Startup** | Optional Windows registry entry to launch at login |
| **Verbosity** | "Main text only" or "All controls" scraping modes |
| **Modes** | On window change / timed interval / manual (hotkey only) |
| **Persistent** | Saves settings to `avryd_config.json` next to the executable |

---

## Project Structure (flat — no subfolders)

```
avryd.py          ← Main app orchestrator, entry point
speaker.py        ← pyttsx3/SAPI speech engine wrapper
ui_scraper.py     ← UI Automation scraper (UIScraper class)
focus_tracker.py  ← Foreground window change detector
hotkeys.py        ← Global hotkey manager (keyboard library)
gui.py            ← Tkinter control panel
config.py         ← JSON config load/save (singleton)
logger.py         ← Rotating file + console logger
requirements.txt  ← Python dependencies
setup.iss         ← Inno Setup installer script
build.bat         ← One-click build + installer packaging
README.md         ← This file
avryd_config.json ← Auto-generated settings file
avryd.log         ← Auto-generated log file
```

---

## Installation (from source)

### Prerequisites

- **Windows 10/11** (Windows only — uses SAPI, win32, UI Automation)
- **Python 3.10+**
- **pip**

### 1. Install dependencies

```bash
pip install -r requirements.txt
```

### 2. Run AVRYD

```bash
python avryd.py
```

AVRYD will:
1. Start the speech engine and say "AVRYD is active and listening."
2. Open the settings GUI
3. Begin watching for foreground window changes
4. Appear in the system tray

---

## Building a distributable

### Prerequisite: PyInstaller

```bash
pip install pyinstaller
```

### One-click build

```bat
build.bat
```

This will:
1. Install all dependencies
2. Run PyInstaller to produce `dist\avryd\avryd.exe`
3. If **Inno Setup 6** is installed, compile `dist\installer\AVRYD_Setup.exe`

Install `AVRYD_Setup.exe` on any Windows machine — no Python required.

---

## Default Hotkeys

| Hotkey | Action |
|---|---|
| `Ctrl + Alt + S` | Read current window now |
| `Ctrl + Alt + P` | Pause / Resume background reading |
| `Ctrl + Alt + G` | Show settings GUI |

Hotkeys can be changed or disabled in the GUI → Hotkeys panel.

---

## Configuration

Settings are saved to `avryd_config.json` automatically when you click
**Save Settings** in the GUI. They are loaded on every startup.

Key settings:

```json
{
  "voice_id": null,
  "rate": 175,
  "volume": 1.0,
  "verbosity": "main_text",
  "refresh_mode": "on_change",
  "refresh_interval": 3,
  "hotkeys_enabled": true,
  "launch_at_startup": false,
  "minimize_to_tray": true
}
```

---

## Verbosity Modes

| Mode | Description |
|---|---|
| `main_text` | Reads document areas, edit fields, large text — filters buttons, menus, nav chrome |
| `all_controls` | Reads every labelled control including buttons, tabs, menu items |

---

## Refresh / Trigger Modes

| Mode | Description |
|---|---|
| `on_change` | Speaks automatically when the foreground window changes |
| `timed` | Re-reads the window every N seconds (set interval in GUI) |
| `manual` | Silent — only reads when hotkey or "Read Now" button is pressed |

---

## Extending AVRYD

Each module is self-contained and easy to expand:

- **Browser reading** → extend `ui_scraper.py` to detect browser windows and use CDP/Selenium
- **OCR** → add `ocr_scraper.py` using pytesseract for non-accessible apps
- **Document reading** → add `doc_reader.py` using python-docx / pdfplumber
- **Custom filters** → add filter rules to `UIScraper._walk()` by app name or window class
- **New hotkeys** → add entries to `HotkeyManager` and expose them in `gui.py`

---

## License

© Emtra. All rights reserved.
