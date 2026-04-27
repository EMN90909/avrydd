AVRYD — PIPER TTS ENGINE
========================

Place the Piper TTS binary files in this folder:

  resources/piper/
  ├── piper.exe             ← Main Piper executable
  ├── piper_phonemize.dll   ← Required DLL (from Piper release)
  ├── espeak-ng-data/       ← eSpeak data directory
  └── voices/               ← Voice model files
      ├── en_US-lessac-medium.onnx
      ├── en_US-lessac-medium.onnx.json
      └── (other .onnx voice files)

DOWNLOAD PIPER
--------------
Get the latest Piper release from:
  https://github.com/rhasspy/piper/releases

Download: piper_windows_amd64.zip
Extract all files to this folder (resources/piper/).

DOWNLOAD VOICE MODELS
---------------------
Voice models are .onnx files. Get them from:
  https://huggingface.co/rhasspy/piper-voices/

Recommended voices:
  en_US-lessac-medium.onnx     (English, US, natural)
  en_GB-alan-medium.onnx       (English, UK)
  de_DE-thorsten-medium.onnx   (German)
  fr_FR-siwis-medium.onnx      (French)
  es_ES-mls_9972-low.onnx      (Spanish)

Place .onnx files and their .onnx.json config files
in the voices/ subfolder.

TESSDATA (OCR FALLBACK)
-----------------------
For OCR fallback support, also place Tesseract data in:
  resources/tessdata/

Download from:
  https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata

Save as: resources/tessdata/eng.traineddata
