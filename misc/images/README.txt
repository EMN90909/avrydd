AVRYD ICONS
===========

This directory contains SVG source icons for Avryd.
Convert them to .ico files for use in the Windows app and installer.

Files:
  icon_256.svg  →  icon_256.ico  (256x256, main icon)
  icon_128.svg  →  icon_128.ico  (128x128)
  icon_64.svg   →  icon_64.ico   (64x64, tray icon)

Conversion tools:
  - Inkscape: File → Export → As ICO
  - ImageMagick: convert icon_256.svg -resize 256x256 icon_256.ico
  - Online: https://convertio.co/svg-ico/

The build.bat script expects .ico files in this directory.
Copy the converted files here before running build.bat.
