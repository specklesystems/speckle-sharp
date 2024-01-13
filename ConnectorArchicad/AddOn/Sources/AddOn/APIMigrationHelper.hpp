#pragma once

#include <ACAPinc.h>

// Renamed functions in Archicad 27

#ifndef ServerMainVers_2700

// API functions
#define ACAPI_Element_GetElemTypeName ACAPI_Goodies_GetElemTypeName

#define ACAPI_Grouping_GetConnectedElements ACAPI_Element_GetConnectedElements

#define ACAPI_Selection_Select ACAPI_Element_Select
#define ACAPI_Selection_DeselectAll ACAPI_Element_DeselectAll

#define ACAPI_AddOnAddOnCommunication_CallFromEventLoop ACAPI_Command_CallFromEventLoop
#define ACAPI_AddOnAddOnCommunication_Test ACAPI_Command_Test
#define ACAPI_AddOnAddOnCommunication_Call ACAPI_Command_Call
#define ACAPI_AddOnAddOnCommunication_RegisterSupportedService ACAPI_Register_SupportedService
#define ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler ACAPI_Install_AddOnCommandHandler

#define ACAPI_UserInput_SetElementHighlight ACAPI_Interface_SetElementHighlight
#define ACAPI_UserInput_ClearElementHighlight ACAPI_Interface_ClearElementHighlight

#define ACAPI_Dialog_CreateAttributePicker ACAPI_Interface_CreateAttributePicker

#define ACAPI_Category_GetCategoryValue ACAPI_Element_GetCategoryValue
#define ACAPI_Category_GetCategoryValueDefault ACAPI_Element_GetCategoryValueDefault

#define ACAPI_LibraryPart_GetNum ACAPI_LibPart_GetNum
#define ACAPI_LibraryPart_Search ACAPI_LibPart_Search
#define ACAPI_LibraryPart_Create ACAPI_LibPart_Create
#define ACAPI_LibraryPart_NewSection ACAPI_LibPart_NewSection
#define ACAPI_LibraryPart_WriteSection ACAPI_LibPart_WriteSection
#define ACAPI_LibraryPart_EndSection ACAPI_LibPart_EndSection
#define ACAPI_LibraryPart_SetDetails_ParamDef ACAPI_LibPart_SetDetails_ParamDef
#define ACAPI_LibraryPart_Get ACAPI_LibPart_Get
#define ACAPI_LibraryPart_GetParams ACAPI_LibPart_GetParams
#define ACAPI_LibraryPart_GetSect_ParamDef ACAPI_LibPart_GetSect_ParamDef
#define ACAPI_LibraryPart_AddSection ACAPI_LibPart_AddSection
#define ACAPI_LibraryPart_Register ACAPI_LibPart_Register
#define ACAPI_LibraryPart_Save ACAPI_LibPart_Save

#define ACAPI_Sight_GetCurrentWindowSight ACAPI_3D_GetCurrentWindowSight

#define ACAPI_MenuItem_RegisterMenu ACAPI_Register_Menu
#define ACAPI_MenuItem_InstallMenuHandler ACAPI_Install_MenuHandler

#define ACAPI_ProjectOperation_CatchProjectEvent ACAPI_Notify_CatchProjectEvent

#define ACAPI_Notification_CatchSelectionChange ACAPI_Notify_CatchSelectionChange
#define ACAPI_Notification_RegisterEventHandler ACAPI_Notify_RegisterEventHandler

#define ACAPI_AddOnIntegration_RegisterFileType ACAPI_Register_FileType
#define ACAPI_AddOnIntegration_InstallFileTypeHandler3D ACAPI_Install_FileTypeHandler3D
#define ACAPI_AddOnIntegration_InstallModulCommandHandler ACAPI_Install_ModulCommandHandler

#define ACAPI_Licensing_GetProtectionMode ACAPI_Protection_GetProtectionMode
#define ACAPI_Licensing_GetSerialNumber ACAPI_Protection_GetSerialNumber
#define ACAPI_Licensing_GetBoxMask ACAPI_Protection_GetBoxMask
#define ACAPI_Licensing_GetConfigurationNumber ACAPI_Protection_GetConfigurationNumber
#define ACAPI_Licensing_GetNumberOfLicenses ACAPI_Protection_GetNumberOfLicenses
#define ACAPI_Licensing_GetPartnerId ACAPI_Protection_GetPartnerId

#define ACAPI_AddOnIntegration_RegisterModulDataHandler ACAPI_Register_ModulDataHandler
#define ACAPI_AddOnIntegration_InstallModulDataSaveOldFormatHandler ACAPI_Install_ModulDataSaveOldFormatHandler
#define ACAPI_AddOnIntegration_InstallModulDataMergeHandler ACAPI_Install_ModulDataMergeHandler

#define ACAPI_MenuItem_GetMenuItemFlags(par1, par2) (ACAPI_Interface (APIIo_GetMenuItemFlagsID, par1, par2))
#define ACAPI_MenuItem_SetMenuItemFlags(par1, par2) (ACAPI_Interface (APIIo_SetMenuItemFlagsID, par1, par2))
#define ACAPI_UserInput_GetPoint(par1, par2) (ACAPI_Interface (APIIo_GetPointID, par1, par2))

