#include "CreateZone.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Level.hpp"
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
	API_SubElement** /*marker*/,
	AttributeManager& /*attributeManager*/,
	LibpartImportManager& /*libpartImportManager*/,
	GS::Array<GS::UniString>& log) const
{
	GSErrCode err = NoError;
	
	Utility::SetElementType (element.header, API_ZoneID);
	err = Utility::GetBaseElementData (element, &memo, nullptr, log);
	if (err != NoError)
		return err;

	memoMask = APIMemoMask_Polygon;

	// The shape of the zone
	Objects::ElementShape zoneShape;

	if (os.Contains (ElementBase::Shape)) {
		os.Get (ElementBase::Shape, zoneShape);
		element.zone.poly.nSubPolys = zoneShape.SubpolyCount ();
		element.zone.poly.nCoords = zoneShape.VertexCount ();
		element.zone.poly.nArcs = zoneShape.ArcCount ();

		ACAPI_ELEMENT_MASK_SET (mask, API_ZoneType, poly.nSubPolys);
		ACAPI_ELEMENT_MASK_SET (mask, API_ZoneType, poly.nCoords);
		ACAPI_ELEMENT_MASK_SET (mask, API_ZoneType, poly.nArcs);

		zoneShape.SetToMemo (memo, Objects::ElementShape::MemoMainPolygon);
		
		element.zone.manual = true;
		ACAPI_ELEMENT_MASK_SET (mask, API_ZoneType, manual);
	}

	if (os.Contains (Room::Height)) {
		os.Get (Room::Height, element.zone.roomHeight);
		ACAPI_ELEMENT_MASK_SET (mask, API_ZoneType, roomHeight);
	}

	// The name and number of the zone
	if (os.Contains (Room::Name)) {
		GS::UniString str;
		os.Get (Room::Name, str);
		GS::ucscpy (element.zone.roomName, str.ToUStr ());
		ACAPI_ELEMENT_MASK_SET (mask, API_ZoneType, roomName);
	}
	if (os.Contains (Room::Number)) {
		GS::UniString str;
		os.Get (Room::Number, str);
		GS::ucscpy (element.zone.roomNoStr, str.ToUStr ());
		ACAPI_ELEMENT_MASK_SET (mask, API_ZoneType, roomNoStr);
	}

	// The floor index and level of the zone
	if (os.Contains (ElementBase::Level)) {
		GetStoryFromObjectState (os, zoneShape.Level (), element.header.floorInd, element.zone.roomBaseLev);
	} else {
		Utility::SetStoryLevelAndFloor (zoneShape.Level (), element.header.floorInd, element.zone.roomBaseLev);
	}
	ACAPI_ELEMENT_MASK_SET (mask, API_ZoneType, roomBaseLev);
	ACAPI_ELEMENT_MASK_SET (mask, API_Elem_Head, floorInd);

	return NoError;
}


GS::String CreateZone::GetName () const
{
	return CreateZoneCommandName;
}


} // namespace AddOnCommands
