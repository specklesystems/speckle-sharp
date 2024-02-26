#include "CreateWall.hpp"
#include "APIMigrationHelper.hpp"
#include "CommandHelpers.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Level.hpp"
#include "Objects/Point.hpp"
#include "Objects/Polyline.hpp"
#include "RealNumber.h"
#include "DGModule.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"
#include "OnExit.hpp"
using namespace FieldNames;

namespace AddOnCommands
{


GS::String CreateWall::GetFieldName () const
{
	return FieldNames::Walls;
}


GS::UniString CreateWall::GetUndoableCommandName () const
{
	return "CreateSpeckleWall";
}


GSErrCode CreateWall::GetElementFromObjectState (const GS::ObjectState& os,
	API_Element& element,
	API_Element& elementMask,
	API_ElementMemo& memo,
	GS::UInt64& memoMask,
	API_SubElement** /*marker*/,
	AttributeManager& /*attributeManager*/,
	LibpartImportManager& /*libpartImportManager*/,
	GS::Array<GS::UniString>& log) const
{
	GSErrCode err;

	Utility::SetElementType (element.header, API_WallID);
	err = Utility::GetBaseElementData (element, &memo, nullptr, log);
	if (err != NoError)
		return err;

	err = GetElementBaseFromObjectState (os, element, elementMask);
	if (err != NoError)
		return err;

	memoMask = APIMemoMask_Polygon;

	// Wall geometry

	// The start and end points of the wall
	Objects::Point3D startPoint;
	if (os.Contains (Wall::StartPoint)) {
		os.Get (Wall::StartPoint, startPoint);
		element.wall.begC = startPoint.ToAPI_Coord ();
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, begC);
	}

