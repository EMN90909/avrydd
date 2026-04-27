; ============================================================
; Avryd Screen Reader — NSIS Installer Script
; ============================================================

!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "WinMessages.nsh"
!include "x64.nsh"

; ── Metadata ──────────────────────────────────────────────
Name "Avryd Screen Reader"
OutFile "..\dist\AvrydSetup.exe"
InstallDir "$PROGRAMFILES64\Avryd"
InstallDirRegKey HKLM "Software\Avryd" "InstallDir"
RequestExecutionLevel admin
SetCompressor /SOLID lzma
Unicode True

!define PRODUCT_NAME       "Avryd"
!define PRODUCT_VERSION    "1.0.0"
!define PRODUCT_PUBLISHER  "Avryd"
!define PRODUCT_WEB_SITE   "https://avryd.onrender.com"
!define REGKEY             "Software\Microsoft\Windows\CurrentVersion\Uninstall\Avryd"

; ── MUI Settings ──────────────────────────────────────────
!define MUI_ABORTWARNING
!define MUI_ICON "..\misc\images\icon_256.ico"
!define MUI_UNICON "..\misc\images\icon_256.ico"
!define MUI_WELCOMEPAGE_TITLE "Welcome to Avryd Screen Reader Setup"
!define MUI_WELCOMEPAGE_TEXT "This wizard will guide you through the installation of Avryd $\r$\n$\r$\nAvryd is a powerful screen reader for Windows that reads your screen aloud, helps you navigate apps, web pages, documents, and dialogs.$\r$\n$\r$\nClick Next to continue."
!define MUI_FINISHPAGE_RUN "$INSTDIR\Avryd.exe"
!define MUI_FINISHPAGE_RUN_TEXT "Launch Avryd Screen Reader"
!define MUI_FINISHPAGE_LINK "Visit Avryd website"
!define MUI_FINISHPAGE_LINK_LOCATION "${PRODUCT_WEB_SITE}"

; ── Pages ─────────────────────────────────────────────────
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "..\PRIVACY_TERMS.txt"
!insertmacro MUI_PAGE_DIRECTORY
Page custom SummaryPage SummaryPageLeave
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

; ── Detect 32/64-bit ──────────────────────────────────────
Function .onInit
    ${If} ${RunningX64}
        StrCpy $INSTDIR "$PROGRAMFILES64\Avryd"
    ${Else}
        StrCpy $INSTDIR "$PROGRAMFILES\Avryd"
    ${EndIf}
FunctionEnd

; ── Summary Page ──────────────────────────────────────────
Var SummaryDlg

Function SummaryPage
    nsDialogs::Create 1018
    Pop $SummaryDlg

    ${NSD_CreateLabel} 0 0 100% 12u "Installation Summary"
    ${NSD_CreateLabel} 0 20u 100% 12u "The following will be installed:"
    ${NSD_CreateLabel} 0 40u 100% 12u "  • Avryd Screen Reader v${PRODUCT_VERSION}"
    ${NSD_CreateLabel} 0 54u 100% 12u "  • Piper TTS voice engine"
    ${NSD_CreateLabel} 0 68u 100% 12u "  • Desktop shortcut"
    ${NSD_CreateLabel} 0 82u 100% 12u "  • Added to system PATH"
    ${NSD_CreateLabel} 0 96u 100% 12u "  • Uninstaller"
    ${NSD_CreateLabel} 0 120u 100% 12u "Install location:"
    ${NSD_CreateLabel} 0 134u 100% 12u "$INSTDIR"

    nsDialogs::Show
FunctionEnd

Function SummaryPageLeave
FunctionEnd

