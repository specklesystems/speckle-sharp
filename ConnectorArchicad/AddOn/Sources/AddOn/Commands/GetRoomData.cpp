#include "GetRoomData.hpp"
#include <locale>
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Level.hpp"
#include "Objects/Point.hpp"
#include "RealNumber.h"
#include "FieldNames.hpp"
#include "Polyline.hpp"
using namespace FieldNames;

namespace AddOnCommands
{


GS::String GetRoomData::GetFieldName () const
{
	return Zones;
}


API_ElemTypeID GetRoomData::GetElemTypeID () const
{
	return API_ZoneID;
}


GS::ErrCode GetRoomData::SerializeElementType (const API_Element& element,
	const API_ElementMemo& memo,
	GS::ObjectState& os) const
{
	// quantities
	API_ElementQuantity	quantity = {};
	API_Quantities		quantities = {};
	API_QuantitiesMask	mask;

	ACAPI_ELEMENT_QUANTITY_MASK_CLEAR (mask);
	ACAPI_ELEMENT_QUANTITY_MASK_SET (mask, zone, area);
	ACAPI_ELEMENT_QUANTITY_MASK_SET (mask, zone, volume);

	quantities.elements = &quantity;
	GSErrCode err = ACAPI_Element_GetQuantities (element.header.guid, nullptr, &quantities, &mask);
	if (err != NoError)
		return err;

	// The identifier of the room
	os.Add (ElementBase::ApplicationId, APIGuidToString (element.zone.head.guid));
	GS::UniString roomName = element.zone.roomName;
	GS::UniString roomNum = element.zone.roomNoStr;
	os.Add (Room::Name, roomName);
	os.Add (Room::Number, roomNum);

	// The index of the room's floor
	API_StoryType story = Utility::GetStory (element.zone.head.floorInd);
	os.Add (ElementBase::Level, Objects::Level (story));

	// The base point of the room
	double level = Utility::GetStoryLevel (element.zone.head.floorInd) + element.zone.roomBaseLev;
	os.Add (Room::BasePoint, Objects::Point3D (0, 0, level));
	os.Add (ElementBase::Shape, Objects::ElementShape (element.zone.poly, memo, Objects::ElementShape::MemoMainPolygon, level));

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
	os.Add (Room::Height, element.zone.roomHeight);
	os.Add (Room::Area, quantity.zone.area);
	os.Add (Room::Volume, quantity.zone.volume);


	return NoError;
}


GS::String GetRoomData::GetName () const
{
	return GetRoomDataCommandName;
}


}
