; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{E75E0C90-F9D2-4A1D-93AD-10DE1E6C478E}
AppName=Spake
AppVersion=1.4
;AppVerName=Spake 1.0
AppPublisherURL=https://github.com/vjdw/Spake
AppSupportURL=https://github.com/vjdw/Spake
AppUpdatesURL=https://github.com/vjdw/Spake
DefaultDirName={autopf}\Spake
DisableProgramGroupPage=yes
; Remove the following line to run in administrative install mode (install for all users.)
PrivilegesRequired=lowest
OutputBaseFilename=Spake
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "C:\Users\vince\source\Spake\Spake\bin\Release\net6.0-windows\win-x86\Spake.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\vince\source\Spake\Spake\bin\Release\net6.0-windows\win-x86\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{autoprograms}\Spake"; Filename: "{app}\Spake.exe"
Name: "{autodesktop}\Spake"; Filename: "{app}\Spake.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Spake.exe"; Description: "{cm:LaunchProgram,Spake}"; Flags: nowait postinstall skipifsilent

