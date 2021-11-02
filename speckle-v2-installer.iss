;defining variables
#define AppName      "Speckle@Arup v2 Bundle"
#define Autocad2021Version  GetFileVersion("ConnectorAutocadCivil\ConnectorAutocad2021\bin\Release\SpeckleConnectorAutocad.dll")
#define Autocad2022Version  GetFileVersion("ConnectorAutocadCivil\ConnectorAutocad2022\bin\Release\SpeckleConnectorAutocad.dll")
#define Civil2021Version  GetFileVersion("ConnectorAutocadCivil\ConnectorCivil2021\bin\Release\SpeckleConnectorCivil.dll")
#define Civil2022Version  GetFileVersion("ConnectorAutocadCivil\ConnectorCivil2022\bin\Release\SpeckleConnectorCivil.dll")

#define DynamoVersion  GetFileVersion("ConnectorDynamo\ConnectorDynamo\bin\Release\SpeckleConnectorDynamo.dll")
#define DynamoExtensionVersion  GetFileVersion("ConnectorDynamo\ConnectorDynamoExtension\bin\Release\SpeckleConnectorDynamoExtension.dll")
#define DynamoFunctionsVersion  GetFileVersion("ConnectorDynamo\ConnectorDynamoFunctions\bin\Release\SpeckleConnectorDynamoFunctions.dll")

#define GrasshopperVersion  GetFileVersion("ConnectorGrasshopper\ConnectorGrasshopper\bin\SpeckleConnectorGrasshopper.dll")

#define Revit2019Version  GetFileVersion("ConnectorRevit\ConnectorRevit2019\bin\Release\SpeckleConnectorRevit.dll")
#define Revit2020Version  GetFileVersion("ConnectorRevit\ConnectorRevit2020\bin\Release\SpeckleConnectorRevit.dll")
#define Revit2021Version  GetFileVersion("ConnectorRevit\ConnectorRevit2021\bin\Release\SpeckleConnectorRevit.dll")
#define Revit2022Version  GetFileVersion("ConnectorRevit\ConnectorRevit2022\bin\Release\SpeckleConnectorRevit.dll")

#define Rhino6Version  GetFileVersion("ConnectorRhino\ConnectorRhino6\bin\Release\SpeckleConnectorRhino.rhp")
#define Rhino7Version  GetFileVersion("ConnectorRhino\ConnectorRhino7\bin\Release\SpeckleConnectorRhino.rhp")

#define GSAVersion  GetFileVersion("ConnectorGSA\ConnectorGSA\bin\Release\ConnectorGSA.exe")

