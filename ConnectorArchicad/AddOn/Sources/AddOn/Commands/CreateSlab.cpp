#include "CreateSlab.hpp"
#include "APIMigrationHelper.hpp"
#include "CommandHelpers.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Level.hpp"
#include "Objects/Polyline.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"
#include "AngleData.h"
#include "OnExit.hpp"
using namespace FieldNames;


namespace AddOnCommands {


GS::String CreateSlab::GetFieldName () const
{
	return Slabs;
}


GS::UniString CreateSlab::GetUndoableCommandName () const
{
	return "CreateSpeckleSlab";
}


GSErrCode CreateSlab::GetElementFromObjectState (const GS::ObjectState& os,
	API_Element& element,
	API_Element& mask,
	API_ElementMemo& memo,
	GS::UInt64& memoMask,
	API_SubElement** /*marker*/,
	AttributeManager& /*attributeManager*/,
	LibpartImportManager& /*libpartImportManager*/,
	GS::Array<GS::UniString>& log) const
{
	GSErrCode err = NoError;
	
	Utility::SetElementType (element.header, API_SlabID);
	err = Utility::GetBaseElementData (element, &memo, nullptr, log);
	if (err != NoError)
		return err;

	err = GetElementBaseFromObjectState (os, element, mask);
	if (err != NoError)
		return err;

	// Geometry and positioning
	memoMask = APIMemoMask_Polygon | APIMemoMask_SideMaterials | APIMemoMask_EdgeTrims;

	// The shape of the slab
	Objects::ElementShape slabShape;

	if (os.Contains (ElementBase::Shape)) {
		os.Get (ElementBase::Shape, slabShape);
		element.slab.poly.nSubPolys = slabShape.SubpolyCount ();
		element.slab.poly.nCoords = slabShape.VertexCount ();
		element.slab.poly.nArcs = slabShape.ArcCount ();

		ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, poly.nSubPolys);
		ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, poly.nCoords);
		ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, poly.nArcs);

		slabShape.SetToMemo (memo, Objects::ElementShape::MemoMainPolygon);
	}

	// The floor index and level of the slab
	if (os.Contains (ElementBase::Level)) {
		GetStoryFromObjectState (os, slabShape.Level (), element.header.floorInd, element.slab.level);
	}
	else {
		Utility::SetStoryLevelAndFloor (slabShape.Level (), element.header.floorInd, element.slab.level);
	}
	ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, level);
	ACAPI_ELEMENT_MASK_SET (mask, API_Elem_Head, floorInd);

	// The thickness of the slab
	if (os.Contains (Slab::Thickness)) {
		os.Get (Slab::Thickness, element.slab.thickness);
		ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, thickness);
	}

	// The structure of the slab
	if (os.Contains (Slab::Structure)) {
		GS::UniString structureName;
		os.Get (Slab::Structure, structureName);

		GS::Optional<API_ModelElemStructureType> type = structureTypeNames.FindValue (structureName);
		if (type.HasValue ())
			element.slab.modelElemStructureType = type.Get ();

		ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, modelElemStructureType);
	}

	// The building material name of the slab
	GS::UniString attributeName;
	if (os.Contains (Slab::BuildingMaterialName) &&
		element.slab.modelElemStructureType == API_BasicStructure) {

		os.Get (Slab::BuildingMaterialName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_BuildingMaterialID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				element.slab.buildingMaterial = attribute.header.index;
		}
		ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, buildingMaterial);
	}

	// The composite name of the slab
	if (os.Contains (Slab::CompositeName) &&
		element.slab.modelElemStructureType == API_CompositeStructure) {

		os.Get (Slab::CompositeName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_CompWallID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				element.slab.composite = attribute.header.index;
		}
		ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, composite);
	}

	// The edge type of the slab
	API_EdgeTrimID edgeType = APIEdgeTrim_Perpendicular;
	if (os.Contains (Slab::EdgeAngleType)) {
		GS::UniString edgeTypeName;
		os.Get (Slab::EdgeAngleType, edgeTypeName);

		GS::Optional<API_EdgeTrimID> type = edgeAngleTypeNames.FindValue (edgeTypeName);
		if (type.HasValue ())
			edgeType = type.Get ();
	}

	// The edge angle of the slab
	GS::Optional<double> edgeAngle;
	if (os.Contains (Slab::EdgeAngle)) {
		double angle = 0;
		os.Get (Slab::EdgeAngle, angle);
		edgeAngle = angle;
	}

	// Setting side materials and edge angles
	BMhKill ((GSHandle*) &memo.edgeTrims);
	BMhKill ((GSHandle*) &memo.edgeIDs);

	memo.edgeTrims = (API_EdgeTrim**) BMAllocateHandle ((element.slab.poly.nCoords + 1) * sizeof (API_EdgeTrim), ALLOCATE_CLEAR, 0);

	//Ignore memo side materials because export and import do not work properly
	//BMpFree (reinterpret_cast<GSPtr> (memo.sideMaterials));
	//memo.sideMaterials = (API_OverriddenAttribute*) BMAllocatePtr ((element.slab.poly.nCoords + 1) * sizeof (API_OverriddenAttribute), ALLOCATE_CLEAR, 0);
	for (Int32 k = 1; k <= element.slab.poly.nCoords; ++k) {
		//memo.sideMaterials[k] = element.slab.sideMat;

		(*(memo.edgeTrims))[k].sideType = edgeType;
		(*(memo.edgeTrims))[k].sideAngle = (edgeAngle.HasValue ()) ? edgeAngle.Get () : PI / 2;
	}

	// The reference plane location of the slab
	if (os.Contains (Slab::ReferencePlaneLocation)) {
		GS::UniString refPlaneLocationName;
		os.Get (Slab::ReferencePlaneLocation, refPlaneLocationName);

		GS::Optional<API_SlabReferencePlaneLocationID> id = referencePlaneLocationNames.FindValue (refPlaneLocationName);
		if (id.HasValue ())
			element.slab.referencePlaneLocation = id.Get ();

		ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, referencePlaneLocation);
	}

	// Floor Plan and Section - Floor Plan Display

	// Show on Stories - Story visibility
	bool isAutoOnStoryVisibility = false;
	Utility::CreateVisibility (os, VisibilityContData, isAutoOnStoryVisibility, element.slab.visibilityCont);
	ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, visibilityCont.showOnHome);
	ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, visibilityCont.showAllAbove);
	ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, visibilityCont.showAllBelow);
	ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, visibilityCont.showRelAbove);
	ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, visibilityCont.showRelBelow);

	Utility::CreateVisibility (os, VisibilityFillData, isAutoOnStoryVisibility, element.slab.visibilityFill);
	ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, visibilityFill.showOnHome);
	ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, visibilityFill.showAllAbove);
	ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, visibilityFill.showAllBelow);
	ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, visibilityFill.showRelAbove);
	ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, visibilityFill.showRelBelow);

	// Floor Plan and Section - Cut Surfaces

	// The pen index and linetype name of slab section line
	if (os.Contains (Slab::sectContPen)) {
		os.Get (Slab::sectContPen, element.slab.sectContPen);
		ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, sectContPen);
	}

	if (os.Contains (Slab::sectContLtype)) {

		os.Get (Slab::sectContLtype, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				element.slab.sectContLtype = attribute.header.index;
		}
		ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, sectContLtype);
	}

	// Override cut fill and cut fill backgound pens
	if (CommandHelpers::SetCutfillPens(
		os,
		Slab::cutFillPen,
		Slab::cutFillBackgroundPen,
		element.slab,
		mask)
		!= NoError)
		return Error;

	// Outlines

	// The pen index and linetype name of slab contour line
	if (os.Contains (Slab::contourPen)) {
		os.Get (Slab::contourPen, element.slab.pen);
		ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, pen);
	}

	if (os.Contains (Slab::contourLineType)) {

		os.Get (Slab::contourLineType, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				element.slab.ltypeInd = attribute.header.index;
		}
		ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, ltypeInd);
	}

	// The pen index and linetype name of slab hidden contour line
	if (os.Contains (Slab::hiddenContourLinePen)) {
		os.Get (Slab::hiddenContourLinePen, element.slab.hiddenContourLinePen);
		ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, hiddenContourLinePen);
	}

	if (os.Contains (Slab::hiddenContourLineType)) {

		os.Get (Slab::hiddenContourLineType, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				element.slab.hiddenContourLineType = attribute.header.index;
		}
		ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, hiddenContourLineType);
	}

	// Floor Plan and Section - Cover Fills
	if (os.Contains (Slab::useFloorFill)) {
		os.Get (Slab::useFloorFill, element.slab.useFloorFill);
		ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, useFloorFill);
	}

	if (os.Contains (Slab::use3DHatching)) {
		os.Get (Slab::use3DHatching, element.slab.use3DHatching);
		ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, use3DHatching);
	}

	if (os.Contains (Slab::floorFillPen)) {
		os.Get (Slab::floorFillPen, element.slab.floorFillPen);
		ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, floorFillPen);
	}

	if (os.Contains (Slab::floorFillBGPen)) {
		os.Get (Slab::floorFillBGPen, element.slab.floorFillBGPen);
		ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, floorFillBGPen);
	}

	// Cover fill type
	if (os.Contains (Slab::floorFillName)) {

		os.Get (Slab::floorFillName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_FilltypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				element.slab.floorFillInd = attribute.header.index;
		}
		ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, floorFillInd);
	}

	// Cover Fill Transformation
	Utility::CreateHatchOrientation (os, element.slab.hatchOrientation.type);
	ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, hatchOrientation.type);

	if (os.Contains (Slab::hatchOrientationOrigoX)) {
		os.Get (Slab::hatchOrientationOrigoX, element.slab.hatchOrientation.origo.x);
		ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, hatchOrientation.origo.x);
	}

	if (os.Contains (Slab::hatchOrientationOrigoY)) {
		os.Get (Slab::hatchOrientationOrigoY, element.slab.hatchOrientation.origo.y);
		ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, hatchOrientation.origo.y);
	}

	if (os.Contains (Slab::hatchOrientationXAxisX)) {
		os.Get (Slab::hatchOrientationXAxisX, element.slab.hatchOrientation.matrix00);
		ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, hatchOrientation.matrix00);
	}

	if (os.Contains (Slab::hatchOrientationXAxisY)) {
		os.Get (Slab::hatchOrientationXAxisY, element.slab.hatchOrientation.matrix10);
		ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, hatchOrientation.matrix10);
	}

	if (os.Contains (Slab::hatchOrientationYAxisX)) {
		os.Get (Slab::hatchOrientationYAxisX, element.slab.hatchOrientation.matrix01);
		ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, hatchOrientation.matrix01);
	}

	if (os.Contains (Slab::hatchOrientationYAxisY)) {
		os.Get (Slab::hatchOrientationYAxisY, element.slab.hatchOrientation.matrix11);
		ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, hatchOrientation.matrix11);
	}

	// Model

	// Overridden materials
	ResetAPIOverriddenAttribute (element.slab.topMat);
	if (os.Contains (Slab::topMat)) {

		os.Get (Slab::topMat, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_MaterialID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				SetAPIOverriddenAttribute (element.slab.topMat, attribute.header.index);
				ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, GetAPIOverriddenAttributeIndexField (topMat));
			}
		}
	}
	ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, GetAPIOverriddenAttributeBoolField (topMat));

	ResetAPIOverriddenAttribute (element.slab.sideMat);
	if (os.Contains (Slab::sideMat)) {

		os.Get (Slab::sideMat, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_MaterialID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				SetAPIOverriddenAttribute (element.slab.sideMat, attribute.header.index);
				ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, GetAPIOverriddenAttributeIndexField (sideMat));
			}
		}
	}
	ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, GetAPIOverriddenAttributeBoolField (sideMat));

	ResetAPIOverriddenAttribute (element.slab.botMat);
	if (os.Contains (Slab::botMat)) {

		os.Get (Slab::botMat, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_MaterialID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute)) {
				SetAPIOverriddenAttribute (element.slab.botMat, attribute.header.index);
				ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, GetAPIOverriddenAttributeIndexField (botMat));
			}
		}
	}
	ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, GetAPIOverriddenAttributeBoolField (botMat));

	// The overridden materials are chained
	if (os.Contains (Slab::materialsChained)) {
		os.Get (Slab::materialsChained, element.slab.materialsChained);
		ACAPI_ELEMENT_MASK_SET (mask, API_SlabType, materialsChained);
	}

	return NoError;
}


GS::String CreateSlab::GetName () const
{
	return CreateSlabCommandName;
}


} // namespace AddOnCommands
