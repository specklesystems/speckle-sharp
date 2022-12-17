#include "CreateWall.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Point.hpp"
#include "RealNumber.h"
#include "DGModule.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"

namespace AddOnCommands
{
static GSErrCode CreateNewWall (API_Element& wall)
{
	return ACAPI_Element_Create (&wall, nullptr);
}

static GSErrCode ModifyExistingWall (API_Element& wall, API_Element& mask)
{
	return ACAPI_Element_Change (&wall, &mask, nullptr, 0, true);
}

static GSErrCode GetWallFromObjectState (const GS::ObjectState& os, API_Element& element, API_Element& wallMask)
{
	GSErrCode err;

	GS::UniString guidString;
	os.Get (ApplicationIdFieldName, guidString);
	element.header.guid = APIGuidFromString (guidString.ToCStr ());
#ifdef ServerMainVers_2600
	element.header.type.typeID = API_WallID;
#else
	element.header.typeID = API_WallID;
#endif

	err = Utility::GetBaseElementData (element, nullptr);
	if (err != NoError)
		return err;

	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, begC);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, endC);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_Elem_Head, floorInd);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, bottomOffset);

	// The start and end points of the wall
	Objects::Point3D startPoint;
	if (os.Contains (Wall::StartPointFieldName))
		os.Get (Wall::StartPointFieldName, startPoint);
	element.wall.begC = startPoint.ToAPI_Coord ();

	Objects::Point3D endPoint;
	if (os.Contains (Wall::EndPointFieldName))
		os.Get (Wall::EndPointFieldName, endPoint);
	element.wall.endC = endPoint.ToAPI_Coord ();

	// The floor index and bottom offset of the wall
	if (os.Contains (FloorIndexFieldName)) {
		os.Get (FloorIndexFieldName, element.header.floorInd);
		Utility::SetStoryLevel (startPoint.Z, element.header.floorInd, element.wall.bottomOffset);
	} else {
		Utility::SetStoryLevelAndFloor (startPoint.Z, element.header.floorInd, element.wall.bottomOffset);
	}

	// The arc angle of the wall
	if (os.Contains (Wall::ArcAngleFieldName))
		os.Get (Wall::ArcAngleFieldName, element.wall.angle);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, angle);

	// The height of the wall
	if (os.Contains (Wall::HeightFieldName))
		if (!os.Contains (FloorIndexFieldName)) // TODO: rethink a better way to check for this 
			element.wall.relativeTopStory = 0; // unlink top story
	os.Get (Wall::HeightFieldName, element.wall.height);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, height);

	if (os.Contains (Wall::FlippedFieldName))
		os.Get (Wall::FlippedFieldName, element.wall.flipped);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, flipped);


	// The profile type of the wall
	short profileType = 0;
	if (os.Contains (Wall::WallComplexityFieldName)) {
		GS::UniString wallComplexityName;
		os.Get (Wall::WallComplexityFieldName, wallComplexityName);
		GS::Optional<short> type = profileTypeNames.FindValue (wallComplexityName);
		if (type.HasValue ())
			profileType = type.Get ();

		element.wall.profileType = profileType;
		ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, profileType);
	}

	// The structure of the wall
	if (os.Contains (Wall::StructureFieldName)) {
		API_ModelElemStructureType structureType = API_BasicStructure;
		GS::UniString structureName;
		os.Get (Wall::StructureFieldName, structureName);

		GS::Optional<API_ModelElemStructureType> type = structureTypeNames.FindValue (structureName);
		if (type.HasValue ())
			structureType = type.Get ();

		element.wall.modelElemStructureType = structureType;

		if (structureType == API_BasicStructure) {
			element.wall.profileType = profileType == 0 ? APISect_Normal : APISect_Trapez;
		} else {
			if (structureType == API_CompositeStructure) {
				element.wall.profileType = profileType == 0 ? APISect_Normal : APISect_Trapez;
			} else {
				element.wall.profileType = APISect_Poly;
			}
		}

		ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, profileType);
		ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, modelElemStructureType);
	}

	// The geometry method of the wall
	if (os.Contains (Wall::GeometryMethodFieldName)) {
		GS::UniString wallGeometryName;
		os.Get (Wall::GeometryMethodFieldName, wallGeometryName);

		GS::Optional<API_WallTypeID> type = wallTypeNames.FindValue (wallGeometryName);
		if (type.HasValue ())
			element.wall.type = type.Get ();

		if (element.wall.type == APIWtyp_Trapez) {
			element.wall.profileType = APISect_Normal;
			ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, profileType);
		}

		ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, type);
	}

	// The building material index of the wall
	if (os.Contains (Wall::BuildingMaterialIndexFieldName) &&
	  element.wall.modelElemStructureType == API_BasicStructure) {
		os.Get (Wall::BuildingMaterialIndexFieldName, element.wall.buildingMaterial);
		ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, buildingMaterial);
	}

	// The composite index of the wall
	if (os.Contains (Wall::CompositeIndexFieldName) &&
	  element.wall.modelElemStructureType == API_CompositeStructure)
		os.Get (Wall::CompositeIndexFieldName, element.wall.composite);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, composite);

	// The profile index of the wall
	if (os.Contains (Wall::ProfileIndexFieldName) &&
	  element.wall.modelElemStructureType == API_ProfileStructure)
		os.Get (Wall::ProfileIndexFieldName, element.wall.profileAttr);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, profileAttr);

	// The thickness of the wall
	if (os.Contains (Wall::ThicknessFieldName))
		os.Get (Wall::ThicknessFieldName, element.wall.thickness);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, thickness);

	// The first thickness of the trapezoid wall
	if (os.Contains (Wall::FirstThicknessFieldName))
		os.Get (Wall::FirstThicknessFieldName, element.wall.thickness);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, thickness);

	// The second thickness of the trapezoid wall
	if (os.Contains (Wall::SecondThicknessFieldName))
		os.Get (Wall::SecondThicknessFieldName, element.wall.thickness1);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, thickness1);

	// The outside slant angle of the wall
	if (os.Contains (Wall::OutsideSlantAngleFieldName))
		os.Get (Wall::OutsideSlantAngleFieldName, element.wall.slantAlpha);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, slantAlpha);

	// The inside slant angle of the wall
	if (os.Contains (Wall::InsideSlantAngleFieldName))
		os.Get (Wall::InsideSlantAngleFieldName, element.wall.slantBeta);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, slantBeta);

	// Door & window
	if (os.Contains (Wall::HasDoorFieldName))
		os.Get (Wall::HasDoorFieldName, element.wall.hasDoor);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, hasDoor);

	if (os.Contains (Wall::HasWindowFieldName))
		os.Get (Wall::HasWindowFieldName, element.wall.hasWindow);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, hasWindow);

	return NoError;
}

GS::String CreateWall::GetName () const
{
	return CreateWallCommandName;
}

GS::ObjectState CreateWall::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const
{
	GS::ObjectState result;

	GS::Array<GS::ObjectState> walls;
	parameters.Get (WallsFieldName, walls);

	const auto& listAdder = result.AddList<GS::UniString> (ApplicationIdsFieldName);

	ACAPI_CallUndoableCommand ("CreateSpeckleWall", [&] () -> GSErrCode {
		for (const GS::ObjectState& wallOs : walls) {
			API_Element wall{};
			API_Element wallMask{};

			GSErrCode err = GetWallFromObjectState (wallOs, wall, wallMask);
			if (err != NoError)
				continue;

			bool wallExists = Utility::ElementExists (wall.header.guid);
			if (wallExists) {
				err = ModifyExistingWall (wall, wallMask);
			} else {
				err = CreateNewWall (wall);
			}

			if (err == NoError) {
				GS::UniString elemId = APIGuidToString (wall.header.guid);
				listAdder (elemId);
			}
		}

		return NoError;
	});

	return result;
}
}
