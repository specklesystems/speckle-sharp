#include "APIEnvir.h"
#include "ACAPinc.h"
#include "APIMigrationHelper.hpp"

#include "DGModule.hpp"
#include "Process.hpp"
#include "ResourceIds.hpp"
#include "FileSystem.hpp"

#include "Commands/GetModelForElements.hpp"
#include "Commands/GetElementIds.hpp"
#include "Commands/GetElementTypes.hpp"
#include "Commands/GetWallData.hpp"
#include "Commands/GetDoorData.hpp"
#include "Commands/GetWindowData.hpp"
#include "Commands/GetBeamData.hpp"
#include "Commands/GetColumnData.hpp"
#include "Commands/GetElementBaseData.hpp"
#include "Commands/GetGridElementData.hpp"
#include "Commands/GetObjectData.hpp"
#include "Commands/GetOpeningData.hpp"
#include "Commands/GetSlabData.hpp"
#include "Commands/GetRoofData.hpp"
#include "Commands/GetShellData.hpp"
#include "Commands/GetSkylightData.hpp"
#include "Commands/GetProjectInfo.hpp"
#include "Commands/GetZoneData.hpp"
#include "Commands/CreateWall.hpp"
#include "Commands/CreateDoor.hpp"
#include "Commands/CreateWindow.hpp"
#include "Commands/CreateBeam.hpp"
#include "Commands/CreateColumn.hpp"
#include "Commands/CreateGridElement.hpp"
#include "Commands/CreateObject.hpp"
#include "Commands/CreateOpening.hpp"
#include "Commands/CreateRoof.hpp"
#include "Commands/CreateSkylight.hpp"
#include "Commands/CreateSlab.hpp"
#include "Commands/CreateShell.hpp"
#include "Commands/CreateZone.hpp"
#include "Commands/CreateDirectShape.hpp"
#include "Commands/SelectElements.hpp"
#include "Commands/FinishReceiveTransaction.hpp"


#define CHECKERROR(f) { GSErrCode err = (f); if (err != NoError) { return err; } }


static const GSResID AddOnInfoID = ID_ADDON_INFO;
static const Int32 AddOnNameID = 1;
static const Int32 AddOnDescriptionID = 2;

static const short AddOnMenuID = ID_ADDON_MENU;
static const Int32 AddOnCommandID = 1;


class AvaloniaProcessManager {
public:
	void Start ()
	{
		if (IsRunning ()) {
			return;
		}

		try {
			const GS::UniString command = GetPlatformSpecificExecutablePath ();
			const GS::Array<GS::UniString> arguments = GetExecutableArguments ();

			avaloniaProcess = GS::Process::Create (command, arguments);
		} catch (GS::Exception&) {
			DG::ErrorAlert ("Error", "Can't start Speckle UI", "OK");
		}
	}


	void Stop ()
	{
		if (!IsRunning ()) {
			return;
		}

		avaloniaProcess->Kill ();
	}


	bool IsRunning ()
	{
		return avaloniaProcess.HasValue () && !avaloniaProcess->IsTerminated ();
	}


private:
	GS::UniString GetPlatformSpecificExecutablePath ()
	{
		IO::Location ownFileLoc;
		auto err = ACAPI_GetOwnLocation (&ownFileLoc);
		if (err != NoError) {
			return "";
		}

#if defined (macintosh)
		static const char* ProductionConnector = "../../../Common/ConnectorArchicad/ConnectorArchicad.app/Contents/MacOS/ConnectorArchicad";
#else
		static const char* ProductionConnector = "../../../Common/ConnectorArchicad/ConnectorArchicad.exe";
#endif

		IO::Location location (ownFileLoc);
		location.AppendToLocal (IO::RelativeLocation (ProductionConnector));

		bool exist (false);
		err = IO::fileSystem.Contains (location, &exist);
		if (err != NoError || !exist) {
			location = ownFileLoc;

#if defined (macintosh)
#ifdef DEBUG
			static const char* DevelopmentConnector = "../../../../ConnectorArchicad/bin/Debug/net6.0/ConnectorArchicad";
#else
			static const char* DevelopmentConnector = "../../../../ConnectorArchicad/bin/Release/net6.0/ConnectorArchicad";
#endif
#else
#ifdef DEBUG
			static const char* DevelopmentConnector = "../../../../ConnectorArchicad/bin/Debug/net6.0/ConnectorArchicad.exe";
#else
			static const char* DevelopmentConnector = "../../../../ConnectorArchicad/bin/Release/net6.0/ConnectorArchicad.exe";
#endif
#endif

			location.AppendToLocal (IO::RelativeLocation (DevelopmentConnector));
		}

		GS::UniString executableStr;
		location.ToPath (&executableStr);

		return executableStr;
	}


