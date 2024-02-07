#include "CreateRoof.hpp"
#include "APIMigrationHelper.hpp"
#include "CommandHelpers.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Polyline.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"
#include "AngleData.h"
using namespace FieldNames;


namespace AddOnCommands {


GS::String CreateRoof::GetFieldName () const
{
	return FieldNames::Roofs;
}


GS::UniString CreateRoof::GetUndoableCommandName () const
{
	return "CreateSpeckleRoof";
}


GSErrCode CreateRoof::GetElementFromObjectState (const GS::ObjectState& os,
	API_Element& element,
	API_Element& elementMask,
	API_ElementMemo& memo,
	GS::UInt64& memoMask,
	API_SubElement** /*marker = nullptr*/,
	AttributeManager& /*attributeManager*/,
	LibpartImportManager& /*libpartImportManager*/,
	GS::Array<GS::UniString>& log) const
{
	GSErrCode err = NoError;

	Utility::SetElementType (element.header, API_RoofID);
	err = Utility::GetBaseElementData (element, &memo, nullptr, log);
	if (err != NoError)
		return err;

	err = GetElementBaseFromObjectState (os, element, elementMask);
	if (err != NoError)
		return err;

	// The structure of the roof
	if (os.Contains (Roof::RoofClassName)) {
		GS::UniString roofClassName;
		os.Get (Roof::RoofClassName, roofClassName);

		GS::Optional<API_RoofClassID> type = roofClassNames.FindValue (roofClassName);
		if (type.HasValue ()) {
			element.roof.roofClass = type.Get ();
			ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, roofClass);
		}
	}

	// Geometry and positioning
	memoMask = APIMemoMask_Polygon | APIMemoMask_SideMaterials | APIMemoMask_EdgeTrims;

	// The shape of the roof
	Objects::ElementShape roofShape;
	Objects::ElementShape pivotPolygon;

	GS::ObjectState baseLineOs;
	Objects::Point3D startPoint;
	Objects::Point3D endPoint;

	GS::ObjectState allLevels;

