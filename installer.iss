[Setup]
AppName=St Mark Bulletin Generator
AppVersion=1.0
; Where the app installs by default (Program Files)
DefaultDirName={autopf}\StMarkBulletinGenerator
; Output settings for the generated setup.exe
OutputDir=./installer_output
OutputBaseFilename=setup
Compression=lzma2
SolidCompression=yes

[Files]
; Grab everything from the dotnet publish output folder
Source: "publish_output\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Creates the Start Menu shortcut directly in the programs list
Name: "{autoprograms}\St Mark Bulletin Generator"; Filename: "{app}\st_mark_bulletin_generator.exe"
; Creates the Desktop shortcut (optional for the user during install)
Name: "{autodesktop}\St Mark Bulletin Generator"; Filename: "{app}\st_mark_bulletin_generator.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"
