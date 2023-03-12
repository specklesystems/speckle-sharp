#include "GetRoomData.hpp"
#include <locale>
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Point.hpp"
#include "RealNumber.h"
#include "FieldNames.hpp"
#include "Polyline.hpp"
using namespace FieldNames;

namespace AddOnCommands
{
static GS::ObjectState SerializeRoomType (const API_ZoneType& zone, const API_ElementMemo& memo, const API_ElementQuantity& quantity)
{
	GS::ObjectState os;

	// The identifier of the room
	os.Add (ApplicationId, APIGuidToString (zone.head.guid));
	GS::UniString roomName = zone.roomName;
	GS::UniString roomNum = zone.roomNoStr;
	os.Add (Room::Name, roomName);
	os.Add (Room::Number, roomNum);

	// The index of the room's floor
	os.Add (FloorIndex, zone.head.floorInd);

	// The base point of the room
	double level = Utility::GetStoryLevel (zone.head.floorInd) + zone.roomBaseLev;
	os.Add (Room::BasePoint, Objects::Point3D (0, 0, level));
	os.Add (Shape, Objects::ElementShape (zone.poly, memo, level));

	// double polyCoords [zone.poly.nCoords*3];
	//
	// for (Int32 point_index = 0, coord_index = 0; point_index < zone.poly.nCoords; ++point_index, coord_index+=3)
	// {
	//     const API_Coord coord = (*memo.coords)[point_index];
	//     polyCoords[coord_index] = coord.x;
	//     polyCoords[coord_index+1] = coord.y;
	//     polyCoords[coord_index+2] = level;
	// }

	// Room Props
	os.Add (Room::Height, zone.roomHeight);
	os.Add (Room::Area, quantity.zone.area);
	os.Add (Room::Volume, quantity.zone.volume);


	return os;
}

GS::String GetRoomData::GetName () const
{
	return GetRoomDataCommandName
}

GS::ObjectState GetRoomData::Execute (const GS::ObjectState& parameters,
									  GS::ProcessControl& /*processControl*/) const
{
	GS::Array<GS::UniString> ids;
	parameters.Get (ApplicationIds, ids);
	GS::Array<API_Guid> elementGuids = ids.Transform<API_Guid> ([] (const GS::UniString& idStr) {
		return APIGuidFromString (idStr.ToCStr ());
	});

	GS::ObjectState result;
	const auto& listAdder = result.AddList<GS::ObjectState> (Zones);
	for (const API_Guid& guid : elementGuids) {
		// element and memo 
		API_Element element{};
		API_ElementMemo elementMemo{};
		element.header.guid = guid;

		GSErrCode err = ACAPI_Element_Get (&element);
		if (err != NoError) continue;

#ifdef ServerMainVers_2600
		if (element.header.type.typeID != API_ZoneID)
#else
		if (element.header.typeID != API_ZoneID)
#endif
			continue;
		err = ACAPI_Element_GetMemo (guid, &elementMemo, APIMemoMask_All);
		if (err != NoError) continue;

		// quantities
		API_ElementQuantity	quantity = {};
		API_Quantities		quantities = {};
		API_QuantitiesMask	mask;

		ACAPI_ELEMENT_QUANTITY_MASK_CLEAR (mask);
		ACAPI_ELEMENT_QUANTITY_MASK_SET (mask, zone, area);
		ACAPI_ELEMENT_QUANTITY_MASK_SET (mask, zone, volume);

		quantities.elements = &quantity;
		err = ACAPI_Element_GetQuantities (guid, nullptr, &quantities, &mask);
		if (err != NoError) continue;

		listAdder (SerializeRoomType (element.zone, elementMemo, quantity));
	}

	return result;
}
}
