[Setup]
AppName=St Marks Bulletin Generator
AppVersion=1.0
; Where the app installs by default (Program Files)
DefaultDirName={autopf}\StMarkBulletinGenerator
DefaultGroupName=St Mark Bulletin Generator
; Output settings for the generated setup.exe
OutputDir=./installer_output
OutputBaseFilename=setup
Compression=lzma2
SolidCompression=yes

[Files]
; Grab everything from the dotnet publish output folder
Source: "publish_output\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Creates Start Menu and Desktop shortcuts. 
; NOTE: Ensure "st_mark_bulletin_generator.exe" matches the exact name of your generated executable.
Name: "{group}\St Mark Bulletin Generator"; Filename: "{app}\st_mark_bulletin_generator.exe"
Name: "{autodesktop}\St Mark Bulletin Generator"; Filename: "{app}\st_mark_bulletin_generator.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"
