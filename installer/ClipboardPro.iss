; Build with Inno Setup 6. This creates a per-user install and a clean uninstaller.
#define AppName "Clipboard Pro"
#define AppVersion "1.0.12"
#define AppExeName "ClipboardPro.exe"
[Setup]
AppId={{8BC88A28-B048-4EC6-A8CB-66EF8D1549E5}
AppName={#AppName}
AppVersion={#AppVersion}
DefaultDirName={localappdata}\Programs\Clipboard Pro
DefaultGroupName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}
PrivilegesRequired=lowest
OutputDir=..\artifacts
OutputBaseFilename=ClipboardPro-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\src\ClipboardPro\Assets\ClipboardPro.ico
WizardImageFile=..\src\ClipboardPro\Assets\InstallerWizard.bmp
WizardSmallImageFile=..\src\ClipboardPro\Assets\InstallerWizardSmall.bmp
CloseApplications=yes
[Files]
Source: "..\src\ClipboardPro\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Tasks]
Name: "startup"; Description: "Iniciar Clipboard Pro con Windows"; Flags: unchecked
Name: "desktopicon"; Description: "Crear acceso directo en el escritorio"; Flags: unchecked
[Icons]
Name: "{autostartup}\Clipboard Pro"; Filename: "{app}\{#AppExeName}"; Parameters: "--background"; Tasks: startup
Name: "{autodesktop}\Clipboard Pro"; Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\Assets\ClipboardPro.ico"; Tasks: desktopicon
[Run]
Filename: "{app}\{#AppExeName}"; Description: "Abrir Clipboard Pro"; Flags: nowait postinstall skipifsilent
[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var Answer: Integer;
begin
  if CurUninstallStep = usUninstall then begin
    Answer := MsgBox('¿Quieres conservar tu historial y configuración?', mbConfirmation, MB_YESNO);
    if Answer = IDNO then DelTree(ExpandConstant('{localappdata}\Clipboard Pro\ClipboardPro'), True, True, True);
  end;
end;
