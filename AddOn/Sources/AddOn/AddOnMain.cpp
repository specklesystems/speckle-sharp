#include "APIEnvir.h"
#include "ACAPinc.h"

#include "DGModule.hpp"
#include "Process.hpp"
#include "ResourceIds.hpp"

static const GSResID AddOnInfoID 			= ID_ADDON_INFO;
static const Int32 AddOnNameID 				= 1;
static const Int32 AddOnDescriptionID 		= 2;

static const short AddOnMenuID 				= ID_ADDON_MENU;
static const Int32 AddOnCommandID 			= 1;


static GS::UniString GetPlatformSpecificExecutable ()
{
#if defined (macintosh)
	return "";
#endif
#if defined (WINDOWS)

	static const char* FolderName = "ConnectorArchicad";
	static const char* FileName = "ConnectorArchicad.exe";

	IO::Location ownFileLoc;
	const auto err = ACAPI_GetOwnLocation (&ownFileLoc);
	if (err != NoError) {
		return "";
	}

	ownFileLoc.DeleteLastLocalName ();
	ownFileLoc.AppendToLocal (IO::Name (FolderName));
	ownFileLoc.AppendToLocal (IO::Name (FileName));

	GS::UniString executableStr;
	ownFileLoc.ToPath (&executableStr);

	return executableStr;

#endif
}

static GS::Array<GS::UniString> GetExecutableArguments ()
{
	UShort portNumber;
	const auto err = ACAPI_Goodies (APIAny_GetHttpConnectionPortID, &portNumber);

	if (err != NoError) {
		throw GS::IllegalArgumentException ();
	}

	return GS::Array<GS::UniString> { GS::ValueToUniString (portNumber) };
}

static GSErrCode MenuCommandHandler (const API_MenuParams* menuParams)
{
	switch (menuParams->menuItemRef.menuResID) {
	case AddOnMenuID:
		switch (menuParams->menuItemRef.itemIndex) {
		case AddOnCommandID:
		{
			try {
				const GS::UniString command = GetPlatformSpecificExecutable ();
				const GS::Array<GS::UniString> arguments = GetExecutableArguments ();

				GS::Process process = GS::Process::Create (command, arguments);
			}
			catch (GS::Exception&) {
				DG::ErrorAlert ("Error", "Can't start Speckle UI", "OK");
			}
		}
		break;
	}
	break;
	}

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
	return ACAPI_Install_MenuHandler (AddOnMenuID, MenuCommandHandler);
}

GSErrCode __ACENV_CALL FreeData (void)
{
	return NoError;
}
