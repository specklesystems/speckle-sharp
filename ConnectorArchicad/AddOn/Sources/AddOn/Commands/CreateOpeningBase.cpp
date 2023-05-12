#include "CreateOpeningBase.hpp"
#include "DGModule.hpp"
#include "FieldNames.hpp"
#include "Utility.hpp"
#include "ExchangeManager.hpp"

namespace AddOnCommands
{


bool CreateOpeningBase::CheckEnvironment (const GS::ObjectState& os, API_Element& element)
{
	API_Element elem{};

	// Check if parent exist
	GS::String parentSpeckleId;
	os.Get (FieldNames::ElementBase::ParentElementId, parentSpeckleId);

	bool isConverted = false;
	API_Guid parentArchicadId;
	ExchangeManager::GetInstance ().GetState (parentSpeckleId, isConverted, parentArchicadId);

	if (!isConverted) {
		return false;
	}

	// This may be redundant, should test it
	bool parentExists = Utility::ElementExists (parentArchicadId);
	bool isParentWall = Utility::GetElementType (parentArchicadId) == API_WallID;
	bool isParentRoof = Utility::GetElementType (parentArchicadId) == API_RoofID;
	bool isParentShell = Utility::GetElementType (parentArchicadId) == API_ShellID;
	if (!(parentExists && (isParentWall || isParentRoof || isParentShell))) {
		return false;
	}

	GSErrCode err = NoError;

	if (isParentWall) {

#ifdef ServerMainVers_2600
		elem.header.type = API_WallID;
#else
		elem.header.typeID = API_WallID;
#endif

		if (elem.wall.type == APIWtyp_Poly) {
			return false;
		}
	} else if (isParentRoof) {

#ifdef ServerMainVers_2600
		elem.header.type = API_RoofID;
#else
		elem.header.typeID = API_RoofID;
#endif
	} else if (isParentShell) {

#ifdef ServerMainVers_2600
		elem.header.type = API_ShellID;
#else
		elem.header.typeID = API_ShellID;
#endif
	}

	elem.header.guid = parentArchicadId;

	err = ACAPI_Element_Get (&elem);
	if (err != NoError)
		return false;

	// Set its parent
	if (element.header.type == API_DoorID) {
		element.door.owner = parentArchicadId;
	} else if (element.header.type == API_WindowID) {
		element.window.owner = parentArchicadId;
	} else if (element.header.type == API_SkylightID) {
		element.skylight.owner = parentArchicadId;
	}

	return true;
}


GS::String CreateOpeningBase::GetFieldName () const
{
	return FieldNames::ElementBase::SubElements;
}


}