	Objects::Point3D endPoint;
	if (os.Contains (Wall::EndPoint)) {
		os.Get (Wall::EndPoint, endPoint);
		element.wall.endC = endPoint.ToAPI_Coord ();
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, endC);
	}

	// The floor index and bottom offset of the wall
	if (os.Contains (ElementBase::Level)) {
		GetStoryFromObjectState (os, startPoint.z, element.header.floorInd, element.wall.bottomOffset);
	} else {
		Utility::SetStoryLevelAndFloor (startPoint.z, element.header.floorInd, element.wall.bottomOffset);
	}
	ACAPI_ELEMENT_MASK_SET (elementMask, API_Elem_Head, floorInd);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, bottomOffset);

	// The profile type of the wall
	short profileType = 0;
	if (os.Contains (Wall::WallComplexity)) {
		GS::UniString wallComplexityName;
		os.Get (Wall::WallComplexity, wallComplexityName);
		GS::Optional<short> type = profileTypeNames.FindValue (wallComplexityName);
		if (type.HasValue ())
			profileType = type.Get ();

		element.wall.profileType = profileType;
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, profileType);
	}

	// The structure of the wall
	if (os.Contains (Wall::Structure)) {
		API_ModelElemStructureType structureType = API_BasicStructure;
		GS::UniString structureName;
		os.Get (Wall::Structure, structureName);

		GS::Optional<API_ModelElemStructureType> type = structureTypeNames.FindValue (structureName);
		if (type.HasValue ())
			structureType = type.Get ();

		element.wall.modelElemStructureType = structureType;

		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, profileType);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, modelElemStructureType);
	}

	// The geometry method of the wall
	if (os.Contains (Wall::GeometryMethod)) {
		GS::UniString wallGeometryName;
		os.Get (Wall::GeometryMethod, wallGeometryName);

		GS::Optional<API_WallTypeID> type = wallTypeNames.FindValue (wallGeometryName);
		if (type.HasValue ())
			element.wall.type = type.Get ();

		if (element.wall.type == APIWtyp_Trapez || element.wall.type == APIWtyp_Poly) {
			element.wall.profileType = APISect_Normal;
			ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, profileType);
		}

		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, type);
	}

	// The building material name of the wall
	GS::UniString attributeName;
	if (os.Contains (Wall::BuildingMaterialName) &&
		element.wall.modelElemStructureType == API_BasicStructure) {

		os.Get (Wall::BuildingMaterialName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_BuildingMaterialID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				element.wall.buildingMaterial = attribute.header.index;
		}
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, buildingMaterial);
	}

	// The composite name of the wall
	if (os.Contains (Wall::CompositeName) &&
		element.wall.modelElemStructureType == API_CompositeStructure) {

		os.Get (Wall::CompositeName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_CompWallID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				element.wall.composite = attribute.header.index;
		}
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, composite);
	}

	// The profile name of the wall
	if (os.Contains (Wall::ProfileName) &&
		element.wall.modelElemStructureType == API_ProfileStructure) {

		os.Get (Wall::ProfileName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_ProfileID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				element.wall.profileAttr = attribute.header.index;
		}
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, profileAttr);
	}

	// The arc angle of the wall
	if (os.Contains (Wall::ArcAngle)) {
		os.Get (Wall::ArcAngle, element.wall.angle);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, angle);
	}

	// The shape of the wall
	Objects::ElementShape wallShape;

	if (os.Contains (ElementBase::Shape)) {
		os.Get (ElementBase::Shape, wallShape);
		element.wall.poly.nSubPolys = wallShape.SubpolyCount ();
		element.wall.poly.nCoords = wallShape.VertexCount ();
		element.wall.poly.nArcs = wallShape.ArcCount ();

		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, poly.nSubPolys);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, poly.nCoords);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, poly.nArcs);

		wallShape.SetToMemo (memo, Objects::ElementShape::MemoMainPolygon);
	}

	// The thickness of the wall
	if (os.Contains (Wall::Thickness)) {
		os.Get (Wall::Thickness, element.wall.thickness);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, thickness);
	}

	// The first thickness of the trapezoid wall
	if (os.Contains (Wall::FirstThickness)) {
		os.Get (Wall::FirstThickness, element.wall.thickness);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, thickness);
	}

	// The second thickness of the trapezoid wall
	if (os.Contains (Wall::SecondThickness)) {
		os.Get (Wall::SecondThickness, element.wall.thickness1);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, thickness1);
	}

	// The outside slant angle of the wall
	if (os.Contains (Wall::OutsideSlantAngle)) {
		os.Get (Wall::OutsideSlantAngle, element.wall.slantAlpha);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, slantAlpha);
	}

	// The inside slant angle of the wall
	if (os.Contains (Wall::InsideSlantAngle)) {
		os.Get (Wall::InsideSlantAngle, element.wall.slantBeta);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, slantBeta);
	}

	// The height of the wall
	if (os.Contains (Wall::Height)) {
		if (!os.Contains (ElementBase::Level)) // TODO: rethink a better way to check for this 
			element.wall.relativeTopStory = 0; // unlink top story
		os.Get (Wall::Height, element.wall.height);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, height);
	}

	// PolyWall Corners Can Change
	if (os.Contains (Wall::PolyCanChange)) {
		os.Get (Wall::PolyCanChange, element.wall.polyCanChange);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, polyCanChange);
	}

	// Wall and stories relation

	// The top offset of the wall
	if (os.Contains (Wall::TopOffset)) {
		os.Get (Wall::TopOffset, element.wall.topOffset);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, topOffset);
	}

	// The top linked story
	if (os.Contains (Wall::RelativeTopStoryIndex)) {
		os.Get (Wall::RelativeTopStoryIndex, element.wall.relativeTopStory);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, relativeTopStory);
	}

	// Reference line parameters

	// The reference line location of the wall (outside, center, inside, core outside, core center or core inside)
	if (os.Contains (Wall::ReferenceLineLocation)) {
		GS::UniString referenceLineLocationName;
		os.Get (Wall::ReferenceLineLocation, referenceLineLocationName);

		GS::Optional<API_WallReferenceLineLocationID> type = referenceLineLocationNames.FindValue (referenceLineLocationName);
		if (type.HasValue ())
			element.wall.referenceLineLocation = type.Get ();

		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, referenceLineLocation);
	}

	// The offset of the wall�s base line from reference line
	if (os.Contains (Wall::ReferenceLineOffset)) {
		os.Get (Wall::ReferenceLineOffset, element.wall.offset);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, offset);
	}

	// Distance between reference line and outside face of the wall
	if (os.Contains (Wall::OffsetFromOutside)) {
		os.Get (Wall::OffsetFromOutside, element.wall.offsetFromOutside);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, offsetFromOutside);
	}

	// The index of the reference line beginning and end edge
	if (os.Contains (Wall::ReferenceLineStartIndex)) {
		os.Get (Wall::ReferenceLineStartIndex, element.wall.rLinInd);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, rLinInd);
	}

	if (os.Contains (Wall::ReferenceLineEndIndex)) {
		os.Get (Wall::ReferenceLineEndIndex, element.wall.rLinEndInd);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, rLinEndInd);
	}

	if (os.Contains (Wall::Flipped)) {
		os.Get (Wall::Flipped, element.wall.flipped);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, flipped);
	}

	// Floor Plan and Section - Floor Plan Display

	// Story visibility
	Utility::CreateVisibility (os, "", element.wall.isAutoOnStoryVisibility, element.wall.visibility);

	ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, isAutoOnStoryVisibility);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, visibility.showOnHome);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, visibility.showAllAbove);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, visibility.showAllBelow);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, visibility.showRelAbove);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, visibility.showRelBelow);

	// The display options (Projected, Projected with Overhead, Cut Only, Outlines Only, Overhead All or Symbolic Cut)
	if (os.Contains (Wall::DisplayOptionName)) {
		GS::UniString displayOptionName;
		os.Get (Wall::DisplayOptionName, displayOptionName);

		GS::Optional<API_ElemDisplayOptionsID> type = displayOptionNames.FindValue (displayOptionName);
		if (type.HasValue ()) {
			element.wall.displayOption = type.Get ();
			ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, displayOption);
		}
	}

	// Show projection (To Floor Plan Range, To Absolute Display Limit, Entire Element)
	if (os.Contains (Wall::ViewDepthLimitationName)) {
		GS::UniString viewDepthLimitationName;
		os.Get (Wall::ViewDepthLimitationName, viewDepthLimitationName);

		GS::Optional<API_ElemViewDepthLimitationsID> type = viewDepthLimitationNames.FindValue (viewDepthLimitationName);
		if (type.HasValue ()) {
			element.wall.viewDepthLimitation = type.Get ();
			ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, viewDepthLimitation);
		}
	}

	// Floor Plan and Section - Cut Surfaces parameters

	// The pen index of wall�s cut contour line
	if (os.Contains (Wall::CutLinePenIndex)) {
		os.Get (Wall::CutLinePenIndex, element.wall.contPen);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, contPen);
	}

	// The linetype name of wall�s cut contour line
	if (os.Contains (Wall::CutLinetypeName)) {

		os.Get (Wall::CutLinetypeName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				element.wall.contLtype = attribute.header.index;
		}
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, contLtype);
	}

	// Override cut fill and cut fill backgound pens
	if (CommandHelpers::SetCutfillPens(
		os,
		Wall::OverrideCutFillPenIndex,
		Wall::OverrideCutFillBackgroundPenIndex,
		element.wall,
		elementMask)
		!= NoError)
		return Error;

	// Floor Plan and Section - Outlines parameters

	// The pen index of wall�s uncut contour line
	if (os.Contains (Wall::UncutLinePenIndex)) {
		os.Get (Wall::UncutLinePenIndex, element.wall.contPen3D);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, contPen3D);
	}

	// The linetype name of wall�s uncut contour line
	if (os.Contains (Wall::UncutLinetypeName)) {

		os.Get (Wall::UncutLinetypeName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				element.wall.belowViewLineType = attribute.header.index;
		}
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, belowViewLineType);
	}

	// The pen index of wall�s overhead contour line
	if (os.Contains (Wall::OverheadLinePenIndex)) {
		os.Get (Wall::OverheadLinePenIndex, element.wall.aboveViewLinePen);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, aboveViewLinePen);
	}

	// The linetype name of wall�s overhead contour line
	if (os.Contains (Wall::OverheadLinetypeName)) {

		os.Get (Wall::OverheadLinetypeName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				element.wall.aboveViewLineType = attribute.header.index;
		}
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, aboveViewLineType);
	}

	// Model - Override Surfaces

	// The reference overridden material name
	ResetAPIOverriddenAttribute (element.wall.refMat);
	if (os.Contains (Wall::ReferenceMaterialName)) {
		//element.wall.refMat.overridden = true;
		os.Get (Wall::ReferenceMaterialName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_MaterialID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				SetAPIOverriddenAttribute (element.wall.refMat, attribute.header.index);
				ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, GetAPIOverriddenAttributeIndexField (refMat));
			}
		}
	}
	ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, GetAPIOverriddenAttributeBoolField (refMat));

	// The index of the reference material start and end edge index
	if (os.Contains (Wall::ReferenceMaterialStartIndex)) {
		os.Get (Wall::ReferenceMaterialStartIndex, element.wall.refInd);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, refInd);
	}

	if (os.Contains (Wall::ReferenceMaterialEndIndex)) {
		os.Get (Wall::ReferenceMaterialEndIndex, element.wall.refEndInd);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, refEndInd);
	}

	// The opposite overridden material name
	ResetAPIOverriddenAttribute (element.wall.oppMat);
	if (os.Contains (Wall::OppositeMaterialName)) {
		//element.wall.oppMat.overridden = true;
		os.Get (Wall::OppositeMaterialName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_MaterialID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				SetAPIOverriddenAttribute (element.wall.oppMat, attribute.header.index);
				ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, GetAPIOverriddenAttributeIndexField (oppMat));
			}
		}
	}
	ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, GetAPIOverriddenAttributeBoolField (oppMat));

	// The index of the opposite material start and end edge index
	if (os.Contains (Wall::OppositeMaterialStartIndex)) {
		os.Get (Wall::OppositeMaterialStartIndex, element.wall.oppInd);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, oppInd);
	}

	if (os.Contains (Wall::OppositeMaterialEndIndex)) {
		os.Get (Wall::OppositeMaterialEndIndex, element.wall.oppEndInd);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, oppEndInd);
	}

	// The side overridden material name
	ResetAPIOverriddenAttribute (element.wall.sidMat);
	if (os.Contains (Wall::SideMaterialName)) {
		//element.wall.sidMat.overridden = true;
		os.Get (Wall::SideMaterialName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_MaterialID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				SetAPIOverriddenAttribute (element.wall.sidMat, attribute.header.index);
				ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, GetAPIOverriddenAttributeIndexField  (sidMat));
			}
		}
	}
	ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, GetAPIOverriddenAttributeBoolField (sidMat));

	// The overridden materials are chained
	if (os.Contains (Wall::MaterialsChained)) {
		os.Get (Wall::MaterialsChained, element.wall.materialsChained);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, materialsChained);
	}

	// The end surface of the wall is inherited from the adjoining wall
	if (os.Contains (Wall::InheritEndSurface)) {
		os.Get (Wall::InheritEndSurface, element.wall.inheritEndSurface);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, inheritEndSurface);
	}

	// Align texture mapping to wall edges
	if (os.Contains (Wall::AlignTexture)) {
		os.Get (Wall::AlignTexture, element.wall.alignTexture);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, alignTexture);
	}

	// Sequence
	if (os.Contains (Wall::Sequence)) {
		os.Get (Wall::Sequence, element.wall.sequence);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, sequence);
	}

	// Model - Log Details (log height, start with half log, surface of horizontal edges, log shape)
	Int32 beamFlag = 0;
	if (os.Contains (Wall::LogHeight)) {
		os.Get (Wall::LogHeight, element.wall.logHeight);

		if (os.Contains (Wall::StartWithHalfLog)) {
			bool startWithHalfLog = false;
			os.Get (Wall::StartWithHalfLog, startWithHalfLog);

			if (startWithHalfLog)
				beamFlag = beamFlag + APIWBeam_HalfLog;
		}

		if (os.Contains (Wall::SurfaceOfHorizontalEdges)) {
			GS::UniString surfaceOfHorizontalEdgesName;
			os.Get (Wall::SurfaceOfHorizontalEdges, surfaceOfHorizontalEdgesName);

			GS::Optional<Int32> type = beamFlagNames.FindValue (surfaceOfHorizontalEdgesName);
			if (type.HasValue ())
				beamFlag = beamFlag + type.Get ();
		}

		if (os.Contains (Wall::LogShape)) {
			GS::UniString logShapeName;
			os.Get (Wall::LogShape, logShapeName);

			GS::Optional<Int32> type = beamFlagNames.FindValue (logShapeName);
			if (type.HasValue ())
				beamFlag = beamFlag + type.Get ();
		}

		element.wall.beamFlags = beamFlag;
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, logHeight);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, beamFlags);
	}

	// Model - Defines the relation of wall to zones (Zone Boundary, Reduce Zone Area Only, No Effect on Zones)
	if (os.Contains (Wall::WallRelationToZoneName)) {
		GS::UniString wallRelationToZoneName;
		os.Get (Wall::WallRelationToZoneName, wallRelationToZoneName);

		GS::Optional<API_ZoneRelID> type = relationToZoneNames.FindValue (wallRelationToZoneName);
		if (type.HasValue ())
			element.wall.zoneRel = type.Get ();

		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, type);
	}

	// Door & window
	if (os.Contains (Wall::HasDoor)) {
		os.Get (Wall::HasDoor, element.wall.hasDoor);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, hasDoor);
	}

	if (os.Contains (Wall::HasWindow)) {
		os.Get (Wall::HasWindow, element.wall.hasWindow);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, hasWindow);
	}

	return NoError;
}


GS::String CreateWall::GetName () const
{
	return CreateWallCommandName;
}


} // namespace AddOnCommands