#define AppPublisher "Speckle@Arup"
#define AppURL       "https://speckle.arup.com"
#define SpeckleFolder "{localappdata}\Speckle"
#define AnalyticsFolder "{localappdata}\SpeckleAnalytics"      
#define AnalyticsFilename       "analytics.exe"

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
OutputBaseFilename=Speckle@ArupInstaller-v{#AppVersion}
SetupIconFile=ConnectionManager\SpeckleConnectionManagerUI\Assets\favicon.ico
Compression=lzma
SolidCompression=yes
ChangesAssociations=yes
PrivilegesRequired=lowest
VersionInfoVersion={#AppVersion}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl" 

[Messages]
WelcomeLabel2=This will install [name/ver] on your computer.%n%nIt is recommended that you close all other applications before continuing.%n%nThis installer is intended for Arup staff only, and will replace the Speckle Systems / AEC Systems Ltd Speckle Manager with Arup's own account manager.

[Components]
Name: dynamo; Description: Speckle for Dynamo 2.1+ - v{#DynamoVersion}; Types: full
Name: dynamoext; Description: Speckle for Dynamo Extensions - v{#DynamoExtensionVersion}; Types: full
Name: dynamofunc; Description: Speckle for Dynamo Functions - v{#DynamoFunctionsVersion}; Types: full
Name: rhino6; Description: Speckle for Rhino 6 - v{#Rhino6Version};  Types: full
Name: rhino7; Description: Speckle for Rhino 7 - v{#Rhino7Version};  Types: full
Name: gh; Description: Speckle for Grasshopper - v{#GrasshopperVersion};  Types: full
Name: revit19; Description: Speckle for Revit 2019 - v{#Revit2019Version};  Types: full
Name: revit20; Description: Speckle for Revit 2020 - v{#Revit2020Version};  Types: full
Name: revit21; Description: Speckle for Revit 2021 - v{#Revit2021Version};  Types: full
Name: revit22; Description: Speckle for Revit 2022 - v{#Revit2021Version};  Types: full
Name: autocad21; Description: Speckle for AutoCAD 2021 - v{#Autocad2021Version};  Types: full
Name: autocad22; Description: Speckle for AutoCAD 2022 - v{#Autocad2022Version};  Types: full
Name: civil21; Description: Speckle for AutoCADCivil 2021 - v{#Civil2021Version};  Types: full
Name: civil22; Description: Speckle for AutoCADCivil 2022 - v{#Civil2022Version};  Types: full
Name: gsa; Description: Speckle for Oasys GSA - v{#GSAVersion};  Types: full
Name: connectionmanager; Description: Speckle@Arup ConnectionManager - v{#AppVersion};  Types: full custom; Flags: fixed
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

;revit19
Source: "ConnectorRevit\ConnectorRevit2019\bin\Release\*"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2019\SpeckleRevit2\"; Flags: ignoreversion recursesubdirs; Components: revit19
Source: "ConnectorRevit\ConnectorRevit2019\bin\Release\SpeckleRevit2.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2019\"; Flags: ignoreversion recursesubdirs; Components: revit19
Source: "Objects\Converters\ConverterRevit\ConverterRevit2019\bin\Release\netstandard2.0\Objects.Converter.Revit2019.dll"; DestDir: "{userappdata}\Speckle\Kits\Objects\"; Flags: ignoreversion recursesubdirs; Components: revit19

;revit20
Source: "ConnectorRevit\ConnectorRevit2020\bin\Release\*"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2020\SpeckleRevit2\"; Flags: ignoreversion recursesubdirs; Components: revit20
Source: "ConnectorRevit\ConnectorRevit2020\bin\Release\SpeckleRevit2.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2020\"; Flags: ignoreversion recursesubdirs; Components: revit20
Source: "Objects\Converters\ConverterRevit\ConverterRevit2020\bin\Release\netstandard2.0\Objects.Converter.Revit2020.dll"; DestDir: "{userappdata}\Speckle\Kits\Objects\"; Flags: ignoreversion recursesubdirs; Components: revit20

;revit21
Source: "ConnectorRevit\ConnectorRevit2021\bin\Release\*"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2021\SpeckleRevit2\"; Flags: ignoreversion recursesubdirs; Components: revit21
Source: "ConnectorRevit\ConnectorRevit2021\bin\Release\SpeckleRevit2.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2021\"; Flags: ignoreversion recursesubdirs; Components: revit21
Source: "Objects\Converters\ConverterRevit\ConverterRevit2021\bin\Release\netstandard2.0\Objects.Converter.Revit2021.dll"; DestDir: "{userappdata}\Speckle\Kits\Objects\"; Flags: ignoreversion recursesubdirs; Components: revit21

;revit22
Source: "ConnectorRevit\ConnectorRevit2022\bin\Release\*"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2022\SpeckleRevit2\"; Flags: ignoreversion recursesubdirs; Components: revit22
Source: "ConnectorRevit\ConnectorRevit2022\bin\Release\SpeckleRevit2.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2022\"; Flags: ignoreversion recursesubdirs; Components: revit22
Source: "Objects\Converters\ConverterRevit\ConverterRevit2022\bin\Release\netstandard2.0\Objects.Converter.Revit2022.dll"; DestDir: "{userappdata}\Speckle\Kits\Objects\"; Flags: ignoreversion recursesubdirs; Components: revit22

;autocad21
Source: "ConnectorAutocadCivil\ConnectorAutocad2021\bin\Release\*"; DestDir: "{userappdata}\Autodesk\ApplicationPlugins\Speckle2AutoCAD2021\"; Flags: ignoreversion recursesubdirs; Components: autocad21
Source: "Objects\Converters\ConverterAutocadCivil\ConverterAutocad2021\bin\Release\netstandard2.0\Objects.Converter.Autocad2021.dll"; DestDir: "{userappdata}\Speckle\Kits\Objects\"; Flags: ignoreversion recursesubdirs; Components: autocad21

;autocad22
Source: "ConnectorAutocadCivil\ConnectorAutocad2022\bin\Release\*"; DestDir: "{userappdata}\Autodesk\ApplicationPlugins\Speckle2AutoCAD2022\"; Flags: ignoreversion recursesubdirs; Components: autocad22
Source: "Objects\Converters\ConverterAutocadCivil\ConverterAutocad2022\bin\Release\netstandard2.0\Objects.Converter.Autocad2022.dll"; DestDir: "{userappdata}\Speckle\Kits\Objects\"; Flags: ignoreversion recursesubdirs; Components: autocad22

;civil21
Source: "ConnectorAutocadCivil\ConnectorCivil2021\bin\Release\*"; DestDir: "{userappdata}\Autodesk\ApplicationPlugins\Speckle2Civil3D2021\"; Flags: ignoreversion recursesubdirs; Components: civil21
Source: "Objects\Converters\ConverterAutocadCivil\ConverterCivil2021\bin\Release\netstandard2.0\Objects.Converter.Civil2021.dll"; DestDir: "{userappdata}\Speckle\Kits\Objects\"; Flags: ignoreversion recursesubdirs; Components: civil21

;civil22
Source: "ConnectorAutocadCivil\ConnectorCivil2022\bin\Release\*"; DestDir: "{userappdata}\Autodesk\ApplicationPlugins\Speckle2Civil3D2022\"; Flags: ignoreversion recursesubdirs; Components: civil22
Source: "Objects\Converters\ConverterAutocadCivil\ConverterCivil2022\bin\Release\netstandard2.0\Objects.Converter.Civil2022.dll"; DestDir: "{userappdata}\Speckle\Kits\Objects\"; Flags: ignoreversion recursesubdirs; Components: civil22

;dynamo
Source: "ConnectorDynamo\ConnectorDynamo\bin\Release\*"; DestDir: "{userappdata}\Dynamo\Dynamo Core\2.11\packages\SpeckleDynamo2\bin\"; Flags: ignoreversion recursesubdirs; Components: dynamo
Source: "ConnectorDynamo\ConnectorDynamo\pkg.json"; DestDir: "{userappdata}\Dynamo\Dynamo Core\2.11\packages\SpeckleDynamo2\"; Flags: ignoreversion recursesubdirs; Components: dynamo
Source: "ConnectorDynamo\ConnectorDynamo\bin\Release\*"; DestDir: "{userappdata}\Dynamo\Dynamo Revit\2.0\packages\SpeckleDynamo2\bin\"; Flags: ignoreversion recursesubdirs; Components: dynamo
Source: "ConnectorDynamo\ConnectorDynamo\pkg.json"; DestDir: "{userappdata}\Dynamo\Dynamo Revit\2.0\packages\SpeckleDynamo2\"; Flags: ignoreversion recursesubdirs; Components: dynamo
Source: "ConnectorDynamo\ConnectorDynamo\bin\Release\*"; DestDir: "{userappdata}\Dynamo\Dynamo Revit\2.1\packages\SpeckleDynamo2\bin\"; Flags: ignoreversion recursesubdirs; Components: dynamo
Source: "ConnectorDynamo\ConnectorDynamo\pkg.json"; DestDir: "{userappdata}\Dynamo\Dynamo Revit\2.1\packages\SpeckleDynamo2\"; Flags: ignoreversion recursesubdirs; Components: dynamo
Source: "ConnectorDynamo\ConnectorDynamo\bin\Release\*"; DestDir: "{userappdata}\Dynamo\Dynamo Revit\2.5\packages\SpeckleDynamo2\bin\"; Flags: ignoreversion recursesubdirs; Components: dynamo
Source: "ConnectorDynamo\ConnectorDynamo\pkg.json"; DestDir: "{userappdata}\Dynamo\Dynamo Revit\2.5\packages\SpeckleDynamo2\"; Flags: ignoreversion recursesubdirs; Components: dynamo
Source: "ConnectorDynamo\ConnectorDynamo\bin\Release\*"; DestDir: "{userappdata}\Dynamo\Dynamo Revit\2.6\packages\SpeckleDynamo2\bin\"; Flags: ignoreversion recursesubdirs; Components: dynamo
Source: "ConnectorDynamo\ConnectorDynamo\pkg.json"; DestDir: "{userappdata}\Dynamo\Dynamo Revit\2.6\packages\SpeckleDynamo2\"; Flags: ignoreversion recursesubdirs; Components: dynamo
Source: "ConnectorDynamo\ConnectorDynamo\bin\Release\*"; DestDir: "{userappdata}\Dynamo\Dynamo Revit\2.10\packages\SpeckleDynamo2\bin\"; Flags: ignoreversion recursesubdirs; Components: dynamo
Source: "ConnectorDynamo\ConnectorDynamo\pkg.json"; DestDir: "{userappdata}\Dynamo\Dynamo Revit\2.10\packages\SpeckleDynamo2\"; Flags: ignoreversion recursesubdirs; Components: dynamo
Source: "Objects\Converters\ConverterDynamo\ConverterDynamoRevit\bin\Release\netstandard2.0\Objects.Converter.DynamoRevit.dll"; DestDir: "{userappdata}\Speckle\Kits\Objects\"; Flags: ignoreversion recursesubdirs; Components: dynamo
Source: "Objects\Converters\ConverterDynamo\ConverterDynamoRevit2021\bin\Release\netstandard2.0\Objects.Converter.DynamoRevit2021.dll"; DestDir: "{userappdata}\Speckle\Kits\Objects\"; Flags: ignoreversion recursesubdirs; Components: dynamo
Source: "Objects\Converters\ConverterDynamo\ConverterDynamoRevit2022\bin\Release\netstandard2.0\Objects.Converter.DynamoRevit2022.dll"; DestDir: "{userappdata}\Speckle\Kits\Objects\"; Flags: ignoreversion recursesubdirs; Components: dynamo
Source: "Objects\Converters\ConverterDynamo\ConverterDynamoSandbox\bin\Release\netstandard2.0\Objects.Converter.DynamoSandbox.dll"; DestDir: "{userappdata}\Speckle\Kits\Objects\"; Flags: ignoreversion recursesubdirs; Components: dynamo

;dynamoext
Source: "ConnectorDynamo\ConnectorDynamoExtension\bin\Release\*"; DestDir: "{userappdata}\Dynamo\Dynamo Core\2.11\packages\SpeckleDynamo2\bin\"; Flags: ignoreversion recursesubdirs; Components: dynamoext
Source: "ConnectorDynamo\ConnectorDynamoExtension\bin\Release\SpeckleConnectorDynamoExtension_ViewExtensionDefinition.xml"; DestDir: "{userappdata}\Dynamo\Dynamo Core\2.11\packages\SpeckleDynamo2\extra\"; Flags: ignoreversion recursesubdirs; Components: dynamoext
Source: "ConnectorDynamo\ConnectorDynamoExtension\bin\Release\*"; DestDir: "{userappdata}\Dynamo\Dynamo Revit\2.0\packages\SpeckleDynamo2\bin\"; Flags: ignoreversion recursesubdirs; Components: dynamoext
Source: "ConnectorDynamo\ConnectorDynamoExtension\bin\Release\SpeckleConnectorDynamoExtension_ViewExtensionDefinition.xml"; DestDir: "{userappdata}\Dynamo\Dynamo Revit\2.0\packages\SpeckleDynamo2\extra\"; Flags: ignoreversion recursesubdirs; Components: dynamoext
Source: "ConnectorDynamo\ConnectorDynamoExtension\bin\Release\*"; DestDir: "{userappdata}\Dynamo\Dynamo Revit\2.1\packages\SpeckleDynamo2\bin\"; Flags: ignoreversion recursesubdirs; Components: dynamoext
Source: "ConnectorDynamo\ConnectorDynamoExtension\bin\Release\SpeckleConnectorDynamoExtension_ViewExtensionDefinition.xml"; DestDir: "{userappdata}\Dynamo\Dynamo Revit\2.1\packages\SpeckleDynamo2\extra\"; Flags: ignoreversion recursesubdirs; Components: dynamoext
Source: "ConnectorDynamo\ConnectorDynamoExtension\bin\Release\*"; DestDir: "{userappdata}\Dynamo\Dynamo Revit\2.5\packages\SpeckleDynamo2\bin\"; Flags: ignoreversion recursesubdirs; Components: dynamoext
Source: "ConnectorDynamo\ConnectorDynamoExtension\bin\Release\SpeckleConnectorDynamoExtension_ViewExtensionDefinition.xml"; DestDir: "{userappdata}\Dynamo\Dynamo Revit\2.5\packages\SpeckleDynamo2\extra\"; Flags: ignoreversion recursesubdirs; Components: dynamoext
Source: "ConnectorDynamo\ConnectorDynamoExtension\bin\Release\*"; DestDir: "{userappdata}\Dynamo\Dynamo Revit\2.6\packages\SpeckleDynamo2\bin\"; Flags: ignoreversion recursesubdirs; Components: dynamoext
Source: "ConnectorDynamo\ConnectorDynamoExtension\bin\Release\SpeckleConnectorDynamoExtension_ViewExtensionDefinition.xml"; DestDir: "{userappdata}\Dynamo\Dynamo Revit\2.6\packages\SpeckleDynamo2\extra\"; Flags: ignoreversion recursesubdirs; Components: dynamoext
Source: "ConnectorDynamo\ConnectorDynamoExtension\bin\Release\*"; DestDir: "{userappdata}\Dynamo\Dynamo Revit\2.10\packages\SpeckleDynamo2\bin\"; Flags: ignoreversion recursesubdirs; Components: dynamoext
Source: "ConnectorDynamo\ConnectorDynamoExtension\bin\Release\SpeckleConnectorDynamoExtension_ViewExtensionDefinition.xml"; DestDir: "{userappdata}\Dynamo\Dynamo Revit\2.10\packages\SpeckleDynamo2\extra\"; Flags: ignoreversion recursesubdirs; Components: dynamoext

;dynamofunc
Source: "ConnectorDynamo\ConnectorDynamoFunctions\bin\Release\*"; DestDir: "{userappdata}\Dynamo\Dynamo Core\2.11\packages\SpeckleDynamo2\bin\"; Flags: ignoreversion recursesubdirs; Components: dynamofunc
Source: "ConnectorDynamo\ConnectorDynamoFunctions\bin\Release\*"; DestDir: "{userappdata}\Dynamo\Dynamo Revit\2.0\packages\SpeckleDynamo2\bin\"; Flags: ignoreversion recursesubdirs; Components: dynamofunc
Source: "ConnectorDynamo\ConnectorDynamoFunctions\bin\Release\*"; DestDir: "{userappdata}\Dynamo\Dynamo Revit\2.1\packages\SpeckleDynamo2\bin\"; Flags: ignoreversion recursesubdirs; Components: dynamofunc
Source: "ConnectorDynamo\ConnectorDynamoFunctions\bin\Release\*"; DestDir: "{userappdata}\Dynamo\Dynamo Revit\2.5\packages\SpeckleDynamo2\bin\"; Flags: ignoreversion recursesubdirs; Components: dynamofunc
Source: "ConnectorDynamo\ConnectorDynamoFunctions\bin\Release\*"; DestDir: "{userappdata}\Dynamo\Dynamo Revit\2.6\packages\SpeckleDynamo2\bin\"; Flags: ignoreversion recursesubdirs; Components: dynamofunc
Source: "ConnectorDynamo\ConnectorDynamoFunctions\bin\Release\*"; DestDir: "{userappdata}\Dynamo\Dynamo Revit\2.10\packages\SpeckleDynamo2\bin\"; Flags: ignoreversion recursesubdirs; Components: dynamofunc

;gsa
Source: "ConnectorGSA\ConnectorGSA\bin\Release\*"; DestDir: "{userappdata}\Oasys\SpeckleGSA\"; Flags: ignoreversion recursesubdirs; Components: gsa
Source: "Objects\Converters\ConverterGSA\ConverterGSA\bin\Release\Objects.Converter.GSA.dll"; DestDir: "{userappdata}\Speckle\Kits\Objects"; Flags: ignoreversion recursesubdirs; Components: gsa

;connectionmanager
Source: "ConnectionManager\SpeckleConnectionManager\bin\Release\net5.0\win10-x64\publish\*"; DestDir: "{userappdata}\speckle-connection-manager\"; Flags: ignoreversion recursesubdirs; Components: connectionmanager
Source: "ConnectionManager\SpeckleConnectionManagerUI\bin\Release\net5.0\win10-x64\publish\*"; DestDir: "{userappdata}\speckle-connection-manager-ui\"; Flags: ignoreversion recursesubdirs; Components: connectionmanager

;kits
Source: "Objects\Objects\bin\Release\netstandard2.0\Objects.dll"; DestDir: "{userappdata}\Speckle\Kits\Objects"; Flags: ignoreversion recursesubdirs; Components: kits

;analytics
Source: "Analytics\bin\Release\net461\win-x64\*"; DestDir: "{#AnalyticsFolder}"; Flags: ignoreversion recursesubdirs;

[InstallDelete]
Type: filesandordirs; Name: "{userappdata}\McNeel\Rhinoceros\6.0\Plug-ins\SpeckleRhino2 (8dd5f30b-a13d-4a24-abdc-3e05c8c87143)\*"
Type: filesandordirs; Name: "{userappdata}\McNeel\Rhinoceros\7.0\Plug-ins\SpeckleRhino2 (8dd5f30b-a13d-4a24-abdc-3e05c8c87143)\*"
Type: filesandordirs; Name: "{userappdata}\Grasshopper\Libraries\SpeckleGrasshopper2\*"
Type: filesandordirs; Name: "{userappdata}\Autodesk\Revit\Addins\2019\*"
Type: filesandordirs; Name: "{userappdata}\Autodesk\Revit\Addins\2020\*"
Type: filesandordirs; Name: "{userappdata}\Autodesk\Revit\Addins\2021\*"
Type: filesandordirs; Name: "{userappdata}\Autodesk\Revit\Addins\2022\*"
Type: filesandordirs; Name: "{userappdata}\Autodesk\ApplicationPlugins\Speckle2AutoCAD2021\*"
Type: filesandordirs; Name: "{userappdata}\Autodesk\ApplicationPlugins\Speckle2AutoCAD2022\*"
Type: filesandordirs; Name: "{userappdata}\Autodesk\ApplicationPlugins\Speckle2Civil3D2021\*"
Type: filesandordirs; Name: "{userappdata}\Autodesk\ApplicationPlugins\Speckle2Civil3D2022\*"
Type: filesandordirs; Name: "{userappdata}\Dynamo\Dynamo Core\2.11\packages\SpeckleDynamo2\*"
Type: filesandordirs; Name: "{userappdata}\Dynamo\Dynamo Revit\2.0\packages\SpeckleDynamo2\*"
Type: filesandordirs; Name: "{userappdata}\Dynamo\Dynamo Revit\2.1\packages\SpeckleDynamo2\*"
Type: filesandordirs; Name: "{userappdata}\Dynamo\Dynamo Revit\2.5\packages\SpeckleDynamo2\*"
Type: filesandordirs; Name: "{userappdata}\Dynamo\Dynamo Revit\2.6\packages\SpeckleDynamo2\*"
Type: filesandordirs; Name: "{userappdata}\Dynamo\*"
Type: filesandordirs; Name: "{userappdata}\Oasys\SpeckleGSA\*"
Type: filesandordirs; Name: "{userappdata}\Speckle\Kits\Objects\*"
Type: filesandordirs; Name: "{localappdata}\SpeckleAnalytics\*"

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

; Set url protocol to save auth details
Root: HKCU; Subkey: "Software\Classes\speckle"; ValueType: "string"; ValueData: "URL:speckle"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\speckle"; ValueType: "string"; ValueName: "URL Protocol"; ValueData: ""
Root: HKCU; Subkey: "Software\Classes\speckle\DefaultIcon"; ValueType: "string"; ValueData: "{userappdata}\speckle-connection-manager\SpeckleConnectionManager.exe,0"
Root: HKCU; Subkey: "Software\Classes\speckle\shell\open\command"; ValueType: "string"; ValueData: """{userappdata}\speckle-connection-manager\SpeckleConnectionManager.exe"" ""%1"""

[Icons]
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{userappdata}\Microsoft\Windows\Start Menu\Programs\Oasys\SpeckleGSAV2"; Filename: "{userappdata}\Oasys\SpeckleGSA\ConnectorGSA.exe";
Name: "{group}\Speckle@Arup AccountManager"; Filename: "{userappdata}\speckle-connection-manager-ui\SpeckleConnectionManagerUI.exe";

[Run]
Filename: "{userappdata}\speckle-connection-manager-ui\SpeckleConnectionManagerUI.exe"; Description: "Authenticate with the Speckle Server"; Flags: nowait postinstall skipifsilent
Filename: "{#AnalyticsFolder}\analytics.exe"; Parameters: "{#AppVersion} {#GetEnv('ENABLE_TELEMETRY_DOMAIN')}"; Description: "Send anonymous analytics to Arup. No project data or personally identifiable information will be sent."

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

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{localappdata}\Programs\speckle-manager\Uninstall SpeckleManager.exe'), '/currentuser /S', '', SW_HIDE, ewWaitUntilTerminated, ResultCode)
end;