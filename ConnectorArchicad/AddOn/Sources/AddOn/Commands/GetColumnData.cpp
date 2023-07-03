#include "GetColumnData.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Level.hpp"
#include "Objects/Point.hpp"
#include "RealNumber.h"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"
using namespace FieldNames;

namespace AddOnCommands
{


GS::String GetColumnData::GetFieldName () const
{
	return Columns;
}


API_ElemTypeID GetColumnData::GetElemTypeID () const
{
	return API_ColumnID;
}


GS::UInt64 GetColumnData::GetMemoMask () const
{
	return APIMemoMask_ColumnSegment |
		APIMemoMask_AssemblySegmentScheme |
		APIMemoMask_AssemblySegmentCut;
}


GS::ErrCode	GetColumnData::SerializeElementType (const API_Element& elem,
	const API_ElementMemo& memo,
	GS::ObjectState& os) const
{
	GS::ErrCode err = NoError;
	err = GetDataCommand::SerializeElementType (elem, memo, os);
	if (NoError != err)
		return err;

	// Positioning - geometry
	API_StoryType story = Utility::GetStory (elem.column.head.floorInd);
	os.Add (ElementBase::Level, Objects::Level (story));

	double z = Utility::GetStoryLevel (elem.column.head.floorInd) + elem.column.bottomOffset;
	os.Add (Column::origoPos, Objects::Point3D (elem.column.origoPos.x, elem.column.origoPos.y, z));
	os.Add (Column::height, elem.column.height);

	// Positioning - story relation
	os.Add (Column::bottomOffset, elem.column.bottomOffset);
	os.Add (Column::topOffset, elem.column.topOffset);
	os.Add (Column::relativeTopStory, elem.column.relativeTopStory);

	// Positioning - slanted column
	os.Add (Column::isSlanted, elem.column.isSlanted);
	os.Add (Column::slantAngle, elem.column.slantAngle);
	os.Add (Column::slantDirectionAngle, elem.column.slantDirectionAngle);

	os.Add (Column::isFlipped, elem.column.isFlipped);

	// Positioning - wrapping
	os.Add (Column::Wrapping, elem.column.wrapping);

	// Positioning - Defines the relation of column to zones (Zone Boundary, Reduce Zone Area Only, No Effect on Zones)
	os.Add (Column::ColumnRelationToZoneName, relationToZoneNames.Get (elem.column.zoneRel));

	// End Cuts
	os.Add (Column::nCuts, elem.column.nCuts);
	if (memo.assemblySegmentCuts != nullptr) {
		GS::ObjectState allCuts;
		Utility::GetAllCutData (memo.assemblySegmentCuts, allCuts);
		os.Add (AssemblySegment::CutData, allCuts);
	}

	// Reference Axis
	os.Add (Column::coreAnchor, elem.column.coreAnchor);
	os.Add (Column::axisRotationAngle, elem.column.axisRotationAngle);

	// Segment
	API_Attribute attrib;
	os.Add (Column::nSegments, elem.column.nSegments);
	os.Add (Column::nProfiles, elem.column.nProfiles);

	bool NotOnlyProfileSegment = false;
	if (memo.columnSegments != nullptr) {
		GS::ObjectState allSegments;

		GSSize segmentsCount = BMGetPtrSize (reinterpret_cast<GSPtr>(memo.columnSegments)) / sizeof (API_ColumnSegmentType);
		DBASSERT (segmentsCount == elem.column.nSegments);

		for (GSSize idx = 0; idx < segmentsCount; ++idx) {
			API_ColumnSegmentType columnSegment = memo.columnSegments[idx];
			GS::ObjectState currentSegment;

			GS::ObjectState assemblySegment;
			Utility::GetSegmentData (columnSegment.assemblySegmentData, assemblySegment);
			currentSegment.Add (Column::ColumnSegment::segmentData, assemblySegment);

			if (columnSegment.assemblySegmentData.modelElemStructureType != API_ProfileStructure)
				NotOnlyProfileSegment = true;

			// Veneer attributes
			if (abs (columnSegment.venThick) > EPS) {
				// Veneer type
				currentSegment.Add (Column::ColumnSegment::VenType, venTypeNames.Get (columnSegment.venType));

				// Veneer building material
				BNZeroMemory (&attrib, sizeof (API_Attribute));
				attrib.header.typeID = API_BuildingMaterialID;
				attrib.header.index = columnSegment.venBuildingMaterial;
				ACAPI_Attribute_Get (&attrib);

				currentSegment.Add (Column::ColumnSegment::VenBuildingMaterial, GS::UniString{attrib.header.name});

				// Veneer thick
				currentSegment.Add (Column::ColumnSegment::VenThick, columnSegment.venThick);
			}

			// The extrusion overridden material name
			int countOverriddenMaterial = 0;
			if (columnSegment.extrusionSurfaceMaterial.overridden) {
				BNZeroMemory (&attrib, sizeof (API_Attribute));
				attrib.header.typeID = API_MaterialID;
				attrib.header.index = columnSegment.extrusionSurfaceMaterial.attributeIndex;

				if (NoError == ACAPI_Attribute_Get (&attrib))
					countOverriddenMaterial = countOverriddenMaterial + 1;
				currentSegment.Add (Column::ColumnSegment::ExtrusionSurfaceMaterial, GS::UniString{attrib.header.name});
			}

			// The ends overridden material name
			if (columnSegment.endsMaterial.overridden) {
				BNZeroMemory (&attrib, sizeof (API_Attribute));
				attrib.header.typeID = API_MaterialID;
				attrib.header.index = columnSegment.endsMaterial.attributeIndex;

				if (NoError == ACAPI_Attribute_Get (&attrib))
					countOverriddenMaterial = countOverriddenMaterial + 1;
				currentSegment.Add (Column::ColumnSegment::EndsSurfaceMaterial, GS::UniString{attrib.header.name});
			}

			// The overridden materials are chained
			if (countOverriddenMaterial > 1) {
				currentSegment.Add (Column::ColumnSegment::MaterialsChained, columnSegment.materialsChained);
			}

			allSegments.Add (GS::String::SPrintf (AssemblySegment::SegmentName, idx + 1), currentSegment);
		}

		os.Add (Column::segments, allSegments);
	}

	// Scheme
	os.Add (Column::nSchemes, elem.column.nSchemes);

	if (memo.assemblySegmentSchemes != nullptr) {
		GS::ObjectState allSchemes;
		Utility::GetAllSchemeData (memo.assemblySegmentSchemes, allSchemes);
		os.Add (AssemblySegment::SchemeData, allSchemes);
	}

	// Floor Plan and Section - Floor Plan Display

	// Show on Stories - Story visibility
	Utility::GetVisibility (elem.column.isAutoOnStoryVisibility, elem.column.visibility, os, ShowOnStories);

	// The display options (Projected, Projected with Overhead, Cut Only, Outlines Only, Overhead All or Symbolic Cut)
	os.Add (Column::DisplayOptionName, displayOptionNames.Get (elem.column.displayOption));

	// Show projection (To Floor Plan Range, To Absolute Display Limit, Entire Element)
	os.Add (Column::ViewDepthLimitationName, viewDepthLimitationNames.Get (elem.column.viewDepthLimitation));

	// Floor Plan and Section - Cut Surfaces

	// The pen index and linetype name of column core and veneer contour line
	if (NotOnlyProfileSegment) {
		// Core
		os.Add (Column::corePen, elem.column.corePen);

		BNZeroMemory (&attrib, sizeof (API_Attribute));
		attrib.header.typeID = API_LinetypeID;
		attrib.header.index = elem.column.contLtype;

		if (NoError == ACAPI_Attribute_Get (&attrib))
			os.Add (Column::CoreLinetypeName, GS::UniString{attrib.header.name});

		// Veneer
		os.Add (Column::VeneerPenIndex, elem.column.venLinePen);

		BNZeroMemory (&attrib, sizeof (API_Attribute));
		attrib.header.typeID = API_LinetypeID;
		attrib.header.index = elem.column.venLineType;

		if (NoError == ACAPI_Attribute_Get (&attrib))
			os.Add (Column::VeneerLinetypeName, GS::UniString{attrib.header.name});
	}

	// Override cut fill pen
	if (elem.column.penOverride.overrideCutFillPen) {
		os.Add (Column::OverrideCutFillPenIndex, elem.column.penOverride.cutFillPen);
	}

	// Override cut fill backgound pen
	if (elem.column.penOverride.overrideCutFillBackgroundPen) {
		os.Add (Column::OverrideCutFillBackgroundPenIndex, elem.column.penOverride.cutFillBackgroundPen);
	}

	// Floor Plan and Section - Outlines
	;
	// The pen index of column uncut contour line
	os.Add (Column::UncutLinePenIndex, elem.column.belowViewLinePen);

	// The linetype name of column uncut contour line
	BNZeroMemory (&attrib, sizeof (API_Attribute));
	attrib.header.typeID = API_LinetypeID;
	attrib.header.index = elem.column.belowViewLineType;

	if (NoError == ACAPI_Attribute_Get (&attrib))
		os.Add (Column::UncutLinetypeName, GS::UniString{attrib.header.name});

	// The pen index of column overhead contour line
	os.Add (Column::OverheadLinePenIndex, elem.column.aboveViewLinePen);

	// The linetype name of column overhead contour line
	BNZeroMemory (&attrib, sizeof (API_Attribute));
	attrib.header.typeID = API_LinetypeID;
	attrib.header.index = elem.column.aboveViewLineType;

	if (NoError == ACAPI_Attribute_Get (&attrib))
		os.Add (Column::OverheadLinetypeName, GS::UniString{attrib.header.name});

	// The pen index of column hidden contour line
	os.Add (Column::HiddenLinePenIndex, elem.column.hiddenLinePen);

	// The linetype name of column hidden contour line
	BNZeroMemory (&attrib, sizeof (API_Attribute));
	attrib.header.typeID = API_LinetypeID;
	attrib.header.index = elem.column.hiddenLineType;

	if (NoError == ACAPI_Attribute_Get (&attrib))
		os.Add (Column::HiddenLinetypeName, GS::UniString{attrib.header.name});

	// Floor Plan and Section - Floor Plan Symbol

	// Core Symbol Type (Plain, Slash, X, Crosshair)
	os.Add (Column::CoreSymbolTypeName, coreSymbolTypeNames.Get (elem.column.coreSymbolType));

	// Core Symbol Lengths
	os.Add (Column::coreSymbolPar1, elem.column.coreSymbolPar1);
	os.Add (Column::coreSymbolPar2, elem.column.coreSymbolPar2);

	// Core Symbol Pen
	os.Add (Column::CoreSymbolPenIndex, elem.column.coreSymbolPen);

	// Floor Plan and Section - Cover Fills
	os.Add (Column::useCoverFill, elem.column.useCoverFill);
	if (elem.column.useCoverFill) {
		os.Add (Column::useCoverFillFromSurface, elem.column.useCoverFillFromSurface);
		os.Add (Column::coverFillForegroundPen, elem.column.coverFillForegroundPen);
		os.Add (Column::coverFillBackgroundPen, elem.column.coverFillBackgroundPen);

		// Cover fill type
		if (!elem.column.useCoverFillFromSurface) {

			BNZeroMemory (&attrib, sizeof (API_Attribute));
			attrib.header.typeID = API_FilltypeID;
			attrib.header.index = elem.column.coverFillType;

			if (NoError == ACAPI_Attribute_Get (&attrib))
				os.Add (Column::coverFillType, GS::UniString{attrib.header.name});
		}

		// Cover Fill Transformation
		Utility::GetCoverFillTransformation (elem.column.coverFillOrientationComesFrom3D, elem.column.coverFillTransformationType, os);

		if ((elem.column.coverFillTransformationType == API_CoverFillTransformationType_Rotated || elem.column.coverFillTransformationType == API_CoverFillTransformationType_Distorted) && !elem.column.coverFillOrientationComesFrom3D) {
			os.Add (Column::CoverFillTransformationOrigoX, elem.column.coverFillTransformation.origo.x);
			os.Add (Column::CoverFillTransformationOrigoY, elem.column.coverFillTransformation.origo.y);
			os.Add (Column::CoverFillTransformationXAxisX, elem.column.coverFillTransformation.xAxis.x);
			os.Add (Column::CoverFillTransformationXAxisY, elem.column.coverFillTransformation.xAxis.y);
			os.Add (Column::CoverFillTransformationYAxisX, elem.column.coverFillTransformation.yAxis.x);
			os.Add (Column::CoverFillTransformationYAxisY, elem.column.coverFillTransformation.yAxis.y);
		}
	}

	return NoError;
}


GS::String GetColumnData::GetName () const
{
	return GetColumnDataCommandName;
}


} // namespace AddOnCommands