	switch (element.roof.roofClass) {
	case API_PlaneRoofID:

		if (os.Contains (ElementBase::Shape)) {
			os.Get (ElementBase::Shape, roofShape);
			element.roof.u.planeRoof.poly.nSubPolys = roofShape.SubpolyCount ();
			element.roof.u.planeRoof.poly.nCoords = roofShape.VertexCount ();
			element.roof.u.planeRoof.poly.nArcs = roofShape.ArcCount ();

			roofShape.SetToMemo (memo, Objects::ElementShape::MemoMainPolygon);

			ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, u.planeRoof.poly.nSubPolys);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, u.planeRoof.poly.nCoords);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, u.planeRoof.poly.nArcs);
		}

		if (os.Contains (Roof::PlaneRoofAngle)) {
			os.Get (Roof::PlaneRoofAngle, element.roof.u.planeRoof.angle);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, u.planeRoof.angle);
		}
		if (os.Contains (Roof::PosSign)) {
			os.Get (Roof::PosSign, element.roof.u.planeRoof.posSign);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, u.planeRoof.posSign);
		}

		if (os.Contains (Roof::BaseLine)) {
			os.Get (Roof::BaseLine, baseLineOs);

			baseLineOs.Get (Roof::BegC, startPoint);
			element.roof.u.planeRoof.baseLine.c1 = startPoint.ToAPI_Coord ();
			ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, u.planeRoof.baseLine.c1);

			baseLineOs.Get (Roof::EndC, endPoint);
			element.roof.u.planeRoof.baseLine.c2 = endPoint.ToAPI_Coord ();
			ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, u.planeRoof.baseLine.c2);
		}

		break;
	case API_PolyRoofID:

		// Shape (contour polygon)
		if (os.Contains (ElementBase::Shape)) {
			os.Get (ElementBase::Shape, roofShape);
			element.roof.u.polyRoof.contourPolygon.nSubPolys = roofShape.SubpolyCount ();
			element.roof.u.polyRoof.contourPolygon.nCoords = roofShape.VertexCount ();
			element.roof.u.polyRoof.contourPolygon.nArcs = roofShape.ArcCount ();

			roofShape.SetToMemo (memo, Objects::ElementShape::MemoMainPolygon);

			ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, u.polyRoof.contourPolygon.nSubPolys);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, u.polyRoof.contourPolygon.nCoords);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, u.polyRoof.contourPolygon.nArcs);
		}

		// Pivot Polygon
		if (os.Contains (Roof::PivotPolygon)) {
			os.Get (Roof::PivotPolygon, pivotPolygon);
			element.roof.u.polyRoof.pivotPolygon.nSubPolys = pivotPolygon.SubpolyCount ();
			element.roof.u.polyRoof.pivotPolygon.nCoords = pivotPolygon.VertexCount ();
			element.roof.u.polyRoof.pivotPolygon.nArcs = pivotPolygon.ArcCount ();

			pivotPolygon.SetToMemo (memo, Objects::ElementShape::MemoAdditionalPolygon);

			ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, u.polyRoof.pivotPolygon.nSubPolys);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, u.polyRoof.pivotPolygon.nCoords);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, u.polyRoof.pivotPolygon.nArcs);
		}


		if (os.Contains (Roof::LevelNum)) {
			os.Get (Roof::LevelNum, element.roof.u.polyRoof.levelNum);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_PolyRoofData, levelNum);
		}

		if (os.Contains (Roof::levels))
			os.Get (Roof::levels, allLevels);

		for (short idx = 0; idx < element.roof.u.polyRoof.levelNum; ++idx) {
			GS::ObjectState currentLevel;
			allLevels.Get (GS::String::SPrintf (Roof::RoofLevel::LevelName, idx + 1), currentLevel);

			if (!currentLevel.IsEmpty ()) {
				// Level Height
				if (currentLevel.Contains (Roof::LevelData::LevelHeight)) {
					currentLevel.Get (Roof::LevelData::LevelHeight, element.roof.u.polyRoof.levelData[idx].levelHeight);
					ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofLevelData, levelHeight);
				}

				// Level Angle
				if (currentLevel.Contains (Roof::LevelData::LevelAngle)) {
					currentLevel.Get (Roof::LevelData::LevelAngle, element.roof.u.polyRoof.levelData[idx].levelAngle);
					ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofLevelData, levelAngle);
				}
			}
		}

		if (os.Contains (PivotPolyEdge::EdgeData)) {
			GS::ObjectState allPivotPolyEdges;
			os.Get (PivotPolyEdge::EdgeData, allPivotPolyEdges);

			GS::UInt32 numberOfPivotPolyEdges = element.roof.u.polyRoof.pivotPolygon.nCoords;

			Utility::CreateAllPivotPolyEdgeData (allPivotPolyEdges, numberOfPivotPolyEdges, &memo);
		}

		element.roof.u.polyRoof.overHangType = API_ManualOverhang;
		ACAPI_ELEMENT_MASK_SET (elementMask, API_PolyRoofData, overHangType);

		break;
	default:
		break;
	}

	// The floor index and level of the roof
	if (os.Contains (ElementBase::Level)) {
		GetStoryFromObjectState (os, roofShape.Level (), element.header.floorInd, element.roof.shellBase.level);
	}
	else {
		Utility::SetStoryLevelAndFloor (roofShape.Level (), element.header.floorInd, element.roof.shellBase.level);
	}
	ACAPI_ELEMENT_MASK_SET (elementMask, API_Elem_Head, floorInd);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.level);

	// The thickness of the roof
	if (os.Contains (Roof::Thickness)) {
		os.Get (Roof::Thickness, element.roof.shellBase.thickness);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.thickness);
	}

	// The structure of the roof
	if (os.Contains (Roof::Structure)) {
		GS::UniString structureName;
		os.Get (Roof::Structure, structureName);

		GS::Optional<API_ModelElemStructureType> type = structureTypeNames.FindValue (structureName);
		if (type.HasValue ()) {
			element.roof.shellBase.modelElemStructureType = type.Get ();
			ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.modelElemStructureType);
		}
	}

	// The building material name of the roof.shellBase
	GS::UniString attributeName;
	if (os.Contains (Roof::BuildingMaterialName) &&
		element.roof.shellBase.modelElemStructureType == API_BasicStructure) {

		os.Get (Roof::BuildingMaterialName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_BuildingMaterialID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				element.roof.shellBase.buildingMaterial = attribute.header.index;
				ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.buildingMaterial);
			}
		}
	}

	// The composite name of the roof.shellBase
	if (os.Contains (Roof::CompositeName) &&
		element.roof.shellBase.modelElemStructureType == API_CompositeStructure) {

		os.Get (Roof::CompositeName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_CompWallID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				element.roof.shellBase.composite = attribute.header.index;
				ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.composite);
			}
		}
	}

	// The edge type of the roof
	if (os.Contains (Roof::EdgeAngleType)) {
		GS::UniString edgeAngleType;
		os.Get (Roof::EdgeAngleType, edgeAngleType);

		GS::Optional<API_EdgeTrimID> type = edgeAngleTypeNames.FindValue (edgeAngleType);
		if (type.HasValue ()) {
			element.roof.shellBase.edgeTrim.sideType = type.Get ();
			ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.edgeTrim.sideType);
		}
	}

	// The edge angle of the roof
	if (os.Contains (Roof::EdgeAngle)) {
		os.Get (Roof::EdgeAngle, element.roof.shellBase.edgeTrim.sideAngle);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.edgeTrim.sideAngle);
	}

	// Floor Plan and Section - Floor Plan Display

	// Show on Stories - Story visibility
	bool isAutoOnStoryVisibility = false;
	Utility::CreateVisibility (os, VisibilityContData, isAutoOnStoryVisibility, element.roof.shellBase.visibilityCont);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.visibilityCont.showOnHome);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.visibilityCont.showAllAbove);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.visibilityCont.showAllBelow);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.visibilityCont.showRelAbove);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.visibilityCont.showRelBelow);

	Utility::CreateVisibility (os, VisibilityFillData, isAutoOnStoryVisibility, element.roof.shellBase.visibilityFill);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.visibilityFill.showOnHome);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.visibilityFill.showAllAbove);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.visibilityFill.showAllBelow);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.visibilityFill.showRelAbove);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.visibilityFill.showRelBelow);

	// The display options (Projected, Projected with Overhead, Cut Only, Outlines Only, Overhead All or Symbolic Cut)
	if (os.Contains (Roof::DisplayOptionName)) {
		GS::UniString displayOptionName;
		os.Get (Roof::DisplayOptionName, displayOptionName);

		GS::Optional<API_ElemDisplayOptionsID> type = displayOptionNames.FindValue (displayOptionName);
		if (type.HasValue ()) {
			element.roof.shellBase.displayOption = type.Get ();
			ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.displayOption);
		}
	}

	// Show projection (To Floor Plan Range, To Absolute Display Limit, Entire Element)
	if (os.Contains (Roof::ViewDepthLimitationName)) {
		GS::UniString viewDepthLimitationName;
		os.Get (Roof::ViewDepthLimitationName, viewDepthLimitationName);

		GS::Optional<API_ElemViewDepthLimitationsID> type = viewDepthLimitationNames.FindValue (viewDepthLimitationName);
		if (type.HasValue ()) {
			element.roof.shellBase.viewDepthLimitation = type.Get ();
			ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.viewDepthLimitation);
		}
	}

	// Floor Plan and Section - Cut Surfaces

	// The pen index and linetype name of roof section line
	if (os.Contains (Roof::SectContPen)) {
		os.Get (Roof::SectContPen, element.roof.shellBase.sectContPen);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.sectContPen);
	}

	if (os.Contains (Roof::SectContLtype)) {

		os.Get (Roof::SectContLtype, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				element.roof.shellBase.sectContLtype = attribute.header.index;
				ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.sectContLtype);
			}
		}
	}

	// Override cut fill and cut fill backgound pens
	if (CommandHelpers::SetCutfillPens(
		os,
		Roof::CutFillPen,
		Roof::CutFillBackgroundPen,
		element.roof.shellBase,
		elementMask)
		!= NoError)
		return Error;

	// Outlines

	// The pen index and linetype name of roof contour line
	if (os.Contains (Roof::ContourPen)) {
		os.Get (Roof::ContourPen, element.roof.shellBase.pen);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.pen);
	}

	if (os.Contains (Roof::ContourLineType)) {

		os.Get (Roof::ContourLineType, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				element.roof.shellBase.ltypeInd = attribute.header.index;
				ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.ltypeInd);
			}
		}
	}

	// The pen index and linetype name of slab hidden contour line
	if (os.Contains (Roof::OverheadLinePen)) {
		os.Get (Roof::OverheadLinePen, element.roof.shellBase.aboveViewLinePen);

		ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.aboveViewLinePen);
	}

	if (os.Contains (Roof::OverheadLinetype)) {

		os.Get (Roof::OverheadLinetype, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				element.roof.shellBase.aboveViewLineType = attribute.header.index;
				ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.aboveViewLineType);
			}
		}
	}

	// Floor Plan and Section - Cover Fills
	if (os.Contains (Roof::UseFloorFill)) {
		os.Get (Roof::UseFloorFill, element.roof.shellBase.useFloorFill);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.useFloorFill);
	}

	if (os.Contains (Roof::Use3DHatching)) {
		os.Get (Roof::Use3DHatching, element.roof.shellBase.use3DHatching);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.use3DHatching);
	}

	if (os.Contains (Roof::UseFillLocBaseLine)) {
		os.Get (Roof::UseFillLocBaseLine, element.roof.shellBase.useFillLocBaseLine);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.useFillLocBaseLine);
	}

	if (os.Contains (Roof::UseSlantedFill)) {
		os.Get (Roof::UseSlantedFill, element.roof.shellBase.useSlantedFill);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.useSlantedFill);
	}

	if (os.Contains (Roof::FloorFillPen)) {
		os.Get (Roof::FloorFillPen, element.roof.shellBase.floorFillPen);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.floorFillPen);
	}

	if (os.Contains (Roof::FloorFillBGPen)) {
		os.Get (Roof::FloorFillBGPen, element.roof.shellBase.floorFillBGPen);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.floorFillBGPen);
	}

	// Cover fill type
	if (os.Contains (Roof::FloorFillName)) {

		os.Get (Roof::FloorFillName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_FilltypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				element.roof.shellBase.floorFillInd = attribute.header.index;
				ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.floorFillInd);
			}
		}
	}

	// Cover Fill Transformation
	Utility::CreateHatchOrientation (os, element.roof.shellBase.hatchOrientation.type);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.hatchOrientation.type);

	if (os.Contains (Roof::HatchOrientationOrigoX)) {
		os.Get (Roof::HatchOrientationOrigoX, element.roof.shellBase.hatchOrientation.origo.x);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.hatchOrientation.origo.x);
	}

	if (os.Contains (Roof::HatchOrientationOrigoY)) {
		os.Get (Roof::HatchOrientationOrigoY, element.roof.shellBase.hatchOrientation.origo.y);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.hatchOrientation.origo.y);
	}

	if (os.Contains (Roof::HatchOrientationXAxisX)) {
		os.Get (Roof::HatchOrientationXAxisX, element.roof.shellBase.hatchOrientation.matrix00);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.hatchOrientation.matrix00);
	}

	if (os.Contains (Roof::HatchOrientationXAxisY)) {
		os.Get (Roof::HatchOrientationXAxisY, element.roof.shellBase.hatchOrientation.matrix10);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.hatchOrientation.matrix10);
	}

	if (os.Contains (Roof::HatchOrientationYAxisX)) {
		os.Get (Roof::HatchOrientationYAxisX, element.roof.shellBase.hatchOrientation.matrix01);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.hatchOrientation.matrix01);
	}

	if (os.Contains (Roof::HatchOrientationYAxisY)) {
		os.Get (Roof::HatchOrientationYAxisY, element.roof.shellBase.hatchOrientation.matrix11);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.hatchOrientation.matrix11);
	}

	// Model

	// Overridden materials
	ResetAPIOverriddenAttribute (element.roof.shellBase.topMat);
	if (os.Contains (Roof::TopMat)) {

		os.Get (Roof::TopMat, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_MaterialID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				SetAPIOverriddenAttribute (element.roof.shellBase.topMat, attribute.header.index);
				ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, GetAPIOverriddenAttributeIndexField (shellBase.topMat));
			}
		}
	}
	ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, GetAPIOverriddenAttributeBoolField (shellBase.topMat));

	ResetAPIOverriddenAttribute (element.roof.shellBase.sidMat);
	if (os.Contains (Roof::SideMat)) {

		os.Get (Roof::SideMat, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_MaterialID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				SetAPIOverriddenAttribute (element.roof.shellBase.sidMat, attribute.header.index);
				ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, GetAPIOverriddenAttributeIndexField (shellBase.sidMat));
			}
		}
	}
	ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, GetAPIOverriddenAttributeBoolField (shellBase.sidMat));

	ResetAPIOverriddenAttribute (element.roof.shellBase.botMat);
	if (os.Contains (Roof::BotMat)) {
		os.Get (Roof::BotMat, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_MaterialID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				SetAPIOverriddenAttribute (element.roof.shellBase.botMat, attribute.header.index);
				ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, GetAPIOverriddenAttributeIndexField (shellBase.botMat));
			}
		}
	}
	ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, GetAPIOverriddenAttributeBoolField (shellBase.botMat));

	// The overridden materials are chained
	if (os.Contains (Roof::MaterialsChained)) {
		os.Get (Roof::MaterialsChained, element.roof.shellBase.materialsChained);

		ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.materialsChained);
	}

	// Trimming Body (Editable, Contours Down, Pivot Lines Down, Upwards Extrusion or Downwards Extrusion)
	if (os.Contains (Roof::TrimmingBodyName)) {
		GS::UniString trimmingBodyName;
		os.Get (Roof::TrimmingBodyName, trimmingBodyName);

		GS::Optional<API_ShellBaseCutBodyTypeID> type = shellBaseCutBodyTypeNames.FindValue (trimmingBodyName);
		if (type.HasValue ()) {
			element.roof.shellBase.cutBodyType = type.Get ();
			ACAPI_ELEMENT_MASK_SET (elementMask, API_RoofType, shellBase.cutBodyType);
		}
	}

	return NoError;
}


GS::String CreateRoof::GetName () const
{
	return CreateRoofCommandName;
}


} // namespace AddOnCommands
