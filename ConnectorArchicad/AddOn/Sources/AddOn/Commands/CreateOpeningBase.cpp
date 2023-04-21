#include "CreateOpeningBase.hpp"
#include "DGModule.hpp"
#include "FieldNames.hpp"
#include "Utility.hpp"
#include "ExchangeManager.hpp"

namespace AddOnCommands
{


bool CreateOpeningBase::CheckEnvironment (const GS::ObjectState& objectState, API_Element& element)
{
	API_Element wall{};

#ifdef ServerMainVers_2600
	wall.header.type = API_WallID;
#else
	wall.header.typeID = API_WallID;
#endif

	// Check if parent exist
	GS::String parentSpeckleId;
	objectState.Get (FieldNames::ParentElementId, parentSpeckleId);

	bool isConverted = false;
	API_Guid parentArchicadId;
	ExchangeManager::GetInstance ().GetState (parentSpeckleId, isConverted, parentArchicadId);

	if (!isConverted) {
		return false;
	}

	wall.header.guid = parentArchicadId;

	// This may be redundant, should test it
	bool parentExists = Utility::ElementExists (parentArchicadId);
	bool isParentWall = Utility::GetElementType (parentArchicadId) == API_WallID;
	if (!(parentExists && isParentWall)) {
		return false;
	}

	GSErrCode err = ACAPI_Element_Get (&wall);
	if (err != NoError) {
		return false;
	}

	if (wall.wall.type == APIWtyp_Poly) {
		return false;
	}

	// Set its parent
	element.door.owner = parentArchicadId;

	return true;
}


GS::String CreateOpeningBase::GetFieldName () const
{
	return FieldNames::SubElements;
}


}
