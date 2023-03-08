#include "GetSlabData.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Polyline.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"
using namespace FieldNames;


namespace AddOnCommands {


GS::ObjectState SerializeSlabType (const API_SlabType& slab, const API_ElementMemo& memo)
{
	GS::ObjectState os;

	// The identifier of the slab
	os.Add (ApplicationId, APIGuidToString (slab.head.guid));

	// Geometry and positioning
	// The index of the slab's floor
	os.Add (FloorIndex, slab.head.floorInd);

	// The shape of the slab
	double level = Utility::GetStoryLevel (slab.head.floorInd) + slab.level;
	os.Add (Shape, Objects::ElementShape (slab.poly, memo, level));

	// The thickness of the slab
	os.Add (Slab::Thickness, slab.thickness);

	// The structure type of the slab (basic or composite)
	os.Add (Slab::Structure, structureTypeNames.Get (slab.modelElemStructureType));

	// The building material name or composite name of the slab
	API_Attribute attribute;
	switch (slab.modelElemStructureType) {
	case API_BasicStructure:
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_BuildingMaterialID;
		attribute.header.index = slab.buildingMaterial;

		if (NoError == ACAPI_Attribute_Get (&attribute))
			os.Add (Slab::BuildingMaterialName, GS::UniString{attribute.header.name});
		break;
	case API_CompositeStructure:
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_CompWallID;
		attribute.header.index = slab.composite;

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
	os.Add (Slab::ReferencePlaneLocation, referencePlaneLocationNames.Get (slab.referencePlaneLocation));

	// Floor Plan and Section - Floor Plan Display

	// Show on Stories - Story visibility
	{
		GS::UniString visibilityFillString;
		Utility::GetVisibility (false, slab.visibilityFill, visibilityFillString);

		GS::UniString visibilityContString;
		Utility::GetVisibility (false, slab.visibilityCont, visibilityContString);
		
		if (visibilityFillString == visibilityContString && visibilityFillString != CustomStoriesValueName) {
			os.Add (ShowOnStories, visibilityContString);
		}
		else {
			os.Add (ShowOnStories, CustomStoriesValueName);
			
			Utility::ExportVisibility (false, slab.visibilityFill, os, VisibilityFillData, true);
			Utility::ExportVisibility (false, slab.visibilityCont, os, VisibilityContData, true);
		}
	}

	// Floor Plan and Section - Cut Surfaces

	// The pen index and linetype name of beam section line
	API_Attribute attrib;
	os.Add (Slab::sectContPen, slab.sectContPen);

	BNZeroMemory (&attrib, sizeof (API_Attribute));
	attrib.header.typeID = API_LinetypeID;
	attrib.header.index = slab.sectContLtype;

	if (NoError == ACAPI_Attribute_Get (&attrib))
		os.Add (Slab::sectContLtype, GS::UniString{attrib.header.name});

	// Override cut fill pen
	if (slab.penOverride.overrideCutFillPen) {
		os.Add (Slab::cutFillPen, slab.penOverride.cutFillPen);
	}

	// Override cut fill backgound pen
	if (slab.penOverride.overrideCutFillBackgroundPen) {
		os.Add (Slab::cutFillBackgroundPen, slab.penOverride.cutFillBackgroundPen);
	}

	// Outlines

	// The pen index and linetype name of beam contour line
	os.Add (Slab::contourPen, slab.pen);

	BNZeroMemory (&attrib, sizeof (API_Attribute));
	attrib.header.typeID = API_LinetypeID;
	attrib.header.index = slab.ltypeInd;

	if (NoError == ACAPI_Attribute_Get (&attrib))
		os.Add (Slab::contourLineType, GS::UniString{attrib.header.name});

	// The pen index and linetype name of beam hidden contour line
	os.Add (Slab::hiddenContourLinePen, slab.hiddenContourLinePen);

	BNZeroMemory (&attrib, sizeof (API_Attribute));
	attrib.header.typeID = API_LinetypeID;
	attrib.header.index = slab.hiddenContourLineType;

	if (NoError == ACAPI_Attribute_Get (&attrib))
		os.Add (Slab::hiddenContourLineType, GS::UniString{attrib.header.name});

	// Floor Plan and Section - Cover Fills
	os.Add (Slab::useFloorFill, slab.useFloorFill);
	if (slab.useFloorFill) {
		os.Add (Slab::use3DHatching, slab.use3DHatching);
		os.Add (Slab::floorFillPen, slab.floorFillPen);
		os.Add (Slab::floorFillBGPen, slab.floorFillBGPen);

		// Cover fill type
		if (!slab.use3DHatching) {

			BNZeroMemory (&attrib, sizeof (API_Attribute));
			attrib.header.typeID = API_FilltypeID;
			attrib.header.index = slab.floorFillInd;

			if (NoError == ACAPI_Attribute_Get (&attrib))
				os.Add (Slab::floorFillName, GS::UniString{attrib.header.name});
		}

		// Hatch Orientation
		Utility::ExportHatchOrientation (slab.hatchOrientation.type, os);

		if (slab.hatchOrientation.type == API_HatchRotated || slab.hatchOrientation.type == API_HatchDistorted) {
			os.Add (Slab::hatchOrientationOrigoX, slab.hatchOrientation.origo.x);
			os.Add (Slab::hatchOrientationOrigoY, slab.hatchOrientation.origo.y);
			os.Add (Slab::hatchOrientationXAxisX, slab.hatchOrientation.matrix00);
			os.Add (Slab::hatchOrientationXAxisY, slab.hatchOrientation.matrix10);
			os.Add (Slab::hatchOrientationYAxisX, slab.hatchOrientation.matrix01);
			os.Add (Slab::hatchOrientationYAxisY, slab.hatchOrientation.matrix11);
		}
	}

	// Model

	// Overridden materials
	int countOverriddenMaterial = 0;
	if (slab.topMat.overridden) {
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_MaterialID;
		attribute.header.index = slab.topMat.attributeIndex;

		if (NoError == ACAPI_Attribute_Get (&attribute))
			countOverriddenMaterial = countOverriddenMaterial + 1;

		os.Add (Slab::topMat, GS::UniString{attribute.header.name});
	}

	if (slab.sideMat.overridden) {
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_MaterialID;
		attribute.header.index = slab.sideMat.attributeIndex;

		if (NoError == ACAPI_Attribute_Get (&attribute))
			countOverriddenMaterial = countOverriddenMaterial + 1;

		os.Add (Slab::sideMat, GS::UniString{attribute.header.name});
	}

	if (slab.botMat.overridden) {
		BNZeroMemory (&attribute, sizeof (API_Attribute));
		attribute.header.typeID = API_MaterialID;
		attribute.header.index = slab.botMat.attributeIndex;

		if (NoError == ACAPI_Attribute_Get (&attribute))
			countOverriddenMaterial = countOverriddenMaterial + 1;

		os.Add (Slab::botMat, GS::UniString{attribute.header.name});
	}

	// The overridden materials are chained
	if (countOverriddenMaterial > 1) {
		os.Add (Slab::materialsChained, slab.materialsChained);
	}


	return os;
}


GS::String GetSlabData::GetName () const
{
	return GetSlabDataCommandName;
}


GS::ObjectState GetSlabData::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const
{
	GS::Array<GS::UniString> ids;
	parameters.Get (ApplicationIds, ids);
	GS::Array<API_Guid>	elementGuids = ids.Transform<API_Guid> ([] (const GS::UniString& idStr) { return APIGuidFromString (idStr.ToCStr ()); });

	GS::ObjectState result;

	const auto& listAdder = result.AddList<GS::ObjectState> (Slabs);
	for (const API_Guid& guid : elementGuids) {

		API_Element element{};
		API_ElementMemo elementMemo{};

		element.header.guid = guid;
		GSErrCode err = ACAPI_Element_Get (&element);
		if (err != NoError) continue;

#ifdef ServerMainVers_2600
		if (element.header.type.typeID != API_SlabID)
#else
		if (element.header.typeID != API_SlabID)
#endif
			continue;

		err = ACAPI_Element_GetMemo (guid, &elementMemo);
		if (err != NoError) continue;

		listAdder (SerializeSlabType (element.slab, elementMemo));
	}

	return result;
}


}