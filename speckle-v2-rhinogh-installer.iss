;defining variables
#define AppName      "Speckle@Arup-ConnectorRhinoGh"

#define GrasshopperVersion  GetFileVersion("ConnectorGrasshopper\ConnectorGrasshopper\bin\SpeckleConnectorGrasshopper.dll")
#define Rhino6Version  GetFileVersion("ConnectorRhino\ConnectorRhino6\bin\Release\SpeckleConnectorRhino.rhp")
#define Rhino7Version  GetFileVersion("ConnectorRhino\ConnectorRhino7\bin\Release\SpeckleConnectorRhino.rhp")

#define AppPublisher "Speckle@Arup"
#define AppURL       "https://speckle.arup.com"
#define SpeckleFolder "{localappdata}\Speckle"

[Setup]
AppId={{BA3A01AA-F70D-4747-AA0E-E93F38C793C8}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={#SpeckleFolder}
DisableDirPage=yes
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
DisableWelcomePage=no
OutputDir="."
OutputBaseFilename=Speckle@ArupInstallerRhino-v{#AppVersion}
SetupIconFile=ConnectionManager\SpeckleConnectionManagerUI\Assets\favicon.ico
Compression=lzma
SolidCompression=yes
ChangesAssociations=yes
PrivilegesRequired=lowest
VersionInfoVersion={#AppVersion}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Components]
Name: rhino6; Description: Speckle for Rhino 6 - v{#Rhino6Version};  Types: full
Name: rhino7; Description: Speckle for Rhino 7 - v{#Rhino7Version};  Types: full
Name: gh; Description: Speckle for Grasshopper - v{#GrasshopperVersion};  Types: full
Name: kits; Description: Speckle Kits - v{#AppVersion};  Types: full custom; Flags: fixed

[Types]
Name: "full"; Description: "Full installation"
Name: "custom"; Description: "Custom installation"; Flags: iscustom

[Dirs]
Name: "{app}"; Permissions: everyone-full

[Files]
;rhino6                                                                                                                                    
Source: "ConnectorRhino\ConnectorRhino6\bin\Release\*"; DestDir: "{userappdata}\McNeel\Rhinoceros\6.0\Plug-ins\SpeckleRhino2 (8dd5f30b-a13d-4a24-abdc-3e05c8c87143)\"; Flags: ignoreversion recursesubdirs; Components: rhino6

;rhino7
Source: "ConnectorRhino\ConnectorRhino7\bin\Release\*"; DestDir: "{userappdata}\McNeel\Rhinoceros\7.0\Plug-ins\SpeckleRhino2 (8dd5f30b-a13d-4a24-abdc-3e05c8c87143)\"; Flags: ignoreversion recursesubdirs; Components: rhino7

;gh
Source: "ConnectorGrasshopper\ConnectorGrasshopper\bin\*"; DestDir: "{userappdata}\Grasshopper\Libraries\SpeckleGrasshopper2\"; Flags: ignoreversion recursesubdirs; Components: gh
Source: "Objects\Converters\ConverterRhinoGh\ConverterRhino6\bin\Release\netstandard2.0\Objects.Converter.Rhino6.dll"; DestDir: "{userappdata}\Speckle\Kits\Objects\"; Flags: ignoreversion recursesubdirs; Components: rhino6 gh
Source: "Objects\Converters\ConverterRhinoGh\ConverterRhino7\bin\Release\net48\Objects.Converter.Rhino7.dll"; DestDir: "{userappdata}\Speckle\Kits\Objects\"; Flags: ignoreversion recursesubdirs; Components: rhino7 gh

;kits
Source: "Objects\Objects\bin\Release\netstandard2.0\Objects.dll"; DestDir: "{userappdata}\Speckle\Kits\Objects"; Flags: ignoreversion recursesubdirs; Components: kits

[InstallDelete]
Type: filesandordirs; Name: "{userappdata}\McNeel\Rhinoceros\6.0\Plug-ins\SpeckleRhino2 (8dd5f30b-a13d-4a24-abdc-3e05c8c87143)\*"
Type: filesandordirs; Name: "{userappdata}\McNeel\Rhinoceros\7.0\Plug-ins\SpeckleRhino2 (8dd5f30b-a13d-4a24-abdc-3e05c8c87143)\*"
Type: filesandordirs; Name: "{userappdata}\Grasshopper\Libraries\SpeckleGrasshopper2\*"

[Registry]
Root: HKCU; Subkey: "SOFTWARE\McNeel\Rhinoceros\6.0\Plug-ins\8dd5f30b-a13d-4a24-abdc-3e05c8c87143"; ValueType: string; ValueName: "Name"; ValueData: "Speckle";
Root: HKCU; Subkey: "SOFTWARE\McNeel\Rhinoceros\6.0\Plug-ins\8dd5f30b-a13d-4a24-abdc-3e05c8c87143"; ValueType: string; ValueName: "RegPath"; ValueData: "\\HKEY_CURRENT_USER\Software\McNeel\Rhinoceros\6.0\Plug-ins\8dd5f30b-a13d-4a24-abdc-3e05c8c87143";
Root: HKCU; Subkey: "SOFTWARE\McNeel\Rhinoceros\6.0\Plug-ins\8dd5f30b-a13d-4a24-abdc-3e05c8c87143"; ValueType: dword; ValueName: "DirectoryInstall"; ValueData: "0";
Root: HKCU; Subkey: "SOFTWARE\McNeel\Rhinoceros\6.0\Plug-ins\8dd5f30b-a13d-4a24-abdc-3e05c8c87143"; ValueType: dword; ValueName: "IsDotNETPlugIn"; ValueData: "1";
Root: HKCU; Subkey: "SOFTWARE\McNeel\Rhinoceros\6.0\Plug-ins\8dd5f30b-a13d-4a24-abdc-3e05c8c87143"; ValueType: dword; ValueName: "LoadMode"; ValueData: "2";
Root: HKCU; Subkey: "SOFTWARE\McNeel\Rhinoceros\6.0\Plug-ins\8dd5f30b-a13d-4a24-abdc-3e05c8c87143"; ValueType: dword; ValueName: "Type"; ValueData: "16";
Root: HKCU; Subkey: "SOFTWARE\McNeel\Rhinoceros\6.0\Plug-ins\8dd5f30b-a13d-4a24-abdc-3e05c8c87143\CommandList"; ValueType: string; ValueName: "Speckle"; ValueData: "2;Speckle";
Root: HKCU; Subkey: "SOFTWARE\McNeel\Rhinoceros\6.0\Plug-ins\8dd5f30b-a13d-4a24-abdc-3e05c8c87143\PlugIn"; ValueType: string; ValueName: "FileName"; ValueData: "{userappdata}\McNeel\Rhinoceros\6.0\Plug-ins\8dd5f30b-a13d-4a24-abdc-3e05c8c87143\SpeckleConnectorRhino.rhp";  
Root: HKCU; Subkey: "SOFTWARE\McNeel\Rhinoceros\7.0\Plug-ins\8dd5f30b-a13d-4a24-abdc-3e05c8c87143"; ValueType: string; ValueName: "Name"; ValueData: "Speckle";
Root: HKCU; Subkey: "SOFTWARE\McNeel\Rhinoceros\7.0\Plug-ins\8dd5f30b-a13d-4a24-abdc-3e05c8c87143"; ValueType: string; ValueName: "RegPath"; ValueData: "\\HKEY_CURRENT_USER\Software\McNeel\Rhinoceros\7.0\Plug-ins\8dd5f30b-a13d-4a24-abdc-3e05c8c87143";
Root: HKCU; Subkey: "SOFTWARE\McNeel\Rhinoceros\7.0\Plug-ins\8dd5f30b-a13d-4a24-abdc-3e05c8c87143"; ValueType: dword; ValueName: "DirectoryInstall"; ValueData: "0";
Root: HKCU; Subkey: "SOFTWARE\McNeel\Rhinoceros\7.0\Plug-ins\8dd5f30b-a13d-4a24-abdc-3e05c8c87143"; ValueType: dword; ValueName: "IsDotNETPlugIn"; ValueData: "1";
Root: HKCU; Subkey: "SOFTWARE\McNeel\Rhinoceros\7.0\Plug-ins\8dd5f30b-a13d-4a24-abdc-3e05c8c87143"; ValueType: dword; ValueName: "LoadMode"; ValueData: "2";
Root: HKCU; Subkey: "SOFTWARE\McNeel\Rhinoceros\7.0\Plug-ins\8dd5f30b-a13d-4a24-abdc-3e05c8c87143"; ValueType: dword; ValueName: "Type"; ValueData: "16";
Root: HKCU; Subkey: "SOFTWARE\McNeel\Rhinoceros\7.0\Plug-ins\8dd5f30b-a13d-4a24-abdc-3e05c8c87143\CommandList"; ValueType: string; ValueName: "Speckle"; ValueData: "2;Speckle";
Root: HKCU; Subkey: "SOFTWARE\McNeel\Rhinoceros\7.0\Plug-ins\8dd5f30b-a13d-4a24-abdc-3e05c8c87143\PlugIn"; ValueType: string; ValueName: "FileName"; ValueData: "{userappdata}\McNeel\Rhinoceros\7.0\Plug-ins\SpeckleRhino2 (8dd5f30b-a13d-4a24-abdc-3e05c8c87143)\SpeckleConnectorRhino.rhp";  

[Icons]
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"

;checks if minimun requirements are met
[Code]
function IsDotNetDetected(version: string; service: cardinal): boolean;
// Indicates whether the specified version and service pack of the .NET Framework is installed.
//
// version -- Specify one of these strings for the required .NET Framework version:
//    'v1.1.4322'     .NET Framework 1.1
//    'v2.0.50727'    .NET Framework 2.0
//    'v3.0'          .NET Framework 3.0
//    'v3.5'          .NET Framework 3.5
//    'v4\Client'     .NET Framework 4.0 Client Profile
//    'v4\Full'       .NET Framework 4.0 Full Installation
//    'v4.7'          .NET Framework 4.5
//
// service -- Specify any non-negative integer for the required service pack level:
//    0               No service packs required
//    1, 2, etc.      Service pack 1, 2, etc. required
var
    key: string;
    install, release, serviceCount: cardinal;
    check47, success: boolean;
begin
    // .NET 4.5 installs as update to .NET 4.0 Full
    if version = 'v4.7' then begin
        version := 'v4\Full';
        check47 := true;
    end else
        check47 := false;

    // installation key group for all .NET versions
    key := 'SOFTWARE\Microsoft\NET Framework Setup\NDP\' + version;

    // .NET 3.0 uses value InstallSuccess in subkey Setup
    if Pos('v3.0', version) = 1 then begin
        success := RegQueryDWordValue(HKLM, key + '\Setup', 'InstallSuccess', install);
    end else begin
        success := RegQueryDWordValue(HKLM, key, 'Install', install);
    end;

    // .NET 4.0/4.5 uses value Servicing instead of SP
    if Pos('v4', version) = 1 then begin
        success := success and RegQueryDWordValue(HKLM, key, 'Servicing', serviceCount);
    end else begin
        success := success and RegQueryDWordValue(HKLM, key, 'SP', serviceCount);
    end;

    // .NET 4.5 uses additional value Release
    if check47 then begin
        success := success and RegQueryDWordValue(HKLM, key, 'Release', release);
        success := success and (release >= 378389);
    end;

    result := success and (install = 1) and (serviceCount >= service);
end;