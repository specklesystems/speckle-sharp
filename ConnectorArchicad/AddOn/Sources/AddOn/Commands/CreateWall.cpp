#include "CreateWall.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Point.hpp"
#include "Objects/Polyline.hpp"
#include "RealNumber.h"
#include "DGModule.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"
#include "OnExit.hpp"

namespace AddOnCommands
{
static GSErrCode CreateNewWall (API_Element& wall, API_ElementMemo& wallMemo)
{
	return ACAPI_Element_Create (&wall, &wallMemo);
}

static GSErrCode ModifyExistingWall (API_Element& wall, API_Element& mask, API_ElementMemo& wallMemo, GS::UInt64 memoMask)
{
	return ACAPI_Element_Change (&wall, &mask, &wallMemo, memoMask, true);
}

static GSErrCode GetWallFromObjectState (const GS::ObjectState& os,	API_Element& element, API_Element& wallMask, API_ElementMemo& wallMemo, GS::UInt64& memoMask)
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

	err = Utility::GetBaseElementData (element, &wallMemo);
	if (err != NoError)
		return err;

	memoMask = APIMemoMask_Polygon;

	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, poly.nSubPolys);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, poly.nCoords);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, poly.nArcs);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, begC);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, endC);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_Elem_Head, floorInd);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, bottomOffset);

	// Wall geometry
	
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

		if (element.wall.type == APIWtyp_Trapez || element.wall.type == APIWtyp_Poly) {
			element.wall.profileType = APISect_Normal;
			ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, profileType);
		}

		ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, type);
	}

	// The building material name of the wall
	GS::UniString attributeName;
	if (os.Contains (Wall::BuildingMaterialNameFieldName) &&
		element.wall.modelElemStructureType == API_BasicStructure) {

		os.Get (Wall::BuildingMaterialNameFieldName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_BuildingMaterialID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				element.wall.buildingMaterial = attribute.header.index;
		}
		ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, buildingMaterial);
	}

	// The composite name of the wall
	if (os.Contains (Wall::CompositeNameFieldName) &&
		element.wall.modelElemStructureType == API_CompositeStructure) {

		os.Get (Wall::CompositeNameFieldName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_CompWallID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				element.wall.composite = attribute.header.index;
		}
	}
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, composite);

	// The profile name of the wall
	if (os.Contains (Wall::ProfileNameFieldName) &&
		element.wall.modelElemStructureType == API_ProfileStructure) {

		os.Get (Wall::ProfileNameFieldName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_ProfileID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				element.wall.profileAttr = attribute.header.index;
		}
	}
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, profileAttr);

	// The arc angle of the wall
	if (os.Contains (Wall::ArcAngleFieldName))
		os.Get (Wall::ArcAngleFieldName, element.wall.angle);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, angle);

	// The shape of the wall
	Objects::ElementShape wallShape;

	if (os.Contains (ShapeFieldName)) {
		os.Get (ShapeFieldName, wallShape);
		element.wall.poly.nSubPolys = wallShape.SubpolyCount ();
		element.wall.poly.nCoords = wallShape.VertexCount ();
		element.wall.poly.nArcs = wallShape.ArcCount ();

		wallShape.SetToMemo (wallMemo);
	}

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

	// The height of the wall
	if (os.Contains (Wall::HeightFieldName))
		if (!os.Contains (FloorIndexFieldName)) // TODO: rethink a better way to check for this 
			element.wall.relativeTopStory = 0; // unlink top story
	os.Get (Wall::HeightFieldName, element.wall.height);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, height);

	// PolyWall Corners Can Change
	if (os.Contains (Wall::PolyCanChangeFieldName))
		os.Get (Wall::PolyCanChangeFieldName, element.wall.polyCanChange);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, polyCanChange);

	// Wall and stories relation

	// The top offset of the wall
	if (os.Contains (Wall::TopOffsetFieldName))
		os.Get (Wall::TopOffsetFieldName, element.wall.topOffset);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, topOffset);

	// The top linked story
	if (os.Contains (Wall::RelativeTopStoryIndexFieldName))
		os.Get (Wall::RelativeTopStoryIndexFieldName, element.wall.relativeTopStory);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, relativeTopStory);

	// Reference line parameters

	// The reference line location of the wall (outside, center, inside, core outside, core center or core inside)
	if (os.Contains (Wall::ReferenceLineLocationFieldName)) {
		GS::UniString referenceLineLocationName;
		os.Get (Wall::ReferenceLineLocationFieldName, referenceLineLocationName);

		GS::Optional<API_WallReferenceLineLocationID> type = referenceLineLocationNames.FindValue (referenceLineLocationName);
		if (type.HasValue ())
			element.wall.referenceLineLocation = type.Get ();

		ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, type);
	}

	// The offset of the wall’s base line from reference line
	if (os.Contains (Wall::ReferenceLineOffsetFieldName))
		os.Get (Wall::ReferenceLineOffsetFieldName, element.wall.offset);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, offset);

	// Distance between reference line and outside face of the wall
	if (os.Contains (Wall::OffsetFromOutsideFieldName))
		os.Get (Wall::OffsetFromOutsideFieldName, element.wall.offsetFromOutside);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, offsetFromOutside);

	// The index of the reference line beginning and end edge
	if (os.Contains (Wall::ReferenceLineStartIndexFieldName))
		os.Get (Wall::ReferenceLineStartIndexFieldName, element.wall.rLinInd);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, rLinInd);

	if (os.Contains (Wall::ReferenceLineEndIndexFieldName))
		os.Get (Wall::ReferenceLineEndIndexFieldName, element.wall.rLinEndInd);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, rLinEndInd);

	if (os.Contains (Wall::FlippedFieldName))
		os.Get (Wall::FlippedFieldName, element.wall.flipped);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, flipped);

	// Floor Plan and Section - Floor Plan Display

	// Story visibility
	Utility::ImportVisibility (os, element.wall.isAutoOnStoryVisibility, element.wall.visibility);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, isAutoOnStoryVisibility);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, visibility.showOnHome);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, visibility.showAllAbove);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, visibility.showAllBelow);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, visibility.showRelAbove);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, visibility.showRelBelow);

	// The display options (Projected, Projected with Overhead, Cut Only, Outlines Only, Overhead All or Symbolic Cut)
	if (os.Contains (Wall::DisplayOptionNameFieldName)) {
		GS::UniString displayOptionName;
		os.Get (Wall::DisplayOptionNameFieldName, displayOptionName);

		GS::Optional<API_ElemDisplayOptionsID> type = displayOptionNames.FindValue (displayOptionName);
		if (type.HasValue ())
			element.wall.displayOption = type.Get ();

		ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, type);
	}

	// Show projection (To Floor Plan Range, To Absolute Display Limit, Entire Element)
	if (os.Contains (Wall::ViewDepthLimitationNameFieldName)) {
		GS::UniString viewDepthLimitationName;
		os.Get (Wall::ViewDepthLimitationNameFieldName, viewDepthLimitationName);

		GS::Optional<API_ElemViewDepthLimitationsID> type = viewDepthLimitationNames.FindValue (viewDepthLimitationName);
		if (type.HasValue ())
			element.wall.viewDepthLimitation = type.Get ();

		ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, type);
	}

	// Floor Plan and Section - Cut Surfaces parameters

	// The pen index of wall’s cut contour line
	if (os.Contains (Wall::CutLinePenIndexFieldName))
		os.Get (Wall::CutLinePenIndexFieldName, element.wall.contPen);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, contPen);

	// The linetype name of wall’s cut contour line
	if (os.Contains (Wall::CutLinetypeNameFieldName)) {

		os.Get (Wall::CutLinetypeNameFieldName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				element.wall.contLtype = attribute.header.index;
		}
		ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, contLtype);
	}

	// Override cut fill pen
	if (os.Contains (Wall::OverrideCutFillPenIndexFieldName)) {
		element.wall.penOverride.overrideCutFillPen = true;
		os.Get (Wall::OverrideCutFillPenIndexFieldName, element.wall.penOverride.cutFillPen);
	}
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, penOverride.overrideCutFillPen);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, penOverride.cutFillPen);

	// Override cut fill background pen
	if (os.Contains (Wall::OverrideCutFillBackgroundPenIndexFieldName)) {
		element.wall.penOverride.overrideCutFillBackgroundPen = true;
		os.Get (Wall::OverrideCutFillBackgroundPenIndexFieldName, element.wall.penOverride.cutFillBackgroundPen);
	}
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, penOverride.overrideCutFillBackgroundPen);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, penOverride.cutFillBackgroundPen);

	// Floor Plan and Section - Outlines parameters

	// The pen index of wall’s uncut contour line
	if (os.Contains (Wall::UncutLinePenIndexFieldName))
		os.Get (Wall::UncutLinePenIndexFieldName, element.wall.contPen3D);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, contPen3D);

	// The linetype name of wall’s uncut contour line
	if (os.Contains (Wall::UncutLinetypeNameFieldName)) {

		os.Get (Wall::UncutLinetypeNameFieldName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				element.wall.belowViewLineType = attribute.header.index;
		}
		ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, belowViewLineType);
	}

	// The pen index of wall’s overhead contour line
	if (os.Contains (Wall::OverheadLinePenIndexFieldName))
		os.Get (Wall::OverheadLinePenIndexFieldName, element.wall.aboveViewLinePen);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, aboveViewLinePen);

	// The linetype name of wall’s overhead contour line
	if (os.Contains (Wall::OverheadLinetypeNameFieldName)) {

		os.Get (Wall::OverheadLinetypeNameFieldName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				element.wall.aboveViewLineType = attribute.header.index;
		}
		ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, aboveViewLineType);
	}

	// Model - Override Surfaces

	// The reference overridden material name
	if (os.Contains (Wall::ReferenceMaterialNameFieldName)) {
		element.wall.refMat.overridden = true;
		os.Get (Wall::ReferenceMaterialNameFieldName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_MaterialID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				element.wall.refMat.attributeIndex = attribute.header.index;
		}
	}
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, refMat.overridden);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, refMat.attributeIndex);

	// The index of the reference material start and end edge index
	if (os.Contains (Wall::ReferenceMaterialStartIndexFieldName))
		os.Get (Wall::ReferenceMaterialStartIndexFieldName, element.wall.refInd);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, refInd);

	if (os.Contains (Wall::ReferenceMaterialEndIndexFieldName))
		os.Get (Wall::ReferenceMaterialEndIndexFieldName, element.wall.refEndInd);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, refEndInd);

	// The opposite overridden material name
	if (os.Contains (Wall::OppositeMaterialNameFieldName)) {
		element.wall.oppMat.overridden = true;
		os.Get (Wall::OppositeMaterialNameFieldName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_MaterialID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				element.wall.oppMat.attributeIndex = attribute.header.index;
		}
	}
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, oppMat.overridden);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, oppMat.attributeIndex);

	// The index of the opposite material start and end edge index
	if (os.Contains (Wall::OppositeMaterialStartIndexFieldName))
		os.Get (Wall::OppositeMaterialStartIndexFieldName, element.wall.oppInd);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, oppInd);

	if (os.Contains (Wall::OppositeMaterialEndIndexFieldName))
		os.Get (Wall::OppositeMaterialEndIndexFieldName, element.wall.oppEndInd);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, oppEndInd);

	// The side overridden material name
	if (os.Contains (Wall::SideMaterialNameFieldName)) {
		element.wall.sidMat.overridden = true;
		os.Get (Wall::SideMaterialNameFieldName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_MaterialID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				element.wall.sidMat.attributeIndex = attribute.header.index;
		}
	}
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, sidMat.overridden);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, sidMat.attributeIndex);

	// The overridden materials are chained
	if (os.Contains (Wall::MaterialsChainedFieldName))
		os.Get (Wall::MaterialsChainedFieldName, element.wall.materialsChained);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, materialsChained);

	// The end surface of the wall is inherited from the adjoining wall
	if (os.Contains (Wall::InheritEndSurfaceFieldName))
		os.Get (Wall::InheritEndSurfaceFieldName, element.wall.inheritEndSurface);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, inheritEndSurface);

	// Align texture mapping to wall edges
	if (os.Contains (Wall::AlignTextureFieldName))
		os.Get (Wall::AlignTextureFieldName, element.wall.alignTexture);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, alignTexture);

	// Sequence
	if (os.Contains (Wall::SequenceFieldName))
		os.Get (Wall::SequenceFieldName, element.wall.sequence);
	ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, sequence);

	// Model - Log Details (log height, start with half log, surface of horizontal edges, log shape)
	Int32 beamFlag = 0;
	if (os.Contains (Wall::LogHeightFieldName)) {
		os.Get (Wall::LogHeightFieldName, element.wall.logHeight);

		if (os.Contains (Wall::StartWithHalfLogFieldName)) {
			bool startWithHalfLog = false;
			os.Get (Wall::StartWithHalfLogFieldName, startWithHalfLog);

			if (startWithHalfLog)
				beamFlag = beamFlag + APIWBeam_HalfLog;
		}

		if (os.Contains (Wall::SurfaceOfHorizontalEdgesFieldName)) {
			GS::UniString surfaceOfHorizontalEdgesName;
			os.Get (Wall::SurfaceOfHorizontalEdgesFieldName, surfaceOfHorizontalEdgesName);

			GS::Optional<Int32> type = beamFlagNames.FindValue (surfaceOfHorizontalEdgesName);
			if (type.HasValue ())
				beamFlag = beamFlag + type.Get ();
		}

		if (os.Contains (Wall::LogShapeFieldName)) {
			GS::UniString logShapeName;
			os.Get (Wall::LogShapeFieldName, logShapeName);

			GS::Optional<Int32> type = beamFlagNames.FindValue (logShapeName);
			if (type.HasValue ())
				beamFlag = beamFlag + type.Get ();
		}

		element.wall.beamFlags = beamFlag;
		ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, logHeight);
		ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, beamFlags);
	}

	// Model - Defines the relation of wall to zones (Zone Boundary, Reduce Zone Area Only, No Effect on Zones)
	if (os.Contains (Wall::WallRelationToZoneNameFieldName)) {
		GS::UniString wallRelationToZoneName;
		os.Get (Wall::WallRelationToZoneNameFieldName, wallRelationToZoneName);

		GS::Optional<API_ZoneRelID> type = wallRelationToZoneNames.FindValue (wallRelationToZoneName);
		if (type.HasValue ())
			element.wall.zoneRel = type.Get ();

		ACAPI_ELEMENT_MASK_SET (wallMask, API_WallType, type);
	}

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
			API_ElementMemo wallMemo{};
			GS::UInt64 memoMask = 0;
			GS::OnExit memoDisposer ([&wallMemo] { ACAPI_DisposeElemMemoHdls (&wallMemo); });

			GSErrCode err = GetWallFromObjectState (wallOs, wall, wallMask, wallMemo, memoMask);
			if (err != NoError)
				continue;

			bool wallExists = Utility::ElementExists (wall.header.guid);
			if (wallExists) {
				err = ModifyExistingWall (wall, wallMask, wallMemo, memoMask);
			} else {
				err = CreateNewWall (wall, wallMemo);
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