	GS::Array<GS::UniString> GetExecutableArguments ()
	{
		UShort portNumber = 0;
		{
			const auto err = ACAPI_Command_GetHttpConnectionPort (&portNumber);

			if (err != NoError) {
				throw GS::IllegalArgumentException ();
			}
		}

		UShort archicadVersion = 0;
		{
			API_ServerApplicationInfo serverApplicationInfo;
			ACAPI_GetReleaseNumber (&serverApplicationInfo);

			archicadVersion = serverApplicationInfo.mainVersion;
		}

		return GS::Array<GS::UniString> { GS::ValueToUniString (portNumber), GS::ValueToUniString (archicadVersion) };
	}


	GS::Optional<GS::Process> avaloniaProcess;

};

static AvaloniaProcessManager avaloniaProcess;


static GSErrCode MenuCommandHandler (const API_MenuParams* menuParams)
{
	switch (menuParams->menuItemRef.menuResID) {
	case AddOnMenuID:
		switch (menuParams->menuItemRef.itemIndex) {
		case AddOnCommandID:
		{
			avaloniaProcess.Start ();
		}
		break;
		}
		break;
	}

	return NoError;
}


static GSErrCode RegisterAddOnCommands ()
{
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::GetModelForElements> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::GetElementIds> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::GetElementTypes> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::GetWallData> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::GetDoorData> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::GetWindowData> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::GetBeamData> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::GetColumnData> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::GetElementBaseData> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::GetGridElementData> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::GetObjectData> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::GetOpeningData> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::GetRoofData> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::GetShellData> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::GetSkylightData> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::GetSlabData> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::GetProjectInfo> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::GetZoneData> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::CreateWall> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::CreateDoor> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::CreateWindow> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::CreateBeam> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::CreateGridElement> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::CreateColumn> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::CreateObject> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::CreateOpening> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::CreateRoof> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::CreateShell> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::CreateSkylight> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::CreateSlab> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::CreateZone> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::CreateDirectShape> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::SelectElements> ()));
	CHECKERROR (ACAPI_AddOnAddOnCommunication_InstallAddOnCommandHandler (NewOwned<AddOnCommands::FinishReceiveTransaction> ()));

	return NoError;
}


API_AddonType __ACDLL_CALL CheckEnvironment (API_EnvirParams* envir)
{
	RSGetIndString (&envir->addOnInfo.name, AddOnInfoID, AddOnNameID, ACAPI_GetOwnResModule ());
	RSGetIndString (&envir->addOnInfo.description, AddOnInfoID, AddOnDescriptionID, ACAPI_GetOwnResModule ());

#ifdef DEBUG
	return APIAddon_Preload;
#else
	return APIAddon_Normal;
#endif
}


GSErrCode __ACDLL_CALL RegisterInterface (void)
{
	return ACAPI_MenuItem_RegisterMenu (AddOnMenuID, 0, MenuCode_Interoperability, MenuFlag_Default);
}


GSErrCode __ACENV_CALL Initialize (void)
{
	CHECKERROR (RegisterAddOnCommands ());

	return ACAPI_MenuItem_InstallMenuHandler (AddOnMenuID, MenuCommandHandler);
}


GSErrCode __ACENV_CALL FreeData (void)
{
	avaloniaProcess.Stop ();

	return NoError;
}
