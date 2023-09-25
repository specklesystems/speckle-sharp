#include "GetSlabData.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Level.hpp"
#include "Objects/Polyline.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"
using namespace FieldNames;


namespace AddOnCommands {


GS::String GetSlabData::GetFieldName () const
{
	return Slabs;
}


API_ElemTypeID GetSlabData::GetElemTypeID () const
{
	return API_SlabID;
}


GS::ErrCode GetSlabData::SerializeElementType (const API_Element& element,
	const API_ElementMemo& memo,
	GS::ObjectState& os) const
{
	GS::ErrCode err = NoError;
	err = GetDataCommand::SerializeElementType (element, memo, os);
	if (NoError != err)
		return err;

	// Geometry and positioning
	// The index of the slab's floor
	API_StoryType story = Utility::GetStory (element.slab.head.floorInd);
	os.Add (ElementBase::Level, Objects::Level (story));

	// The shape of the slab
	double level = Utility::GetStoryLevel (element.slab.head.floorInd) + element.slab.level;
	os.Add (ElementBase::Shape, Objects::ElementShape (element.slab.poly, memo, Objects::ElementShape::MemoMainPolygon, level));

	// The thickness of the slab
	os.Add (Slab::Thickness, element.slab.thickness);

	// The structure type of the slab (basic or composite)
	os.Add (Slab::Structure, structureTypeNames.Get (element.slab.modelElemStructureType));

	// The building material name or composite name of the slab
	API_Attribute attribute;
	switch (element.slab.modelElemStructureType) {
	case API_BasicStructure:
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_BuildingMaterialID;
		attribute.header.index = element.slab.buildingMaterial;

		if (NoError == ACAPI_Attribute_Get (&attribute))
			os.Add (Slab::BuildingMaterialName, GS::UniString{attribute.header.name});
		break;
	case API_CompositeStructure:
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_CompWallID;
		attribute.header.index = element.slab.composite;

		if (NoError == ACAPI_Attribute_Get (&attribute))
			os.Add (Slab::CompositeName, GS::UniString{attribute.header.name});
		break;
	default:
		break;
	}

	// The edge type and edge angle of the slab
	if ((BMGetHandleSize ((GSHandle) memo.edgeTrims) / sizeof (API_EdgeTrim) >= 1) &&
		(*(memo.edgeTrims))[1].sideType == APIEdgeTrim_CustomAngle) {
		double angle = (*(memo.edgeTrims))[1].sideAngle;
		os.Add (Slab::EdgeAngleType, edgeAngleTypeNames.Get (APIEdgeTrim_CustomAngle));
		os.Add (Slab::EdgeAngle, angle);
	} else {
		os.Add (Slab::EdgeAngleType, edgeAngleTypeNames.Get (APIEdgeTrim_Perpendicular));
	}

	// The reference plane location of the slab
	os.Add (Slab::ReferencePlaneLocation, referencePlaneLocationNames.Get (element.slab.referencePlaneLocation));

	// Floor Plan and Section - Floor Plan Display

	// Show on Stories - Story visibility
	{
		GS::UniString visibilityFillString;
		Utility::GetPredefinedVisibility (false, element.slab.visibilityFill, visibilityFillString);

		GS::UniString visibilityContString;
		Utility::GetPredefinedVisibility (false, element.slab.visibilityCont, visibilityContString);

		if (visibilityFillString == visibilityContString && visibilityFillString != CustomStoriesValueName) {
			os.Add (ShowOnStories, visibilityContString);
		} else {
			os.Add (ShowOnStories, CustomStoriesValueName);

			Utility::GetVisibility (false, element.slab.visibilityFill, os, VisibilityFillData, true);
			Utility::GetVisibility (false, element.slab.visibilityCont, os, VisibilityContData, true);
		}
	}

	// Floor Plan and Section - Cut Surfaces

	// The pen index and linetype name of beam section line
	API_Attribute attrib;
	os.Add (Slab::sectContPen, element.slab.sectContPen);

	BNZeroMemory (&attrib, sizeof (API_Attribute));
	attrib.header.typeID = API_LinetypeID;
	attrib.header.index = element.slab.sectContLtype;

	if (NoError == ACAPI_Attribute_Get (&attrib))
		os.Add (Slab::sectContLtype, GS::UniString{attrib.header.name});

	// Override cut fill pen
	if (element.slab.penOverride.overrideCutFillPen) {
		os.Add (Slab::cutFillPen, element.slab.penOverride.cutFillPen);
	}

	// Override cut fill backgound pen
	if (element.slab.penOverride.overrideCutFillBackgroundPen) {
		os.Add (Slab::cutFillBackgroundPen, element.slab.penOverride.cutFillBackgroundPen);
	}

	// Outlines

	// The pen index and linetype name of beam contour line
	os.Add (Slab::contourPen, element.slab.pen);

	BNZeroMemory (&attrib, sizeof (API_Attribute));
	attrib.header.typeID = API_LinetypeID;
	attrib.header.index = element.slab.ltypeInd;

	if (NoError == ACAPI_Attribute_Get (&attrib))
		os.Add (Slab::contourLineType, GS::UniString{attrib.header.name});

	// The pen index and linetype name of beam hidden contour line
	os.Add (Slab::hiddenContourLinePen, element.slab.hiddenContourLinePen);

	BNZeroMemory (&attrib, sizeof (API_Attribute));
	attrib.header.typeID = API_LinetypeID;
	attrib.header.index = element.slab.hiddenContourLineType;

	if (NoError == ACAPI_Attribute_Get (&attrib))
		os.Add (Slab::hiddenContourLineType, GS::UniString{attrib.header.name});

	// Floor Plan and Section - Cover Fills
	os.Add (Slab::useFloorFill, element.slab.useFloorFill);
	if (element.slab.useFloorFill) {
		os.Add (Slab::use3DHatching, element.slab.use3DHatching);
		os.Add (Slab::floorFillPen, element.slab.floorFillPen);
		os.Add (Slab::floorFillBGPen, element.slab.floorFillBGPen);

		// Cover fill type
		if (!element.slab.use3DHatching) {

			BNZeroMemory (&attrib, sizeof (API_Attribute));
			attrib.header.typeID = API_FilltypeID;
			attrib.header.index = element.slab.floorFillInd;

			if (NoError == ACAPI_Attribute_Get (&attrib))
				os.Add (Slab::floorFillName, GS::UniString{attrib.header.name});
		}

		// Hatch Orientation
		Utility::GetHatchOrientation (element.slab.hatchOrientation.type, os);

		if (element.slab.hatchOrientation.type == API_HatchRotated || element.slab.hatchOrientation.type == API_HatchDistorted) {
			os.Add (Slab::hatchOrientationOrigoX, element.slab.hatchOrientation.origo.x);
			os.Add (Slab::hatchOrientationOrigoY, element.slab.hatchOrientation.origo.y);
			os.Add (Slab::hatchOrientationXAxisX, element.slab.hatchOrientation.matrix00);
			os.Add (Slab::hatchOrientationXAxisY, element.slab.hatchOrientation.matrix10);
			os.Add (Slab::hatchOrientationYAxisX, element.slab.hatchOrientation.matrix01);
			os.Add (Slab::hatchOrientationYAxisY, element.slab.hatchOrientation.matrix11);
		}
	}

	// Model

	// Overridden materials
	int countOverriddenMaterial = 0;
	if (element.slab.topMat.overridden) {
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_MaterialID;
		attribute.header.index = element.slab.topMat.attributeIndex;

		if (NoError == ACAPI_Attribute_Get (&attribute))
			countOverriddenMaterial = countOverriddenMaterial + 1;

		os.Add (Slab::topMat, GS::UniString{attribute.header.name});
	}

	if (element.slab.sideMat.overridden) {
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_MaterialID;
		attribute.header.index = element.slab.sideMat.attributeIndex;

		if (NoError == ACAPI_Attribute_Get (&attribute))
			countOverriddenMaterial = countOverriddenMaterial + 1;

		os.Add (Slab::sideMat, GS::UniString{attribute.header.name});
	}

	if (element.slab.botMat.overridden) {
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_MaterialID;
		attribute.header.index = element.slab.botMat.attributeIndex;

		if (NoError == ACAPI_Attribute_Get (&attribute))
			countOverriddenMaterial = countOverriddenMaterial + 1;

		os.Add (Slab::botMat, GS::UniString{attribute.header.name});
	}

	// The overridden materials are chained
	if (countOverriddenMaterial > 1) {
		os.Add (Slab::materialsChained, element.slab.materialsChained);
	}


	return NoError;
}


GS::String GetSlabData::GetName () const
{
	return GetSlabDataCommandName;
}


} // namespace AddOnCommands
