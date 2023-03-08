#include "GetWallData.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Point.hpp"
#include "Objects/Polyline.hpp"
#include "RealNumber.h"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"
using namespace FieldNames;

#define CHECK_BIT(var,pos) ( (((var) & (pos)) > 0 ) ? (true) : (false) )

namespace AddOnCommands {

static GS::ObjectState SerializeWallType (const API_WallType& wall, const API_ElementMemo& memo)
{
	GS::ObjectState os;

	// The identifier of the wall
	os.Add (ApplicationId, APIGuidToString (wall.head.guid));

	// Wall geometry
	
	// The index of the wall's floor
	os.Add (FloorIndex, wall.head.floorInd);

	// Base offset of the wall
	os.Add (Wall::BaseOffset, wall.bottomOffset);

	// The start and end points of the wall
	double z = Utility::GetStoryLevel (wall.head.floorInd) + wall.bottomOffset;
	os.Add (Wall::StartPoint, Objects::Point3D (wall.begC.x, wall.begC.y, z));
	os.Add (Wall::EndPoint, Objects::Point3D (wall.endC.x, wall.endC.y, z));

	// The profile type of the wall (straight, slanted, trapezoid or polygonal)
	os.Add (Wall::WallComplexity, profileTypeNames.Get (wall.profileType));

	// The structure type of the wall (basic, composite or profiled)
	os.Add (Wall::Structure, structureTypeNames.Get (wall.modelElemStructureType));

	// The geometry method of the wall (straight, trapezoid or polygonal)
	os.Add (Wall::GeometryMethod, wallTypeNames.Get (wall.type));

	// The building material name, composite name or the profile name of the wall
	API_Attribute attribute;
	switch (wall.modelElemStructureType) {
	case API_BasicStructure:
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_BuildingMaterialID;
		attribute.header.index = wall.buildingMaterial;

		if (NoError == ACAPI_Attribute_Get (&attribute))
			os.Add (Wall::BuildingMaterialName, GS::UniString{attribute.header.name});
		break;
	case API_CompositeStructure:
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_CompWallID;
		attribute.header.index = wall.composite;

		if (NoError == ACAPI_Attribute_Get (&attribute))
			os.Add (Wall::CompositeName, GS::UniString{attribute.header.name});
		break;
	case API_ProfileStructure:
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_ProfileID;
		attribute.header.index = wall.profileAttr;

		if (NoError == ACAPI_Attribute_Get (&attribute))
			os.Add (Wall::ProfileName, GS::UniString{attribute.header.name});
		break;
	default:
		break;
	}

	// The arc angle of the wall
	if (abs (wall.angle) > EPS)
		os.Add (Wall::ArcAngle, wall.angle);

	// The polygon of the wall
	if (wall.type == APIWtyp_Poly)
		os.Add (Shape, Objects::ElementShape (wall.poly, memo, z));

	// The thickness of the wall (first and second thickness for trapezoid walls)
	if (wall.type == APIWtyp_Trapez) {
		os.Add (Wall::FirstThickness, wall.thickness);
		os.Add (Wall::SecondThickness, wall.thickness1);
	} else {
		os.Add (Wall::Thickness, wall.thickness);
	}

	// The outside slant angle of the wall
	os.Add (Wall::OutsideSlantAngle, wall.slantAlpha);

	// The inside slant angle of the wall
	if (wall.profileType == APISect_Trapez)
		os.Add (Wall::InsideSlantAngle, wall.slantBeta);

	// Height of the wall
	os.Add (Wall::Height, wall.height);

	// PolyWall Corners Can Change
	if (wall.type == APIWtyp_Poly) {
		os.Add (Wall::PolyCanChange, wall.polyCanChange);
	}

	// Wall and stories relation

	// Top offset of the wall
	os.Add (Wall::TopOffset, wall.topOffset);

	// The top linked story
	os.Add (Wall::RelativeTopStoryIndex, wall.relativeTopStory);

	// Reference line parameters

	// The reference line location of the wall (outside, center, inside, core outside, core center or core inside)
	os.Add (Wall::ReferenceLineLocation, referenceLineLocationNames.Get (wall.referenceLineLocation));

	// The offset of the wall’s base line from reference line
	if (wall.type != APIWtyp_Poly &&
		wall.referenceLineLocation != APIWallRefLine_Center &&
		wall.referenceLineLocation != APIWallRefLine_CoreCenter) {
		os.Add (Wall::ReferenceLineOffset, wall.offset);
	}

	// Distance between reference line and outside face of the wall
	os.Add (Wall::OffsetFromOutside, wall.offsetFromOutside);

	// The index of the reference line beginning and end edge
	os.Add (Wall::ReferenceLineStartIndex, wall.rLinInd);
	os.Add (Wall::ReferenceLineEndIndex, wall.rLinEndInd);

	// Flip Wall on Reference Line
	os.Add (Wall::Flipped, wall.flipped);

	// Floor Plan and Section - Floor Plan Display

	// Show on Stories - Story visibility
	Utility::ExportVisibility (wall.isAutoOnStoryVisibility, wall.visibility, os, ShowOnStories);

	// The display options (Projected, Projected with Overhead, Cut Only, Outlines Only, Overhead All or Symbolic Cut)
	os.Add (Wall::DisplayOptionName, displayOptionNames.Get (wall.displayOption));

	// Show projection (To Floor Plan Range, To Absolute Display Limit, Entire Element)
	os.Add (Wall::ViewDepthLimitationName, viewDepthLimitationNames.Get (wall.viewDepthLimitation));

	// Floor Plan and Section - Cut Surfaces parameters

	// The pen index and linetype name of wall’s cut contour line
	if (wall.modelElemStructureType == API_BasicStructure) {
		os.Add (Wall::CutLinePenIndex, wall.contPen);

		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_LinetypeID;
		attribute.header.index = wall.contLtype;

		if (NoError == ACAPI_Attribute_Get (&attribute))
			os.Add (Wall::CutLinetypeName, GS::UniString{attribute.header.name});
	}

	// Override cut fill pen
	if (wall.penOverride.overrideCutFillPen) {
		os.Add (Wall::OverrideCutFillPenIndex, wall.penOverride.cutFillPen);
	}

	// Override cut fill backgound pen
	if (wall.penOverride.overrideCutFillBackgroundPen) {
		os.Add (Wall::OverrideCutFillBackgroundPenIndex, wall.penOverride.cutFillBackgroundPen);
	}

	// Floor Plan and Section - Outlines parameters

	// The pen index of wall’s uncut contour line
	os.Add (Wall::UncutLinePenIndex, wall.contPen3D);

	// The linetype name of wall’s uncut contour line
	BNZeroMemory (&attribute, sizeof (API_Attribute));
	attribute.header.typeID = API_LinetypeID;
	attribute.header.index = wall.belowViewLineType;

	if (NoError == ACAPI_Attribute_Get (&attribute))
		os.Add (Wall::UncutLinetypeName, GS::UniString{attribute.header.name});

	// The pen index of wall’s overhead contour line
	os.Add (Wall::OverheadLinePenIndex, wall.aboveViewLinePen);

	// The linetype name of wall’s overhead contour line
	BNZeroMemory (&attribute, sizeof (API_Attribute));
	attribute.header.typeID = API_LinetypeID;
	attribute.header.index = wall.aboveViewLineType;

	if (NoError == ACAPI_Attribute_Get (&attribute))
		os.Add (Wall::OverheadLinetypeName, GS::UniString{attribute.header.name});

	// Model - Override Surfaces

	// The reference overridden material name, start and end edge index
	int countOverriddenMaterial = 0;
	if (wall.refMat.overridden) {
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_MaterialID;
		attribute.header.index = wall.refMat.attributeIndex;

		if (NoError == ACAPI_Attribute_Get (&attribute))
			countOverriddenMaterial = countOverriddenMaterial + 1;
		os.Add (Wall::ReferenceMaterialName, GS::UniString{attribute.header.name});

		os.Add (Wall::ReferenceMaterialStartIndex, wall.refInd);
		os.Add (Wall::ReferenceMaterialEndIndex, wall.refEndInd);
	}

	// The opposite overridden material name, start and end edge index
	if (wall.oppMat.overridden) {
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_MaterialID;
		attribute.header.index = wall.oppMat.attributeIndex;

		if (NoError == ACAPI_Attribute_Get (&attribute))
			countOverriddenMaterial = countOverriddenMaterial + 1;
		os.Add (Wall::OppositeMaterialName, GS::UniString{attribute.header.name});

		os.Add (Wall::OppositeMaterialStartIndex, wall.oppInd);
		os.Add (Wall::OppositeMaterialEndIndex, wall.oppEndInd);
	}

	// The side overridden material name
	if (wall.sidMat.overridden) {
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_MaterialID;
		attribute.header.index = wall.sidMat.attributeIndex;

		if (NoError == ACAPI_Attribute_Get (&attribute))
			countOverriddenMaterial = countOverriddenMaterial + 1;

		os.Add (Wall::SideMaterialName, GS::UniString{attribute.header.name});
	}

	// The overridden materials are chained
	if (countOverriddenMaterial > 1) {
		os.Add (Wall::MaterialsChained, wall.materialsChained);
	}

	// The end surface of the wall is inherited from the adjoining wall
	os.Add (Wall::InheritEndSurface, wall.inheritEndSurface);

	// Align texture mapping to wall edges
	os.Add (Wall::AlignTexture, wall.alignTexture);

	// Sequence
	os.Add (Wall::Sequence, wall.sequence);

	// Model - Log Details (log height, start with half log, surface of horizontal edges, log shape)
	if (abs (wall.logHeight) > EPS) {
		os.Add (Wall::LogHeight, wall.logHeight);
		os.Add (Wall::StartWithHalfLog, CHECK_BIT (wall.beamFlags, APIWBeam_HalfLog));

		if (CHECK_BIT (wall.beamFlags, APIWBeam_RefMater)) {
			os.Add (Wall::SurfaceOfHorizontalEdges, beamFlagNames.Get (APIWBeam_RefMater));
		} else if (CHECK_BIT (wall.beamFlags, APIWBeam_OppMater)) {
			os.Add (Wall::SurfaceOfHorizontalEdges, beamFlagNames.Get (APIWBeam_OppMater));
		}

		if (CHECK_BIT (wall.beamFlags, APIWBeam_QuadricLog)) {
			os.Add (Wall::LogShape, beamFlagNames.Get (APIWBeam_QuadricLog));
		} else if (CHECK_BIT (wall.beamFlags, APIWBeam_Stretched)) {
			os.Add (Wall::LogShape, beamFlagNames.Get (APIWBeam_Stretched));
		} else if (CHECK_BIT (wall.beamFlags, APIWBeam_RightLog)) {
			os.Add (Wall::LogShape, beamFlagNames.Get (APIWBeam_RightLog));
		} else if (CHECK_BIT (wall.beamFlags, APIWBeam_LeftLog)) {
			os.Add (Wall::LogShape, beamFlagNames.Get (APIWBeam_LeftLog));
		}
	}

	// Model - Defines the relation of wall to zones (Zone Boundary, Reduce Zone Area Only, No Effect on Zones)
	os.Add (Wall::WallRelationToZoneName, relationToZoneNames.Get (wall.zoneRel));

	// Does it have any embedded object?
	os.Add (Wall::HasDoor, wall.hasDoor);
	os.Add (Wall::HasWindow, wall.hasWindow);

	// End
	return os;
}

GS::String GetWallData::GetName () const
{
	return GetWallDataCommandName;
}

GS::ObjectState GetWallData::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const
{
	GS::Array<GS::UniString> ids;
	parameters.Get (ApplicationIds, ids);
	GS::Array<API_Guid> elementGuids = ids.Transform<API_Guid> ([] (const GS::UniString& idStr) { return APIGuidFromString (idStr.ToCStr ()); });

	GS::ObjectState result;
	const auto& listAdder = result.AddList<GS::ObjectState> (Walls);
	for (const API_Guid& guid : elementGuids) {
		API_Element element{};
		API_ElementMemo elementMemo{};

		element.header.guid = guid;

		GSErrCode err = ACAPI_Element_Get (&element);
		if (err != NoError) {
			continue;
		}

#ifdef ServerMainVers_2600
		if (element.header.type.typeID != API_WallID)
#else
		if (element.header.typeID != API_WallID)
#endif
		{
			continue;
		}

		err = ACAPI_Element_GetMemo (guid, &elementMemo);
		if (err != NoError) continue;

		listAdder (SerializeWallType (element.wall, elementMemo));
	}

	return result;
}

}