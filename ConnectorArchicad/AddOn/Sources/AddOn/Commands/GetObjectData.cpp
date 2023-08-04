#include "GetObjectData.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Level.hpp"
#include "Objects/Point.hpp"
#include "RealNumber.h"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"
using namespace FieldNames;

namespace AddOnCommands
{


GS::String GetObjectData::GetFieldName () const
{
	return FieldNames::Objects;
}


API_ElemTypeID GetObjectData::GetElemTypeID() const
{
	return API_ObjectID;
}


GS::ErrCode	GetObjectData::SerializeElementType (const API_Element& elem,
	const API_ElementMemo& memo,
	GS::ObjectState& os) const
{
	GS::ErrCode err = NoError;
	err = GetDataCommand::SerializeElementType (elem, memo, os);
	if (NoError != err)
		return err;

	API_StoryType story = Utility::GetStory (elem.object.head.floorInd);
	os.Add (ElementBase::Level, Objects::Level (story));

	double z = Utility::GetStoryLevel (elem.object.head.floorInd) + elem.object.level;
	os.Add (Object::pos, Objects::Point3D (elem.object.pos.x, elem.object.pos.x, z));
	
	return NoError;
}


GS::String GetObjectData::GetName () const
{
	return GetObjectDataCommandName;
}


} // namespace AddOnCommands