; ── Install Section ────────────────────────────────────────
Section "Avryd Core" SecCore
    SectionIn RO

    SetOutPath "$INSTDIR"

    ; Main executables
    File "..\dist\app\Avryd.exe"
    File "..\dist\app\Avryd.Core.dll"
    File /nonfatal "..\dist\app\*.dll"
    File /nonfatal "..\dist\app\*.json"

    ; Runtime files
    SetOutPath "$INSTDIR\runtimes"
    File /r /nonfatal "..\dist\app\runtimes\*.*"

    ; Piper TTS
    SetOutPath "$INSTDIR\resources\piper"
    File /nonfatal "..\resources\piper\piper.exe"
    File /nonfatal "..\resources\piper\*.dll"
    File /nonfatal "..\resources\piper\*.onnx"

    SetOutPath "$INSTDIR\resources\piper\voices"
    File /nonfatal /r "..\resources\piper\voices\*.*"

    ; Tessdata (OCR)
    SetOutPath "$INSTDIR\resources\tessdata"
    File /nonfatal /r "..\resources\tessdata\*.*"

    ; Icons
    SetOutPath "$INSTDIR\Assets"
    File /nonfatal "..\misc\images\icon_256.ico"
    File /nonfatal "..\misc\images\icon_128.ico"
    File /nonfatal "..\misc\images\icon_64.ico"

    ; Plugins directory
    SetOutPath "$INSTDIR\plugins"

    ; Write registry keys
    WriteRegStr HKLM "${REGKEY}" "DisplayName" "${PRODUCT_NAME} Screen Reader"
    WriteRegStr HKLM "${REGKEY}" "DisplayVersion" "${PRODUCT_VERSION}"
    WriteRegStr HKLM "${REGKEY}" "Publisher" "${PRODUCT_PUBLISHER}"
    WriteRegStr HKLM "${REGKEY}" "URLInfoAbout" "${PRODUCT_WEB_SITE}"
    WriteRegStr HKLM "${REGKEY}" "InstallLocation" "$INSTDIR"
    WriteRegStr HKLM "${REGKEY}" "DisplayIcon" "$INSTDIR\Avryd.exe"
    WriteRegStr HKLM "${REGKEY}" "UninstallString" "$INSTDIR\Uninstall.exe"
    WriteRegStr HKLM "${REGKEY}" "QuietUninstallString" "$INSTDIR\Uninstall.exe /S"
    WriteRegDWORD HKLM "${REGKEY}" "NoModify" 1
    WriteRegDWORD HKLM "${REGKEY}" "NoRepair" 1
    WriteRegStr HKLM "Software\Avryd" "InstallDir" "$INSTDIR"

    ; Add to system PATH
    EnVar::SetHKLM
    EnVar::AddValue "PATH" "$INSTDIR"

    ; Shortcuts
    CreateDirectory "$SMPROGRAMS\Avryd"
    CreateShortcut "$SMPROGRAMS\Avryd\Avryd Screen Reader.lnk" "$INSTDIR\Avryd.exe" "" "$INSTDIR\Assets\icon_256.ico"
    CreateShortcut "$SMPROGRAMS\Avryd\Uninstall Avryd.lnk" "$INSTDIR\Uninstall.exe"
    CreateShortcut "$DESKTOP\Avryd.lnk" "$INSTDIR\Avryd.exe" "" "$INSTDIR\Assets\icon_256.ico"

    ; Write uninstaller
    WriteUninstaller "$INSTDIR\Uninstall.exe"

SectionEnd

; ── Uninstall Section ──────────────────────────────────────
Section "Uninstall"
    ; Remove from PATH
    EnVar::SetHKLM
    EnVar::DeleteValue "PATH" "$INSTDIR"

    ; Remove files
    RMDir /r "$INSTDIR\resources"
    RMDir /r "$INSTDIR\plugins"
    RMDir /r "$INSTDIR\runtimes"
    RMDir /r "$INSTDIR\Assets"
    Delete "$INSTDIR\*.exe"
    Delete "$INSTDIR\*.dll"
    Delete "$INSTDIR\*.json"
    RMDir "$INSTDIR"

    ; Remove shortcuts
    Delete "$DESKTOP\Avryd.lnk"
    RMDir /r "$SMPROGRAMS\Avryd"

    ; Remove registry
    DeleteRegKey HKLM "${REGKEY}"
    DeleteRegKey HKLM "Software\Avryd"

    MessageBox MB_OK "Avryd has been uninstalled. Your settings in AppData\Local\Avryd were preserved."
SectionEnd
