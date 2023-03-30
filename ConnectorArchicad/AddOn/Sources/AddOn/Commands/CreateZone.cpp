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
static GSErrCode CreateNewZone (API_Element& zone, API_ElementMemo& zoneMemo)
{
	return ACAPI_Element_Create (&zone, &zoneMemo);
}


static GSErrCode ModifyExistingZone (API_Element& zone, API_Element& mask, API_ElementMemo& zoneMemo,
									GS::UInt64 memoMask)
{
	return ACAPI_Element_Change (&zone, &mask, &zoneMemo, memoMask, true);
}


static GSErrCode GetZoneFromObjectState (const GS::ObjectState& os,
										API_Element& element,
										API_Element& mask,
										API_ElementMemo& zoneMemo,
										GS::UInt64& memoMask)
{
	GS::UniString guidString;
	os.Get (ApplicationId, guidString);
	element.header.guid = APIGuidFromString (guidString.ToCStr ());
#ifdef ServerMainVers_2600
	element.header.type.typeID = API_ZoneID;
#else
	element.header.typeID = API_ZoneID;
#endif

	GSErrCode err = Utility::GetBaseElementData (element, &zoneMemo);
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

		zoneShape.SetToMemo (zoneMemo);
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


GS::ObjectState CreateZone::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const
{
	GS::ObjectState result;

	GS::Array<GS::ObjectState> zones;
	parameters.Get (Zones, zones);

	const auto& listAdder = result.AddList<GS::UniString> (ApplicationIds);

	ACAPI_CallUndoableCommand ("CreateSpeckleZone", [&] () -> GSErrCode {
		for (const GS::ObjectState& zoneOs : zones) {
			API_Element zone{};
			API_Element zoneMask{};
			API_ElementMemo zoneMemo{};
			GS::UInt64 memoMask = 0;
			GS::OnExit memoDisposer ([&zoneMemo] { ACAPI_DisposeElemMemoHdls (&zoneMemo); });

			GSErrCode err = GetZoneFromObjectState (zoneOs, zone, zoneMask, zoneMemo, memoMask);
			if (err != NoError)
				continue;

			bool zoneExists = Utility::ElementExists (zone.header.guid);
			if (zoneExists)
				err = ModifyExistingZone (zone, zoneMask, zoneMemo, memoMask);
			else
				err = CreateNewZone (zone, zoneMemo);


			if (err == NoError) {
				GS::UniString elemId = APIGuidToString (zone.header.guid);
				listAdder (elemId);
			}
		}
		return NoError;
	});

	return result;
}
}
