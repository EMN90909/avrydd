# Avryd Screen Reader

A full-featured, fast, and accessible screen reader for Windows — built in **C# / .NET 8** using the **UI Automation (UIA) API**.

---

## Features

| Feature | Description |
|---|---|
| **UIA Engine** | Core built on Windows UI Automation — reads all standard Windows apps |
| **Piper TTS** | High-quality offline voices via Piper TTS engine |
| **Focus Tracking** | Real-time focus change detection and announcement |
| **Reading Modes** | Browse, Reading, Navigation, Typing, Forms |
| **Keyboard Shortcuts** | Full keyboard navigation with customizable hotkeys |
| **Voice Commands** | Say "Open Chrome", "Volume up", "Next item", etc. |
| **Touch / Gesture** | Swipe, tap, and drag to explore screen by touch |
| **Braille Support** | 6-dot and 8-dot Braille keyboard input |
| **OCR Fallback** | Reads inaccessible apps using Tesseract OCR |
| **Plugin System** | Install language/command plugins from GitHub |
| **Account System** | OAuth sign-in (Google/Microsoft/Facebook), product key activation |
| **Privacy First** | Screen content never sent to cloud by default |
| **32 and 64-bit** | Supports both x86 and x64 Windows |

---

## Project Structure

```
Avryd.sln
src/
  Avryd.Core/          Core engine (UIA, Speech, Focus, Input, Plugins, OCR, Auth)
  Avryd.App/           WPF launcher and settings GUI
  Avryd.Service/       Windows background service
resources/
  piper/               Piper TTS engine (user-supplied — see resources/piper/README.txt)
  tessdata/            Tesseract OCR data (user-supplied)
installer/
  setup.nsi            NSIS installer script
misc/
  images/              App icons (SVG sources — convert to .ico before building)
web/                   Web server files (hosted at avryd.onrender.com)
build.bat              Full build and installer
compile_setup.bat      Installer only
PRIVACY_TERMS.txt      Shown during install
```

---

## Setup and Build

### Prerequisites

- [.NET 8 SDK](https://dot.net) (Windows)
- [NSIS](https://nsis.sourceforge.io) (for installer)
- [Piper TTS](https://github.com/rhasspy/piper/releases) — place in `resources/piper/`
- Voice models — place `.onnx` files in `resources/piper/voices/`
- *(Optional)* [Tesseract data](https://github.com/tesseract-ocr/tessdata) — `resources/tessdata/eng.traineddata`

### Build

```bat
build.bat
```

This will:
1. Restore NuGet packages
2. Compile Avryd (x64 and x86)
3. Copy resources
4. Create `dist/AvrydSetup.exe` (if NSIS is installed)

### Create installer only

```bat
compile_setup.bat
```

---

## Keyboard Shortcuts

| Shortcut | Action |
|---|---|
| `Ctrl+Alt+S` | Stop speaking |
| `Ctrl+Alt+R` | Read current item |
| `Ctrl+Alt+Right` | Next item |
| `Ctrl+Alt+Left` | Previous item |
| `Ctrl+Alt+M` | Toggle reading mode |
| `Ctrl+Alt+A` | Read all content |
| `Ctrl+Alt+P` | Repeat last item |
| `Ctrl+Alt+Home` | Jump to top |
| `Ctrl+Alt+End` | Jump to bottom |
| `Ctrl+Alt+B` | Next button |
| `Ctrl+Alt+L` | Next link |
| `Ctrl+Alt+E` | Next edit field |
| `Ctrl+Alt+H` | Next heading |
| `Ctrl+Alt+G` | Open Avryd launcher |

---

## Authentication and Activation

Avryd uses OAuth (Google/Microsoft/Facebook) via the web backend at **https://avryd.onrender.com**.

1. Sign in on the web portal
2. Copy your product key
3. Open Avryd, enter email and product key — activated

Product keys are single-use and bound to one device (hardware fingerprint).

---

## Plugin System

Plugins are `.dll` files hosted at `https://github.com/avryd/avryd/plugins`.

In the Avryd Launcher, go to the Plugins tab, browse available plugins, and install.

---

## Privacy

- Screen content **never** leaves your device by default
- Passwords are **never** spoken aloud by default
- Local settings encrypted with AES-256
- No analytics or telemetry

---

## License

Copyright 2024 Avryd. All rights reserved.
See [PRIVACY_TERMS.txt](./PRIVACY_TERMS.txt) for full terms.
