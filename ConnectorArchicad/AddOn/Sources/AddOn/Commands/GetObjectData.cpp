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

static GS::ObjectState SerializeObjectType (const API_Element& elem, const API_ElementMemo& /*memo*/)
{
	GS::ObjectState os;

	os.Add (ApplicationId, APIGuidToString (elem.object.head.guid));
	os.Add (FloorIndex, elem.object.head.floorInd);

	double z = Utility::GetStoryLevel (elem.object.head.floorInd) + elem.object.level;
	os.Add (Object::pos, Objects::Point3D (elem.object.pos.x, elem.object.pos.x, z));
	
	return os;
}


GS::String GetObjectData::GetName () const
{
	return GetObjectDataCommandName;
}


GS::ObjectState GetObjectData::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const
{
	GS::Array<GS::UniString> ids;
	parameters.Get (ApplicationIds, ids);
	GS::Array<API_Guid> elementGuids = ids.Transform<API_Guid> ([] (const GS::UniString& idStr) { return APIGuidFromString (idStr.ToCStr ()); });

	GS::ObjectState result;
	const auto& listAdder = result.AddList<GS::ObjectState> (FieldNames::Objects);
	for (const API_Guid& guid : elementGuids) {
		API_Element element{};
		element.header.guid = guid;

		GSErrCode err = ACAPI_Element_Get (&element);
		if (err != NoError)
			continue;

		API_ElementMemo memo{};
		ACAPI_Element_GetMemo (guid, &memo);
		if (err != NoError)
			continue;

#ifdef ServerMainVers_2600
		if (element.header.type.typeID != API_ObjectID)
#else
		if (element.header.typeID != API_ObjectID)
#endif
		{
			continue;
		}

		listAdder (SerializeObjectType (element, memo));
	}

	return result;
}


} // namespace AddOnCommands
