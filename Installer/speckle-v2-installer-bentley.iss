;defining variables
#define AppName      "Speckle@Arup v2 Bentley Connectors"
#define MicroStationVersion  GetFileVersion("..\ConnectorMicroStation\ConnectorMicroStation\bin\Release\SpeckleConnectorMicroStation.dll")
#define OpenRoadsVersion  GetFileVersion("..\ConnectorMicroStation\ConnectorOpenRoads\bin\Release\SpeckleConnectorOpenRoads.dll")
#define OpenRailVersion  GetFileVersion("..\ConnectorMicroStation\ConnectorOpenRail\bin\Release\SpeckleConnectorOpenRail.dll")
#define OpenBuildingsVersion  GetFileVersion("..\ConnectorMicroStation\ConnectorOpenBuildings\bin\Release\SpeckleConnectorOpenBuildings.dll")
#define AppPublisher "Speckle@Arup"
#define AppURL       "https://speckle.arup.com"
#define SpeckleFolder "{localappdata}\Speckle"

[Setup]
AppId="1c19cd70-461d-4958-bec6-7270bb4fcdbd"
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
OutputBaseFilename=Speckle@ArupBentleyConnectors-v{#AppVersion}
SetupIconFile=..\Installer\ConnectionManager\SpeckleConnectionManagerUI\Assets\favicon.ico
Compression=lzma
SolidCompression=yes
ChangesAssociations=yes
PrivilegesRequired=lowest
VersionInfoVersion={#AppVersion}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Components]
Name: microstation; Description: Speckle for MicroStation CONNECT Edition Update 14 - v{#MicroStationVersion};  Types: full
Name: openroads; Description: Speckle for OpenRoads Designer CONNECT Edition 2020 R3 - v{#OpenRoadsVersion};  Types: full
Name: openrail; Description: Speckle for OpenRail Designer CONNECT Edition 2020 R3 - v{#OpenRailVersion};  Types: full
Name: openbuildings; Description: Speckle for OpenBuildings Designer CONNECT Edition Update 6 - v{#OpenBuildingsVersion};  Types: full
Name: kits; Description: Speckle Kits (for MicroStation, OpenRoads, OpenRail and OpenBuildings) - v{#AppVersion};  Types: full custom; Flags: fixed

[Types]
Name: "full"; Description: "Full installation"
Name: "custom"; Description: "Custom installation"; Flags: iscustom

[Dirs]
Name: "{app}"; Permissions: everyone-full

[Files]
;microstation
Source: "..\ConnectorMicroStation\ConnectorMicroStation\bin\Release\*"; DestDir: "{userappdata}\Bentley\MicroStation\Addins\SpeckleMicroStation2\"; Flags: ignoreversion recursesubdirs; Components: microstation
Source: "..\Objects\Converters\ConverterMicroStation\ConverterMicroStation\bin\Release\netstandard2.0\Objects.Converter.MicroStation.dll"; DestDir: "{userappdata}\Speckle\Kits\Objects"; Flags: ignoreversion recursesubdirs; Components: microstation
Source: "..\ConnectorMicroStation\ConnectorMicroStation\bin\Release\SpeckleMicrostation2.cfg"; DestDir: "{commonappdata}\Bentley\Microstation CONNECT Edition\Configuration\Organization"; Flags: ignoreversion recursesubdirs; Components: microstation

;openroads
Source: "..\ConnectorMicroStation\ConnectorOpenRoads\bin\Release\*"; DestDir: "{userappdata}\Bentley\OpenRoadsDesigner\Addins\SpeckleOpenRoads2\"; Flags: ignoreversion recursesubdirs; Components: openroads
Source: "..\Objects\Converters\ConverterMicroStation\ConverterOpenRoads\bin\Release\netstandard2.0\Objects.Converter.OpenRoads.dll"; DestDir: "{userappdata}\Speckle\Kits\Objects"; Flags: ignoreversion recursesubdirs; Components: openroads
Source: "..\ConnectorMicroStation\ConnectorOpenRoads\bin\Release\SpeckleOpenRoads2.cfg"; DestDir: "{commonappdata}\Bentley\OpenRoads Designer CE\Configuration\Organization"; Flags: ignoreversion recursesubdirs; Components: openroads

;openrail
Source: "..\ConnectorMicroStation\ConnectorOpenRail\bin\Release\*"; DestDir: "{userappdata}\Bentley\OpenRailDesigner\Addins\SpeckleOpenRail2\"; Flags: ignoreversion recursesubdirs; Components: openrail
Source: "..\Objects\Converters\ConverterMicroStation\ConverterOpenRail\bin\Release\netstandard2.0\Objects.Converter.OpenRail.dll"; DestDir: "{userappdata}\Speckle\Kits\Objects"; Flags: ignoreversion recursesubdirs; Components: openrail
Source: "..\ConnectorMicroStation\ConnectorOpenRail\bin\Release\SpeckleOpenRail2.cfg"; DestDir: "{commonappdata}\Bentley\OpenRail Designer CE\Configuration\Organization"; Flags: ignoreversion recursesubdirs; Components: openrail

;openbuildings
Source: "..\ConnectorMicroStation\ConnectorOpenBuildings\bin\Release\*"; DestDir: "{userappdata}\Bentley\OpenBuildingsDesigner\Addins\SpeckleOpenBuildings2\"; Flags: ignoreversion recursesubdirs; Components: openbuildings
Source: "..\Objects\Converters\ConverterMicroStation\ConverterOpenBuildings\bin\Release\netstandard2.0\Objects.Converter.OpenBuildings.dll"; DestDir: "{userappdata}\Speckle\Kits\Objects"; Flags: ignoreversion recursesubdirs; Components: openbuildings
Source: "..\ConnectorMicroStation\ConnectorOpenBuildings\bin\Release\SpeckleOpenBuildings2.cfg"; DestDir: "{commonappdata}\Bentley\OpenBuildings CONNECT Edition\Configuration\Organization"; Flags: ignoreversion recursesubdirs; Components: openbuildings

;kits
Source: "..\Objects\Objects\bin\Release\netstandard2.0\Objects.dll"; DestDir: "{userappdata}\Speckle\Kits\Objects"; Flags: ignoreversion recursesubdirs; Components: kits

[InstallDelete]
Type: filesandordirs; Name: "{userappdata}\Bentley\MicroStation\Addins\SpeckleMicroStation2\*"
Type: filesandordirs; Name: "{userappdata}\Bentley\OpenRoadsDesigner\Addins\SpeckleOpenRoads2\*"
Type: filesandordirs; Name: "{userappdata}\Bentley\OpenRailDesigner\Addins\SpeckleOpenRail2\*"
Type: filesandordirs; Name: "{userappdata}\Bentley\OpenBuildingsDesigner\Addins\SpeckleOpenBuildings2\*"
Type: files; Name: "{userappdata}\Speckle\Kits\Objects\Objects.Converter.MicroStation.dll"
Type: files; Name: "{userappdata}\Speckle\Kits\Objects\Objects.Converter.OpenRoads.dll"
Type: files; Name: "{userappdata}\Speckle\Kits\Objects\Objects.Converter.OpenRail.dll"
Type: files; Name: "{userappdata}\Speckle\Kits\Objects\Objects.Converter.OpenBuildings.dll"

[Icons]
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"