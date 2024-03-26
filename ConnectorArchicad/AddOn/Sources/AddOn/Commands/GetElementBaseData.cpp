#include "GetElementBaseData.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Level.hpp"
#include "RealNumber.h"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"

using namespace FieldNames;


namespace AddOnCommands
{


GS::String GetElementBaseData::GetFieldName () const
{
	return Elements;
}


API_ElemTypeID GetElementBaseData::GetElemTypeID () const
{
	return API_ZombieElemID;
}


GS::UInt64 GetElementBaseData::GetMemoMask () const
{
	return 0;
}


GS::ErrCode GetElementBaseData::SerializeElementType (const API_Element& elem,
	const API_ElementMemo& /*memo*/,
	GS::ObjectState& os) const
{
	// Positioning
	API_StoryType story = Utility::GetStory (elem.header.floorInd);
	os.Add (ElementBase::Level, Objects::Level (story));

	return NoError;
}


GS::String GetElementBaseData::GetName () const
{
	return GetElementBaseDataCommandName;
}


} // namespace AddOnCommands
