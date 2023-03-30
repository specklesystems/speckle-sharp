#include "GetObjectData.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
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
	const API_ElementMemo& /*memo*/,
	GS::ObjectState& os) const
{
	os.Add (ApplicationId, APIGuidToString (elem.object.head.guid));
	os.Add (FloorIndex, elem.object.head.floorInd);

	double z = Utility::GetStoryLevel (elem.object.head.floorInd) + elem.object.level;
	os.Add (Object::pos, Objects::Point3D (elem.object.pos.x, elem.object.pos.x, z));
	
	return NoError;
}


GS::String GetObjectData::GetName () const
{
	return GetObjectDataCommandName;
}


} // namespace AddOnCommands
