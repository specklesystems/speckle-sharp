#include "CreateZone.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Polyline.hpp"
#include "FieldNames.hpp"
#include "AngleData.h"
#include "OnExit.hpp"
using namespace FieldNames;


namespace AddOnCommands
{


GS::String CreateZone::GetFieldName () const
{
	return Zones;
}


GS::UniString CreateZone::GetUndoableCommandName () const
{
	return "CreateSpeckleZone";
}


GSErrCode CreateZone::GetElementFromObjectState (const GS::ObjectState& os,
	API_Element& element,
	API_Element& mask,
	API_ElementMemo& memo,
	GS::UInt64& memoMask,
	AttributeManager& /*attributeManager*/,
	LibpartImportManager& /*libpartImportManager*/,
	API_SubElement** /*marker = nullptr*/) const

{
#ifdef ServerMainVers_2600
	element.header.type.typeID = API_ZoneID;
#else
	element.header.typeID = API_ZoneID;
#endif

	GSErrCode err = Utility::GetBaseElementData (element, &memo);
	if (err != NoError)
		return err;

	memoMask = APIMemoMask_Polygon;

	ACAPI_ELEMENT_MASK_SET (mask, API_ZoneType, poly.nSubPolys);
	ACAPI_ELEMENT_MASK_SET (mask, API_ZoneType, poly.nCoords);
	ACAPI_ELEMENT_MASK_SET (mask, API_ZoneType, poly.nArcs);
	ACAPI_ELEMENT_MASK_SET (mask, API_ZoneType, roomBaseLev);
	ACAPI_ELEMENT_MASK_SET (mask, API_Elem_Head, floorInd);

	// The shape of the zone
	Objects::ElementShape zoneShape;

	if (os.Contains (Shape)) {
		os.Get (Shape, zoneShape);
		element.zone.poly.nSubPolys = zoneShape.SubpolyCount ();
		element.zone.poly.nCoords = zoneShape.VertexCount ();
		element.zone.poly.nArcs = zoneShape.ArcCount ();

		zoneShape.SetToMemo (memo, Objects::ElementShape::MemoMainPolygon);
	}

	if (os.Contains (Room::Height)) {
		os.Get (Room::Height, element.zone.roomHeight);
		ACAPI_ELEMENT_MASK_SET (mask, API_ZoneType, roomHeight);
	}

	// The name and number of the zone
	if (os.Contains (Room::Name)) {
		os.Get (Room::Name, element.zone.roomName);
		ACAPI_ELEMENT_MASK_SET (mask, API_ZoneType, roomName);
	}
	if (os.Contains (Room::Number)) {
		os.Get (Room::Number, element.zone.roomNoStr);
		ACAPI_ELEMENT_MASK_SET (mask, API_ZoneType, roomNoStr);
	}

	// The floor index and level of the zone
	if (os.Contains (FloorIndex)) {
		os.Get (FloorIndex, element.header.floorInd);
		Utility::SetStoryLevel (zoneShape.Level (), element.header.floorInd, element.zone.roomBaseLev);
	} else {
		Utility::SetStoryLevelAndFloor (zoneShape.Level (), element.header.floorInd, element.zone.roomBaseLev);
	}
	return NoError;
}


GS::String CreateZone::GetName () const
{
	return CreateZoneCommandName
}


} // namespace AddOnCommands
