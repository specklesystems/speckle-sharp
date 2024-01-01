#include "GetZoneData.hpp"
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


GS::String GetZoneData::GetFieldName () const
{
	return Zones;
}


API_ElemTypeID GetZoneData::GetElemTypeID () const
{
	return API_ZoneID;
}


GS::ErrCode GetZoneData::SerializeElementType (const API_Element& element,
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

	GS::UniString roomName = element.zone.roomName;
	GS::UniString roomNum = element.zone.roomNoStr;
	os.Add (Room::Name, roomName);
	os.Add (Room::Number, roomNum);

	// The index of the room's floor
	API_StoryType story = Utility::GetStory (element.zone.head.floorInd);
	os.Add (ElementBase::Level, Objects::Level (story));

	// The base point of the room
	double level = Utility::GetStoryLevel (element.zone.head.floorInd) + element.zone.roomBaseLev + element.zone.roomFlThick;
	{
		Geometry::Polygon2DData polygon;
		Utility::ConstructPoly2DDataFromElementMemo (memo, polygon);
		
		const Box2DData boundingBox = polygon.boundBox;
		GS::Array<Sector> sectors;
		bool res = Geometry::IntersectLineWithPolygon (polygon,
													   boundingBox.GetMidPoint(),
													   boundingBox.GetWidth() > boundingBox.GetHeight() ? Vector2D (1.0, 0.0) : Vector2D (0.0, 1.0),
													   &sectors);
		
		Geometry::FreePolygon2DData (&polygon);
		
		Objects::Point3D basePoint (0, 0, level);
		if (res && sectors.GetSize() > 0) {
			Sector sector = sectors[sectors.GetSize() / 2];
			basePoint = Objects::Point3D (sector.GetMidPoint ().GetX (), sector.GetMidPoint ().GetY (), level);
		}
				
		os.Add (Room::BasePoint, Objects::Point3D (basePoint.x, basePoint.y, basePoint.z));
	}
	os.Add (ElementBase::Shape, Objects::ElementShape (element.zone.poly, memo, Objects::ElementShape::MemoMainPolygon, level));

	// Room Props
	os.Add (Room::Height, element.zone.roomHeight);
	os.Add (Room::Area, quantity.zone.area);
	os.Add (Room::Volume, quantity.zone.volume);

	return NoError;
}


GS::String GetZoneData::GetName () const
{
	return GetRoomDataCommandName;
}


}