#define ACAPI_View_ShowAllIn3D() (ACAPI_Automate (APIDo_ShowAllIn3DID))
#define ACAPI_View_Rebuild() (ACAPI_Automate (APIDo_RebuildID))
#define ACAPI_View_ZoomToSelected() (ACAPI_Automate (APIDo_ZoomToSelectedID))
#define ACAPI_View_GoToView(par1) (ACAPI_Automate (APIDo_GoToViewID, (void*)(par1)))
#define ACAPI_ProjectOperation_Open(par1) (ACAPI_Automate (APIDo_OpenID, (void*)(par1)))
#define ACAPI_ProjectOperation_Save(par1, par2) (ACAPI_Automate (APIDo_SaveID, (void*)(par1), (void*)(par2)))
#define ACAPI_Window_ChangeWindow(par1) (ACAPI_Automate (APIDo_ChangeWindowID, (void*)(par1)))
#define ACAPI_View_ShowSelectionIn3D() (ACAPI_Automate (APIDo_ShowSelectionIn3DID))

#define ACAPI_LibraryManagement_OverwriteLibPart(par1) (ACAPI_Environment (APIEnv_OverwriteLibPartID, (void*)(par1), nullptr))
#define ACAPI_LibraryManagement_GetLibraries(par1, par2) (ACAPI_Environment (APIEnv_GetLibrariesID, (void*)(par1), (void*)(par2)))
#define ACAPI_LibraryManagement_SetLibraries(par1) (ACAPI_Environment (APIEnv_SetLibrariesID, (void*)(par1)))
#define ACAPI_ProjectOperation_Project(par1) (ACAPI_Environment (APIEnv_ProjectID, (void*)(par1)))
#define ACAPI_ProjectSetting_GetStorySettings(par1) (ACAPI_Environment (APIEnv_GetStorySettingsID, (void*)(par1)))
#define ACAPI_ProjectSetting_ChangeStorySettings(par1) (ACAPI_Environment (APIEnv_ChangeStorySettingsID, (void*)(par1)))
#define ACAPI_ProjectSetting_GetPreferences(par1, par2) (ACAPI_Environment (APIEnv_GetPreferencesID, (void*)(par1), (void*)(par2)))
#define ACAPI_ProjectSettings_GetSpecFolder(par1, par2) (ACAPI_Environment (APIEnv_GetSpecFolderID, (void*)(par1), (void*)(par2)))
#define ACAPI_SurveyPoint_GetSurveyPointTransformation(par1) (ACAPI_Environment (APIEnv_GetSurveyPointTransformationID, (void*)(par1)))
#define ACAPI_View_Get3DProjectionSets(par1) (ACAPI_Environment (APIEnv_Get3DProjectionSetsID, (void*)(par1)))
#define ACAPI_View_Change3DProjectionSets(par1) (ACAPI_Environment (APIEnv_Change3DProjectionSetsID, (void*)(par1)))
#define ACAPI_View_Get3DImageSets(par1) (ACAPI_Environment (APIEnv_Get3DImageSetsID, (void*)(par1)))
#define ACAPI_View_Get3DCuttingPlanes(par1) (ACAPI_Environment (APIEnv_Get3DCuttingPlanesID, (void*)(par1), nullptr))
#define ACAPI_View_Change3DCuttingPlanes(par1) (ACAPI_Environment (APIEnv_Change3DCuttingPlanesID, (void*)(par1), nullptr))

#define ACAPI_Window_GetCurrentWindow(par1) (ACAPI_Database (APIDb_GetCurrentWindowID, (void*)(par1)))
#define ACAPI_Database_GetCurrentDatabase(par1) (ACAPI_Database (APIDb_GetCurrentDatabaseID, (void*)(par1)))
#define ACAPI_Database_ChangeCurrentDatabase(par1) (ACAPI_Database (APIDb_ChangeCurrentDatabaseID, (void*)(par1)))

#define ACAPI_Command_GetHttpConnectionPort(par1) (ACAPI_Goodies (APIAny_GetHttpConnectionPortID, (void*)(par1)))
#define ACAPI_LibraryPart_GetMarkerParent(par1, par2) (ACAPI_Goodies_GetMarkerParent ((par1),(par2)))

#define ACAPI_Navigator_GetNavigatorItem(par1, par2) (ACAPI_Navigator (APINavigator_GetNavigatorItemID, (void*)(par1), (void*)(par2)))

// API_OverriddenAttribute
#define IsAPIOverriddenAttributeOverridden(par) (par.overridden)
#define GetAPIOverriddenAttribute(par) par.attributeIndex
#define SetAPIOverriddenAttribute(par1, par2) (par1.overridden = true, par1.attributeIndex = par2)
#define ResetAPIOverriddenAttribute(par) (par.overridden = false)
#define GetAPIOverriddenAttributeIndexField(par) par.attributeIndex
#define GetAPIOverriddenAttributeBoolField(par) par.overridden

// JSON
#include "DGModule.hpp"

#define JSONBase DG::JSBase
#define JSONObject DG::JSObject
#define JSONFunction DG::JSFunction
#define JSONValue DG::JSValue
#define JSONArray DG::JSArray

#else

// API_OverriddenAttribute
#define IsAPIOverriddenAttributeOverridden(par) (par.hasValue)
#define GetAPIOverriddenAttribute(par) par.value
#define SetAPIOverriddenAttribute(par1, par2) (par1 = par2)
#define ResetAPIOverriddenAttribute(par) (par = APINullValue)
#define GetAPIOverriddenAttributeIndexField(par) par
#define GetAPIOverriddenAttributeBoolField(par) par

// JSON
#include "JavascriptEngine.hpp"

#define JSONBase JS::Base
#define JSONObject JS::Object
#define JSONFunction JS::Function
#define JSONValue JS::Value
#define JSONArray JS::Array

#endif

// New constants in Archicad 27

#ifndef ServerMainVers_2700 

#define APIApplicationLayerAttributeIndex 1

#endif
