#include "GetWallData.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Point.hpp"
#include "Objects/Polyline.hpp"
#include "RealNumber.h"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"

#define CHECK_BIT(var,pos) ( (((var) & (pos)) > 0 ) ? (true) : (false) )

namespace AddOnCommands {

static GS::ObjectState SerializeWallType (const API_WallType& wall, const API_ElementMemo& memo)
{
	GS::ObjectState os;

	// The identifier of the wall
	os.Add (ApplicationIdFieldName, APIGuidToString (wall.head.guid));

	// Wall geometry
	
	// The index of the wall's floor
	os.Add (FloorIndexFieldName, wall.head.floorInd);

	// Base offset of the wall
	os.Add (Wall::BaseOffsetFieldName, wall.bottomOffset);

	// The start and end points of the wall
	double z = Utility::GetStoryLevel (wall.head.floorInd) + wall.bottomOffset;
	os.Add (Wall::StartPointFieldName, Objects::Point3D (wall.begC.x, wall.begC.y, z));
	os.Add (Wall::EndPointFieldName, Objects::Point3D (wall.endC.x, wall.endC.y, z));

	// The profile type of the wall (straight, slanted, trapezoid or polygonal)
	os.Add (Wall::WallComplexityFieldName, profileTypeNames.Get (wall.profileType));

	// The structure type of the wall (basic, composite or profiled)
	os.Add (Wall::StructureFieldName, structureTypeNames.Get (wall.modelElemStructureType));

	// The geometry method of the wall (straight, trapezoid or polygonal)
	os.Add (Wall::GeometryMethodFieldName, wallTypeNames.Get (wall.type));

	// The building material name, composite name or the profile name of the wall
	API_Attribute attribute;
	switch (wall.modelElemStructureType) {
	case API_BasicStructure:
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_BuildingMaterialID;
		attribute.header.index = wall.buildingMaterial;

		if (NoError == ACAPI_Attribute_Get (&attribute))
			os.Add (Wall::BuildingMaterialNameFieldName, GS::UniString{attribute.header.name});
		break;
	case API_CompositeStructure:
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_CompWallID;
		attribute.header.index = wall.composite;

		if (NoError == ACAPI_Attribute_Get (&attribute))
			os.Add (Wall::CompositeNameFieldName, GS::UniString{attribute.header.name});
		break;
	case API_ProfileStructure:
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_ProfileID;
		attribute.header.index = wall.profileAttr;

		if (NoError == ACAPI_Attribute_Get (&attribute))
			os.Add (Wall::ProfileNameFieldName, GS::UniString{attribute.header.name});
		break;
	default:
		break;
	}

	// The arc angle of the wall
	if (abs (wall.angle) > EPS)
		os.Add (Wall::ArcAngleFieldName, wall.angle);

	// The polygon of the wall
	if (wall.type == APIWtyp_Poly)
		os.Add (ShapeFieldName, Objects::ElementShape (wall.poly, memo, z));

	// The thickness of the wall (first and second thickness for trapezoid walls)
	if (wall.type == APIWtyp_Trapez) {
		os.Add (Wall::FirstThicknessFieldName, wall.thickness);
		os.Add (Wall::SecondThicknessFieldName, wall.thickness1);
	} else {
		os.Add (Wall::ThicknessFieldName, wall.thickness);
	}

	// The outside slant angle of the wall
	os.Add (Wall::OutsideSlantAngleFieldName, wall.slantAlpha);

	// The inside slant angle of the wall
	if (wall.profileType == APISect_Trapez)
		os.Add (Wall::InsideSlantAngleFieldName, wall.slantBeta);

	// Height of the wall
	os.Add (Wall::HeightFieldName, wall.height);

	// PolyWall Corners Can Change
	if (wall.type == APIWtyp_Poly) {
		os.Add (Wall::PolyCanChangeFieldName, wall.polyCanChange);
	}

	// Wall and stories relation

	// Top offset of the wall
	os.Add (Wall::TopOffsetFieldName, wall.topOffset);

	// The top linked story
	os.Add (Wall::RelativeTopStoryIndexFieldName, wall.relativeTopStory);

	// Reference line parameters

	// The reference line location of the wall (outside, center, inside, core outside, core center or core inside)
	os.Add (Wall::ReferenceLineLocationFieldName, referenceLineLocationNames.Get (wall.referenceLineLocation));

	// The offset of the wall’s base line from reference line
	if (wall.type != APIWtyp_Poly &&
		wall.referenceLineLocation != APIWallRefLine_Center &&
		wall.referenceLineLocation != APIWallRefLine_CoreCenter) {
		os.Add (Wall::ReferenceLineOffsetFieldName, wall.offset);
	}

	// Distance between reference line and outside face of the wall
	os.Add (Wall::OffsetFromOutsideFieldName, wall.offsetFromOutside);

	// The index of the reference line beginning and end edge
	os.Add (Wall::ReferenceLineStartIndexFieldName, wall.rLinInd);
	os.Add (Wall::ReferenceLineEndIndexFieldName, wall.rLinEndInd);

	// Flip Wall on Reference Line
	os.Add (Wall::FlippedFieldName, wall.flipped);

	// Floor Plan and Section - Floor Plan Display

	// Show on Stories - Story visibility
	Utility::ExportVisibility (wall.isAutoOnStoryVisibility, wall.visibility, os);

	// The display options (Projected, Projected with Overhead, Cut Only, Outlines Only, Overhead All or Symbolic Cut)
	os.Add (Wall::DisplayOptionNameFieldName, displayOptionNames.Get (wall.displayOption));

	// Show projection (To Floor Plan Range, To Absolute Display Limit, Entire Element)
	os.Add (Wall::ViewDepthLimitationNameFieldName, viewDepthLimitationNames.Get (wall.viewDepthLimitation));

	// Floor Plan and Section - Cut Surfaces parameters

	// The pen index and linetype name of wall’s cut contour line
	if (wall.modelElemStructureType == API_BasicStructure) {
		os.Add (Wall::CutLinePenIndexFieldName, wall.contPen);

		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_LinetypeID;
		attribute.header.index = wall.contLtype;

		if (NoError == ACAPI_Attribute_Get (&attribute))
			os.Add (Wall::CutLinetypeNameFieldName, GS::UniString{attribute.header.name});
	}

	// Override cut fill pen
	if (wall.penOverride.overrideCutFillPen) {
		os.Add (Wall::OverrideCutFillPenIndexFieldName, wall.penOverride.cutFillPen);
	}

	// Override cut fill backgound pen
	if (wall.penOverride.overrideCutFillBackgroundPen) {
		os.Add (Wall::OverrideCutFillBackgroundPenIndexFieldName, wall.penOverride.cutFillBackgroundPen);
	}

	// Floor Plan and Section - Outlines parameters

	// The pen index of wall’s uncut contour line
	os.Add (Wall::UncutLinePenIndexFieldName, wall.contPen3D);

	// The linetype name of wall’s uncut contour line
	BNZeroMemory (&attribute, sizeof (API_Attribute));
	attribute.header.typeID = API_LinetypeID;
	attribute.header.index = wall.belowViewLineType;

	if (NoError == ACAPI_Attribute_Get (&attribute))
		os.Add (Wall::UncutLinetypeNameFieldName, GS::UniString{attribute.header.name});

	// The pen index of wall’s overhead contour line
	os.Add (Wall::OverheadLinePenIndexFieldName, wall.aboveViewLinePen);

	// The linetype name of wall’s overhead contour line
	BNZeroMemory (&attribute, sizeof (API_Attribute));
	attribute.header.typeID = API_LinetypeID;
	attribute.header.index = wall.aboveViewLineType;

	if (NoError == ACAPI_Attribute_Get (&attribute))
		os.Add (Wall::OverheadLinetypeNameFieldName, GS::UniString{attribute.header.name});

	// Model - Override Surfaces

	// The reference overridden material name, start and end edge index
	int countOverriddenMaterial = 0;
	if (wall.refMat.overridden) {
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_MaterialID;
		attribute.header.index = wall.refMat.attributeIndex;

		if (NoError == ACAPI_Attribute_Get (&attribute))
			countOverriddenMaterial = countOverriddenMaterial + 1;
		os.Add (Wall::ReferenceMaterialNameFieldName, GS::UniString{attribute.header.name});

		os.Add (Wall::ReferenceMaterialStartIndexFieldName, wall.refInd);
		os.Add (Wall::ReferenceMaterialEndIndexFieldName, wall.refEndInd);
	}

	// The opposite overridden material name, start and end edge index
	if (wall.oppMat.overridden) {
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_MaterialID;
		attribute.header.index = wall.oppMat.attributeIndex;

		if (NoError == ACAPI_Attribute_Get (&attribute))
			countOverriddenMaterial = countOverriddenMaterial + 1;
		os.Add (Wall::OppositeMaterialNameFieldName, GS::UniString{attribute.header.name});

		os.Add (Wall::OppositeMaterialStartIndexFieldName, wall.oppInd);
		os.Add (Wall::OppositeMaterialEndIndexFieldName, wall.oppEndInd);
	}

	// The side overridden material name
	if (wall.sidMat.overridden) {
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_MaterialID;
		attribute.header.index = wall.sidMat.attributeIndex;

		if (NoError == ACAPI_Attribute_Get (&attribute))
			countOverriddenMaterial = countOverriddenMaterial + 1;

		os.Add (Wall::SideMaterialNameFieldName, GS::UniString{attribute.header.name});
	}

	// The overridden materials are chained
	if (countOverriddenMaterial > 1) {
		os.Add (Wall::MaterialsChainedFieldName, wall.materialsChained);
	}

	// The end surface of the wall is inherited from the adjoining wall
	os.Add (Wall::InheritEndSurfaceFieldName, wall.inheritEndSurface);

	// Align texture mapping to wall edges
	os.Add (Wall::AlignTextureFieldName, wall.alignTexture);

	// Sequence
	os.Add (Wall::SequenceFieldName, wall.sequence);

	// Model - Log Details (log height, start with half log, surface of horizontal edges, log shape)
	if (abs (wall.logHeight) > EPS) {
		os.Add (Wall::LogHeightFieldName, wall.logHeight);
		os.Add (Wall::StartWithHalfLogFieldName, CHECK_BIT (wall.beamFlags, APIWBeam_HalfLog));

		if (CHECK_BIT (wall.beamFlags, APIWBeam_RefMater)) {
			os.Add (Wall::SurfaceOfHorizontalEdgesFieldName, beamFlagNames.Get (APIWBeam_RefMater));
		} else if (CHECK_BIT (wall.beamFlags, APIWBeam_OppMater)) {
			os.Add (Wall::SurfaceOfHorizontalEdgesFieldName, beamFlagNames.Get (APIWBeam_OppMater));
		}

		if (CHECK_BIT (wall.beamFlags, APIWBeam_QuadricLog)) {
			os.Add (Wall::LogShapeFieldName, beamFlagNames.Get (APIWBeam_QuadricLog));
		} else if (CHECK_BIT (wall.beamFlags, APIWBeam_Stretched)) {
			os.Add (Wall::LogShapeFieldName, beamFlagNames.Get (APIWBeam_Stretched));
		} else if (CHECK_BIT (wall.beamFlags, APIWBeam_RightLog)) {
			os.Add (Wall::LogShapeFieldName, beamFlagNames.Get (APIWBeam_RightLog));
		} else if (CHECK_BIT (wall.beamFlags, APIWBeam_LeftLog)) {
			os.Add (Wall::LogShapeFieldName, beamFlagNames.Get (APIWBeam_LeftLog));
		}
	}

	// Model - Defines the relation of wall to zones (Zone Boundary, Reduce Zone Area Only, No Effect on Zones)
	os.Add (Wall::WallRelationToZoneNameFieldName, wallRelationToZoneNames.Get (wall.zoneRel));

	// Does it have any embedded object?
	os.Add (Wall::HasDoorFieldName, wall.hasDoor);
	os.Add (Wall::HasWindowFieldName, wall.hasWindow);

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
	parameters.Get (ApplicationIdsFieldName, ids);
	GS::Array<API_Guid> elementGuids = ids.Transform<API_Guid> ([] (const GS::UniString& idStr) { return APIGuidFromString (idStr.ToCStr ()); });

	GS::ObjectState result;
	const auto& listAdder = result.AddList<GS::ObjectState> (WallsFieldName);
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