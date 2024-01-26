#include "GetDoorData.hpp"
#include "GetOpeningBaseData.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Point.hpp"
#include "RealNumber.h"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"
using namespace FieldNames;


namespace AddOnCommands {


GS::String GetDoorData::GetFieldName () const
{
	return Doors;
}


API_ElemTypeID GetDoorData::GetElemTypeID () const
{
	return API_DoorID;
}


GS::ErrCode	GetDoorData::SerializeElementType (const API_Element& element,
	const API_ElementMemo& /*memo*/,
	GS::ObjectState& os) const
{
	os.Add (ElementBase::ParentElementId, APIGuidToString (element.door.owner));

	AddOnCommands::GetDoorWindowData<API_DoorType> (element.door, os);

	AddOnCommands::GetOpeningBaseData<API_DoorType> (element.door, os);

	return NoError;
}


GS::String GetDoorData::GetName () const
{
	return GetDoorCommandName;
}


} // namespace AddOnCommands
