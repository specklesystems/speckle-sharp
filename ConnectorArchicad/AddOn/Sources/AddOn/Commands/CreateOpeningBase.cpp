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
		Utility::SetElementType (elem.header, API_WallID);

		if (elem.wall.type == APIWtyp_Poly) {
			return false;
		}
	} else if (isParentRoof) {
		Utility::SetElementType (elem.header, API_RoofID);
	} else if (isParentShell) {
		Utility::SetElementType (elem.header, API_ShellID);
	}

	elem.header.guid = parentArchicadId;

	err = ACAPI_Element_Get (&elem);
	if (err != NoError)
		return false;

	// Set its parent
	API_ElemTypeID elementType = Utility::GetElementType (elem.header);
	if (elementType == API_DoorID) {
		element.door.owner = parentArchicadId;
	} else if (elementType == API_WindowID) {
		element.window.owner = parentArchicadId;
	} else if (elementType == API_SkylightID) {
		element.skylight.owner = parentArchicadId;
	}

	return true;
}


GS::String CreateOpeningBase::GetFieldName () const
{
	return FieldNames::ElementBase::SubElements;
}


}
