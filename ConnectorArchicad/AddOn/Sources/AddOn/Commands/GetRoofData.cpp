#include "GetRoofData.hpp"
#include "APIMigrationHelper.hpp"
#include "CommandHelpers.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Level.hpp"
#include "Objects/Polyline.hpp"
#include "RealNumber.h"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"
using namespace FieldNames;

namespace AddOnCommands
{


GS::String GetRoofData::GetFieldName () const
{
	return Roofs;
}


API_ElemTypeID GetRoofData::GetElemTypeID () const
{
	return API_RoofID;
}


GS::ErrCode GetRoofData::SerializeElementType (const API_Element& element,
	const API_ElementMemo& memo,
	GS::ObjectState& os) const
{
	// quantities
	API_ElementQuantity quantity = {};
	API_Quantities quantities = {};
	API_QuantitiesMask quantityMask;

	ACAPI_ELEMENT_QUANTITY_MASK_CLEAR (quantityMask);
	ACAPI_ELEMENT_QUANTITY_MASK_SET (quantityMask, roof, volume);

	quantities.elements = &quantity;
	GSErrCode err = ACAPI_Element_GetQuantities (element.roof.head.guid, nullptr, &quantities, &quantityMask);
	if (err != NoError)
		return err;

	// Geometry and positioning
	// The story of the shell
	API_StoryType story = Utility::GetStory (element.roof.head.floorInd);
	os.Add (ElementBase::Level, Objects::Level (story));

	// The shape of the roof
	double level = Utility::GetStoryLevel (element.roof.head.floorInd) + element.roof.shellBase.level;
	GSSize levelCount = element.roof.u.polyRoof.levelNum;

	GS::ObjectState baseLineOs;
	GS::ObjectState allLevels;

	switch (element.roof.roofClass) {
	case API_PlaneRoofID:
		os.Add (Roof::RoofClassName, roofClassNames.Get (API_PlaneRoofID));
		os.Add (ElementBase::Shape, Objects::ElementShape (element.roof.u.planeRoof.poly, memo, Objects::ElementShape::MemoMainPolygon, level));
		os.Add (Roof::PlaneRoofAngle, element.roof.u.planeRoof.angle);
		os.Add (Roof::PosSign, element.roof.u.planeRoof.posSign);

		baseLineOs.Add (Roof::BegC, Objects::Point3D (element.roof.u.planeRoof.baseLine.c1.x, element.roof.u.planeRoof.baseLine.c1.y, level));
		baseLineOs.Add (Roof::EndC, Objects::Point3D (element.roof.u.planeRoof.baseLine.c2.x, element.roof.u.planeRoof.baseLine.c2.y, level));
		os.Add (Roof::BaseLine, baseLineOs);

		break;
	case API_PolyRoofID:

		os.Add (Roof::RoofClassName, roofClassNames.Get (API_PolyRoofID));
		os.Add (ElementBase::Shape, Objects::ElementShape (element.roof.u.polyRoof.contourPolygon, memo, Objects::ElementShape::MemoMainPolygon, level));
		os.Add (Roof::PivotPolygon, Objects::ElementShape (element.roof.u.polyRoof.pivotPolygon, memo, Objects::ElementShape::MemoAdditionalPolygon, level));

		// Level num
		os.Add (Roof::LevelNum, levelCount);

		// Level data
		for (GSSize idx = 0; idx < levelCount; ++idx) {
			GS::ObjectState currentLevel;
			currentLevel.Add (Roof::LevelData::LevelHeight, element.roof.u.polyRoof.levelData[idx].levelHeight);
			currentLevel.Add (Roof::LevelData::LevelAngle, element.roof.u.polyRoof.levelData[idx].levelAngle);

			allLevels.Add (GS::String::SPrintf (Roof::RoofLevel::LevelName, idx + 1), currentLevel);
		}
		os.Add (Roof::levels, allLevels);

		if (memo.pivotPolyEdges != nullptr) {
			GS::ObjectState allPivotPolyEdges;
			Utility::GetAllPivotPolyEdgeData (memo.pivotPolyEdges, allPivotPolyEdges);
			os.Add (PivotPolyEdge::EdgeData, allPivotPolyEdges);
		}
		break;
	default:
		break;
	}

	// The thickness of the roof
	os.Add (Roof::Thickness, element.roof.shellBase.thickness);

	// The structure type of the roof (basic or composite)
	os.Add (Roof::Structure, structureTypeNames.Get (element.roof.shellBase.modelElemStructureType));

	// The building material name or composite name of the roof
	API_Attribute attribute;
	switch (element.roof.shellBase.modelElemStructureType) {
	case API_BasicStructure:
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_BuildingMaterialID;
		attribute.header.index = element.roof.shellBase.buildingMaterial;

		if (NoError == ACAPI_Attribute_Get (&attribute))
			os.Add (Roof::BuildingMaterialName, GS::UniString{attribute.header.name});
		break;
	case API_CompositeStructure:
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_CompWallID;
		attribute.header.index = element.roof.shellBase.composite;

		if (NoError == ACAPI_Attribute_Get (&attribute))
			os.Add (Roof::CompositeName, GS::UniString{attribute.header.name});
		break;
	default:
		break;
	}

	// The edge type of the roof
	os.Add (Roof::EdgeAngleType, edgeAngleTypeNames.Get (element.roof.shellBase.edgeTrim.sideType));

	// The edge angle of the roof
	os.Add (Roof::EdgeAngle, element.roof.shellBase.edgeTrim.sideAngle);

	// Floor Plan and Section - Floor Plan Display

	// Show on Stories - Story visibility
	{
		GS::UniString visibilityFillString;
		Utility::GetPredefinedVisibility (false, element.roof.shellBase.visibilityFill, visibilityFillString);

		GS::UniString visibilityContString;
		Utility::GetPredefinedVisibility (false, element.roof.shellBase.visibilityCont, visibilityContString);

		if (visibilityFillString == visibilityContString && visibilityFillString != CustomStoriesValueName) {
			os.Add (ShowOnStories, visibilityContString);
		} else {
			os.Add (ShowOnStories, CustomStoriesValueName);

			Utility::GetVisibility (false, element.roof.shellBase.visibilityFill, os, VisibilityFillData, true);
			Utility::GetVisibility (false, element.roof.shellBase.visibilityCont, os, VisibilityContData, true);
		}
	}

	// The display options (Projected, Projected with Overhead, Cut Only, Outlines Only, Overhead All or Symbolic Cut)
	os.Add (Roof::DisplayOptionName, displayOptionNames.Get (element.roof.shellBase.displayOption));

	// Show projection (To Floor Plan Range, To Absolute Display Limit, Entire Element)
	os.Add (Roof::ViewDepthLimitationName, viewDepthLimitationNames.Get (element.roof.shellBase.viewDepthLimitation));

	// Floor Plan and Section - Cut Surfaces

	// The pen index and linetype name of beam section line
	API_Attribute attrib;
	os.Add (Roof::SectContPen, element.roof.shellBase.sectContPen);

	BNZeroMemory (&attrib, sizeof (API_Attribute));
	attrib.header.typeID = API_LinetypeID;
	attrib.header.index = element.roof.shellBase.sectContLtype;

	if (NoError == ACAPI_Attribute_Get (&attrib))
		os.Add (Roof::SectContLtype, GS::UniString{attrib.header.name});

	// Override cut fill pen and background cut fill pen
	CommandHelpers::GetCutfillPens (element.roof.shellBase, os, Roof::CutFillPen, Roof::CutFillBackgroundPen);

	// Outlines

	// The pen index and linetype name of roof contour line
	os.Add (Roof::ContourPen, element.roof.shellBase.pen);

	BNZeroMemory (&attrib, sizeof (API_Attribute));
	attrib.header.typeID = API_LinetypeID;
	attrib.header.index = element.roof.shellBase.ltypeInd;

	if (NoError == ACAPI_Attribute_Get (&attrib))
		os.Add (Roof::ContourLineType, GS::UniString{attrib.header.name});

	// The pen index and linetype name of roof above contour line
	os.Add (Roof::OverheadLinePen, element.roof.shellBase.aboveViewLinePen);

	BNZeroMemory (&attrib, sizeof (API_Attribute));
	attrib.header.typeID = API_LinetypeID;
	attrib.header.index = element.roof.shellBase.aboveViewLineType;

	if (NoError == ACAPI_Attribute_Get (&attrib))
		os.Add (Roof::OverheadLinetype, GS::UniString{attrib.header.name});

	// Floor Plan and Section - Cover Fills
	os.Add (Roof::UseFloorFill, element.roof.shellBase.useFloorFill);
	if (element.roof.shellBase.useFloorFill) {
		os.Add (Roof::Use3DHatching, element.roof.shellBase.use3DHatching);
		os.Add (Roof::UseFillLocBaseLine, element.roof.shellBase.useFillLocBaseLine);
		os.Add (Roof::UseSlantedFill, element.roof.shellBase.useSlantedFill);
		os.Add (Roof::FloorFillPen, element.roof.shellBase.floorFillPen);
		os.Add (Roof::FloorFillBGPen, element.roof.shellBase.floorFillBGPen);

		// Cover fill type
		if (!element.roof.shellBase.use3DHatching) {

			BNZeroMemory (&attrib, sizeof (API_Attribute));
			attrib.header.typeID = API_FilltypeID;
			attrib.header.index = element.roof.shellBase.floorFillInd;

			if (NoError == ACAPI_Attribute_Get (&attrib))
				os.Add (Roof::FloorFillName, GS::UniString{attrib.header.name});
		}

		// Hatch Orientation
		Utility::GetHatchOrientation (element.roof.shellBase.hatchOrientation.type, os);

		if (element.roof.shellBase.hatchOrientation.type == API_HatchRotated || element.roof.shellBase.hatchOrientation.type == API_HatchDistorted) {
			os.Add (Roof::HatchOrientationOrigoX, element.roof.shellBase.hatchOrientation.origo.x);
			os.Add (Roof::HatchOrientationOrigoY, element.roof.shellBase.hatchOrientation.origo.y);
			os.Add (Roof::HatchOrientationXAxisX, element.roof.shellBase.hatchOrientation.matrix00);
			os.Add (Roof::HatchOrientationXAxisY, element.roof.shellBase.hatchOrientation.matrix10);
			os.Add (Roof::HatchOrientationYAxisX, element.roof.shellBase.hatchOrientation.matrix01);
			os.Add (Roof::HatchOrientationYAxisY, element.roof.shellBase.hatchOrientation.matrix11);
		}
	}

	// Model

	// Overridden materials
	int countOverriddenMaterial = 0;
	if (IsAPIOverriddenAttributeOverridden (element.roof.shellBase.topMat)) {
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_MaterialID;
		attribute.header.index = GetAPIOverriddenAttribute (element.roof.shellBase.topMat);

		if (NoError == ACAPI_Attribute_Get (&attribute))
			countOverriddenMaterial = countOverriddenMaterial + 1;

		os.Add (Roof::TopMat, GS::UniString{attribute.header.name});
	}

	if (IsAPIOverriddenAttributeOverridden (element.roof.shellBase.sidMat)) {
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_MaterialID;
		attribute.header.index = GetAPIOverriddenAttribute (element.roof.shellBase.sidMat);

		if (NoError == ACAPI_Attribute_Get (&attribute))
			countOverriddenMaterial = countOverriddenMaterial + 1;

		os.Add (Roof::SideMat, GS::UniString{attribute.header.name});
	}

	if (IsAPIOverriddenAttributeOverridden (element.roof.shellBase.botMat)) {
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_MaterialID;
		attribute.header.index = GetAPIOverriddenAttribute (element.roof.shellBase.botMat);

		if (NoError == ACAPI_Attribute_Get (&attribute))
			countOverriddenMaterial = countOverriddenMaterial + 1;

		os.Add (Roof::BotMat, GS::UniString{attribute.header.name});
	}

	// The overridden materials are chained
	if (countOverriddenMaterial > 1) {
		os.Add (Roof::MaterialsChained, element.roof.shellBase.materialsChained);
	}

	// Trimming Body (Editable, Contours Down, Pivot Lines Down, Upwards Extrusion or Downwards Extrusion)
	os.Add (Roof::TrimmingBodyName, shellBaseCutBodyTypeNames.Get (element.roof.shellBase.cutBodyType));

	return NoError;
}


GS::String GetRoofData::GetName () const
{
	return GetRoofDataCommandName;
}


}
