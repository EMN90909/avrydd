; setup.iss - Inno Setup Script for AVRYD65 (.NET & C++)
; Use Inno Setup Compiler to build the setup.exe

[Setup]
AppName=AVRYD65
AppVersion=6.5
DefaultDirName={pf}\AVRYD65
DefaultGroupName=AVRYD65
OutputDir=dist
OutputBaseFilename=Avryd_setup
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin

[Files]
; All files from the publish folder (includes Service, Launcher, and DLLs)
Source: "dist\publish\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs

[Registry]
; Add to system PATH
Root: HKLM; Subkey: "SYSTEM\CurrentControlSet\Control\Session Manager\Environment"; \
    ValueType: expandsz; ValueName: "Path"; ValueData: "{olddata};{app}"; \
    Check: NeedsAddPath('{app}')

; Run on Windows Startup
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
    ValueType: string; ValueName: "AVRYD65"; ValueData: """{app}\Avryd65.Launcher.exe"""; Flags: uninsdeletevalue

[Icons]
Name: "{group}\AVRYD65 Launcher"; Filename: "{app}\Avryd65.Launcher.exe"
Name: "{commondesktop}\AVRYD65 Launcher"; Filename: "{app}\Avryd65.Launcher.exe"

[Run]
Filename: "{app}\Avryd65.Launcher.exe"; Description: "Launch AVRYD65 Launcher"; Flags: nowait postinstall

[Code]
var
  Voice: Variant;
  LastSpoken: string;

procedure InitializeVoice;
begin
  try
    Voice := CreateOleObject('SAPI.SpVoice');
  except
  end;
end;

procedure Speak(Text: string);
begin
  if not VarIsEmpty(Voice) then
  begin
    if Text <> LastSpoken then
    begin
      Voice.Speak(Text, 1); // 1 = SVSFlagsAsync
      LastSpoken := Text;
    end;
  end;
end;

procedure CurPageChanged(CurPageID: Integer);
var
  PageName: string;
begin
  case CurPageID of
    wpWelcome: PageName := 'Welcome to AVRYD 65 Setup';
    wpLicense: PageName := 'License Agreement';
    wpPassword: PageName := 'Password Page';
    wpInfoBefore: PageName := 'Information Before Installation';
    wpUserInfo: PageName := 'User Information';
    wpSelectDir: PageName := 'Select Installation Directory';
    wpSelectComponents: PageName := 'Select Components';
    wpSelectProgramGroup: PageName := 'Select Start Menu Folder';
    wpSelectTasks: PageName := 'Select Additional Tasks';
    wpReady: PageName := 'Ready to Install';
    wpPreparing: PageName := 'Preparing to Install';
    wpInstalling: PageName := 'Installing';
    wpInfoAfter: PageName := 'Information After Installation';
    wpFinished: PageName := 'Setup Finished';
  else
    PageName := 'Setup Page';
  end;
  Speak(PageName);
end;

procedure InitializeWizard;
begin
  InitializeVoice;
  Speak('AVRYD 65 Installer Started');
end;

function NeedsAddPath(Param: string): boolean;
var
  OrigPath: string;
begin
  if not RegQueryStringValue(HKEY_LOCAL_MACHINE,
    'SYSTEM\CurrentControlSet\Control\Session Manager\Environment',
    'Path', OrigPath)
  then begin
    Result := True;
    exit;
  end;
  Result := Pos(';' + Param + ';', ';' + OrigPath + ';') = 0;
end;
