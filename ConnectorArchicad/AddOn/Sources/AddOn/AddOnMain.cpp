#include "APIEnvir.h"
#include "ACAPinc.h"

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
#include "Commands/GetObjectData.hpp"
#include "Commands/GetSlabData.hpp"
#include "Commands/GetRoomData.hpp"
#include "Commands/GetProjectInfo.hpp"
#include "Commands/GetSubElementInfo.hpp"
#include "Commands/CreateWall.hpp"
#include "Commands/CreateDoor.hpp"
#include "Commands/CreateWindow.hpp"
#include "Commands/CreateBeam.hpp"
#include "Commands/CreateColumn.hpp"
#include "Commands/CreateObject.hpp"
#include "Commands/CreateSlab.hpp"
#include "Commands/CreateZone.hpp"
#include "Commands/CreateDirectShape.hpp"


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
#if defined (macintosh)
		static const char* FileName = "ConnectorArchicad";
#else
		static const char* FileName = "ConnectorArchicad.exe";
#endif
		static const char* FolderNameCommon = "Common";
		static const char* FolderName = "ConnectorArchicad";

		IO::Location ownFileLoc;
		auto err = ACAPI_GetOwnLocation (&ownFileLoc);
		if (err != NoError) {
			return "";
		}

		IO::Location location (ownFileLoc);
		location.DeleteLastLocalName ();
		location.DeleteLastLocalName ();
		location.DeleteLastLocalName ();
		location.AppendToLocal (IO::Name (FolderNameCommon));
		location.AppendToLocal (IO::Name (FolderName));
		location.AppendToLocal (IO::Name (FileName));

		bool exist (false);
		err = IO::fileSystem.Contains (location, &exist);
		if (err != NoError || !exist) {
			location = ownFileLoc;
			location.DeleteLastLocalName ();
			location.DeleteLastLocalName ();
			location.DeleteLastLocalName ();
			location.DeleteLastLocalName ();

			location.AppendToLocal (IO::Name (FolderName));
			location.AppendToLocal (IO::Name ("bin"));
			location.AppendToLocal (IO::Name ("Debug"));
			location.AppendToLocal (IO::Name ("net6.0"));
			location.AppendToLocal (IO::Name (FileName));
		}

		GS::UniString executableStr;
		location.ToPath (&executableStr);

		return executableStr;
	}

	GS::Array<GS::UniString> GetExecutableArguments ()
	{
		UShort portNumber = 0;
		{
			const auto err = ACAPI_Goodies (APIAny_GetHttpConnectionPortID, &portNumber);

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
	CHECKERROR (ACAPI_Install_AddOnCommandHandler (NewOwned<AddOnCommands::GetModelForElements> ()));
	CHECKERROR (ACAPI_Install_AddOnCommandHandler (NewOwned<AddOnCommands::GetElementIds> ()));
	CHECKERROR (ACAPI_Install_AddOnCommandHandler (NewOwned<AddOnCommands::GetElementTypes> ()));
	CHECKERROR (ACAPI_Install_AddOnCommandHandler (NewOwned<AddOnCommands::GetWallData> ()));
	CHECKERROR (ACAPI_Install_AddOnCommandHandler (NewOwned<AddOnCommands::GetDoorData> ()));
	CHECKERROR (ACAPI_Install_AddOnCommandHandler (NewOwned<AddOnCommands::GetWindowData> ()));
	CHECKERROR (ACAPI_Install_AddOnCommandHandler (NewOwned<AddOnCommands::GetSubElementInfo> ()));
	CHECKERROR (ACAPI_Install_AddOnCommandHandler (NewOwned<AddOnCommands::GetBeamData> ()));
	CHECKERROR (ACAPI_Install_AddOnCommandHandler (NewOwned<AddOnCommands::GetColumnData> ()));
	CHECKERROR (ACAPI_Install_AddOnCommandHandler (NewOwned<AddOnCommands::GetObjectData> ()));
	CHECKERROR (ACAPI_Install_AddOnCommandHandler (NewOwned<AddOnCommands::GetRoomData> ()));
	CHECKERROR (ACAPI_Install_AddOnCommandHandler (NewOwned<AddOnCommands::GetSlabData> ()));
	CHECKERROR (ACAPI_Install_AddOnCommandHandler (NewOwned<AddOnCommands::GetProjectInfo> ()));
	CHECKERROR (ACAPI_Install_AddOnCommandHandler (NewOwned<AddOnCommands::CreateWall> ()));
	CHECKERROR (ACAPI_Install_AddOnCommandHandler (NewOwned<AddOnCommands::CreateDoor> ()));
	CHECKERROR (ACAPI_Install_AddOnCommandHandler (NewOwned<AddOnCommands::CreateWindow> ()));
	CHECKERROR (ACAPI_Install_AddOnCommandHandler (NewOwned<AddOnCommands::CreateBeam> ()));
	CHECKERROR (ACAPI_Install_AddOnCommandHandler (NewOwned<AddOnCommands::CreateColumn> ()));
	CHECKERROR (ACAPI_Install_AddOnCommandHandler (NewOwned<AddOnCommands::CreateObject> ()));
	CHECKERROR (ACAPI_Install_AddOnCommandHandler (NewOwned<AddOnCommands::CreateSlab> ()));
	CHECKERROR (ACAPI_Install_AddOnCommandHandler (NewOwned<AddOnCommands::CreateZone> ()));
	CHECKERROR (ACAPI_Install_AddOnCommandHandler (NewOwned<AddOnCommands::CreateDirectShape> ()));

	return NoError;
}

API_AddonType __ACDLL_CALL CheckEnvironment (API_EnvirParams* envir)
{
	RSGetIndString (&envir->addOnInfo.name, AddOnInfoID, AddOnNameID, ACAPI_GetOwnResModule ());
	RSGetIndString (&envir->addOnInfo.description, AddOnInfoID, AddOnDescriptionID, ACAPI_GetOwnResModule ());

	return APIAddon_Normal;
}

GSErrCode __ACDLL_CALL RegisterInterface (void)
{
	return ACAPI_Register_Menu (AddOnMenuID, 0, MenuCode_Interoperability, MenuFlag_Default);
}

GSErrCode __ACENV_CALL Initialize (void)
{
	CHECKERROR (RegisterAddOnCommands ());

	return ACAPI_Install_MenuHandler (AddOnMenuID, MenuCommandHandler);
}

GSErrCode __ACENV_CALL FreeData (void)
{
	avaloniaProcess.Stop ();

	return NoError;
}
