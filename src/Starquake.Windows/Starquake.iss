; The Windows installer. Inno Setup 6, which is already on GitHub's windows runners.
;
;     iscc /DPayloadDir=<the publish folder> /DAppVersion=1.2.3 Starquake.iss
;
; Both defines have defaults so the script can be compiled by hand without them. PayloadDir is
; whatever `dotnet publish -p:PublishProfile=Aot-win-x64` left behind - the native executable,
; SDL2.dll, openal.dll, Content/ and Data/ - and all of it is installed verbatim.

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#ifndef PayloadDir
  #define PayloadDir "bin\Publish\win-x64"
#endif

[Setup]
AppName=Starquake
AppVersion={#AppVersion}
AppPublisher=Damien C Green
DefaultDirName={autopf}\Starquake
DefaultGroupName=Starquake
OutputBaseFilename=Starquake-win-x64-Setup
SetupIconFile=Starquake.ico
UninstallDisplayIcon={app}\Starquake.exe
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
; x64compatible rather than x64: the publish is win-x64 and will not run on 32-bit Windows, but
; this still lets it install on arm64 Windows, where x64 binaries run under emulation.
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; No UAC prompt and no administrator. With lowest privileges {autopf} resolves to the user's own
; Programs folder instead of Program Files, which is the friendlier install for a game and costs
; nothing: the game writes its high scores to ApplicationData and its screenshots to the pictures
; folder, so it never needs to write where it is installed.
PrivilegesRequired=lowest

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Files]
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{group}\Starquake"; Filename: "{app}\Starquake.exe"
Name: "{group}\Uninstall Starquake"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Starquake"; Filename: "{app}\Starquake.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Starquake.exe"; Description: "Play Starquake now"; Flags: nowait postinstall skipifsilent
