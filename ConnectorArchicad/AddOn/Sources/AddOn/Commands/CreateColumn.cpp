#include "CreateColumn.hpp"
#include "APIMigrationHelper.hpp"
#include "CommandHelpers.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Level.hpp"
#include "Objects/Point.hpp"
#include "RealNumber.h"
#include "DGModule.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"
using namespace FieldNames;

namespace AddOnCommands
{


GS::String CreateColumn::GetFieldName () const
{
	return Columns;
}


GS::UniString CreateColumn::GetUndoableCommandName () const
{
	return "CreateSpeckleColumn";
}


GSErrCode CreateColumn::GetElementFromObjectState (const GS::ObjectState& os,
	API_Element& element,
	API_Element& elementMask,
	API_ElementMemo& memo,
	GS::UInt64& memoMask,
	API_SubElement** /*marker*/,
	AttributeManager& /*attributeManager*/,
	LibpartImportManager& /*libpartImportManager*/,
	GS::Array<GS::UniString>& log) const
{
	GSErrCode err = NoError;

	Utility::SetElementType (element.header, API_ColumnID);
	err = Utility::GetBaseElementData (element, &memo, nullptr, log);
	if (err != NoError)
		return err;

	err = GetElementBaseFromObjectState (os, element, elementMask);
	if (err != NoError)
		return err;

	// Positioning - geometry
	Objects::Point3D origoPos;
	if (os.Contains (Column::origoPos)) {
		os.Get (Column::origoPos, origoPos);
		element.column.origoPos = origoPos.ToAPI_Coord ();
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, origoPos);
	}


	if (os.Contains (ElementBase::Level)) {
		GetStoryFromObjectState (os, origoPos.z, element.header.floorInd, element.column.bottomOffset);
	} else {
		Utility::SetStoryLevelAndFloor (origoPos.z, element.header.floorInd, element.column.bottomOffset);
	}
	ACAPI_ELEMENT_MASK_SET (elementMask, API_Elem_Head, floorInd);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, bottomOffset);

	if (os.Contains (Column::height)) {
		os.Get (Column::height, element.column.height);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, height);
	}

	// Positioning - story relation
	if (os.Contains (Column::bottomOffset)) {
		os.Get (Column::bottomOffset, element.column.bottomOffset);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, bottomOffset);
	}

	if (os.Contains (Column::topOffset)) {
		os.Get (Column::topOffset, element.column.topOffset);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, topOffset);
	}

	if (os.Contains (Column::relativeTopStory)) {
		os.Get (Column::relativeTopStory, element.column.relativeTopStory);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, relativeTopStory);
	}

	// Positioning - slanted column
	if (os.Contains (Column::isSlanted)) {
		os.Get (Column::isSlanted, element.column.isSlanted);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, isSlanted);
	}

	if (os.Contains (Column::slantAngle)) {
		os.Get (Column::slantAngle, element.column.slantAngle);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, slantAngle);
	}

	if (os.Contains (Column::slantDirectionAngle)) {
		os.Get (Column::slantDirectionAngle, element.column.slantDirectionAngle);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, slantDirectionAngle);
	}

	if (os.Contains (Column::isFlipped)) {
		os.Get (Column::isFlipped, element.column.isFlipped);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, isFlipped);
	}

	// Positioning - wrapping
	if (os.Contains (Column::Wrapping)) {
		os.Get (Column::Wrapping, element.column.wrapping);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, wrapping);
	}

	// Model - Defines the relation of column to zones (Zone Boundary, Reduce Zone Area Only, No Effect on Zones)
	if (os.Contains (Column::ColumnRelationToZoneName)) {
		GS::UniString columnRelationToZoneName;
		os.Get (Column::ColumnRelationToZoneName, columnRelationToZoneName);

		GS::Optional<API_ZoneRelID> type = relationToZoneNames.FindValue (columnRelationToZoneName);
		if (type.HasValue ())
			element.column.zoneRel = type.Get ();

		ACAPI_ELEMENT_MASK_SET (elementMask, API_WallType, type);
	}

	// End Cuts
	if (os.Contains (Column::nCuts)) {
		os.Get (Column::nCuts, element.column.nCuts);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, nCuts);
	}

	Utility::CreateAllCutData (os, element.column.nCuts, element, elementMask, &memo);

	// Reference Axis
	if (os.Contains (Column::coreAnchor)) {
		os.Get (Column::coreAnchor, element.column.coreAnchor);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, coreAnchor);
	}

	if (os.Contains (Column::axisRotationAngle)) {
		os.Get (Column::axisRotationAngle, element.column.axisRotationAngle);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, axisRotationAngle);
	}

	// Segment
	if (os.Contains (Column::nSegments)) {
		os.Get (Column::nSegments, element.column.nSegments);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, nSegments);
	}

	if (os.Contains (Column::nProfiles)) {
		os.Get (Column::nProfiles, element.column.nProfiles);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, nProfiles);
	}

	API_ColumnSegmentType defaultColumnSegment;
	if (memo.columnSegments != nullptr) {
		defaultColumnSegment = memo.columnSegments[0];
		memo.columnSegments = (API_ColumnSegmentType*) BMAllocatePtr ((element.column.nSegments) * sizeof (API_ColumnSegmentType), ALLOCATE_CLEAR, 0);
	} else {
		return Error;
	}

	memoMask = APIMemoMask_ColumnSegment |
		APIMemoMask_AssemblySegmentScheme |
		APIMemoMask_AssemblySegmentCut;

#pragma region Segment
	GS::ObjectState allSegments;
	if (os.Contains (Column::segments))
		os.Get (Column::segments, allSegments);

	for (UInt32 idx = 0; idx < element.column.nSegments; ++idx) {
		GS::ObjectState currentSegment;
		allSegments.Get (GS::String::SPrintf (AssemblySegment::SegmentName, idx + 1), currentSegment);

		if (!currentSegment.IsEmpty ()) {

			memo.columnSegments[idx] = defaultColumnSegment;
			GS::ObjectState assemblySegment;
			currentSegment.Get (Column::ColumnSegment::segmentData, assemblySegment);
			Utility::CreateOneSegmentData (assemblySegment, memo.columnSegments[idx].assemblySegmentData, elementMask);

			// Veneer attributes
			if (currentSegment.Contains (Column::ColumnSegment::VenThick)) {
				// Veneer type
				API_VeneerTypeID venType = APIVeneer_Core;
				if (os.Contains (Column::ColumnSegment::VenType)) {
					GS::UniString venTypeName;
					currentSegment.Get (Column::ColumnSegment::VenType, venTypeName);
					GS::Optional<API_VeneerTypeID> type = venTypeNames.FindValue (venTypeName);
					if (type.HasValue ())
						venType = type.Get ();

					memo.columnSegments[idx].venType = venType;
					ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnSegmentType, venType);
				}

				// Veneer building material
				if (currentSegment.Contains (Column::ColumnSegment::VenBuildingMaterial)) {
					GS::UniString attrName;
					currentSegment.Get (Column::ColumnSegment::VenBuildingMaterial, attrName);

					if (!attrName.IsEmpty ()) {
						API_Attribute attrib;
						BNZeroMemory (&attrib, sizeof (API_Attribute));
						attrib.header.typeID = API_BuildingMaterialID;
						CHCopyC (attrName.ToCStr (), attrib.header.name);
						err = ACAPI_Attribute_Get (&attrib);

						if (err == NoError)
							memo.columnSegments[idx].venBuildingMaterial = attrib.header.index;
					}
				}

				// Veneer thick
				currentSegment.Get (Column::ColumnSegment::VenThick, memo.columnSegments[idx].venThick);
			} else {
				memo.columnSegments[idx].venThick = 0.0;
			}
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnSegmentType, venThick);

			// The extrusion overridden material name
			ResetAPIOverriddenAttribute (memo.columnSegments[idx].extrusionSurfaceMaterial);
			if (currentSegment.Contains (Column::ColumnSegment::ExtrusionSurfaceMaterial)) {

				GS::UniString attrName;
				currentSegment.Get (Column::ColumnSegment::ExtrusionSurfaceMaterial, attrName);

				if (!attrName.IsEmpty ()) {
					API_Attribute attrib;
					BNZeroMemory (&attrib, sizeof (API_Attribute));
					attrib.header.typeID = API_MaterialID;
					CHCopyC (attrName.ToCStr (), attrib.header.name);
					err = ACAPI_Attribute_Get (&attrib);

					if (err == NoError) {
						SetAPIOverriddenAttribute (memo.columnSegments[idx].extrusionSurfaceMaterial, attrib.header.index);
						ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnSegmentType, GetAPIOverriddenAttributeIndexField (extrusionSurfaceMaterial));
					}
				}
			}
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnSegmentType, GetAPIOverriddenAttributeBoolField (extrusionSurfaceMaterial));


			// The ends overridden material name
			ResetAPIOverriddenAttribute (memo.columnSegments[idx].endsMaterial);
			if (currentSegment.Contains (Column::ColumnSegment::EndsSurfaceMaterial)) {


				GS::UniString attrName;
				currentSegment.Get (Column::ColumnSegment::EndsSurfaceMaterial, attrName);

				if (!attrName.IsEmpty ()) {
					API_Attribute attrib;
					BNZeroMemory (&attrib, sizeof (API_Attribute));
					attrib.header.typeID = API_MaterialID;
					CHCopyC (attrName.ToCStr (), attrib.header.name);
					err = ACAPI_Attribute_Get (&attrib);

					if (err == NoError) {
						SetAPIOverriddenAttribute (memo.columnSegments[idx].endsMaterial, attrib.header.index);
						ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnSegmentType, GetAPIOverriddenAttributeIndexField (endsMaterial));
					}
				}
			}
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnSegmentType, GetAPIOverriddenAttributeBoolField (endsMaterial));

			// The overridden materials are chained
			if (currentSegment.Contains (Column::ColumnSegment::MaterialsChained))
				currentSegment.Get (Column::ColumnSegment::MaterialsChained, memo.columnSegments[idx].materialsChained);
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnSegmentType, materialsChained);
		}
	}
#pragma endregion

	// Scheme
	if (os.Contains (Column::nSchemes)) {
		os.Get (Column::nSchemes, element.column.nSchemes);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, nSchemes);
	}

	Utility::CreateAllSchemeData (os, element.column.nSchemes, element, elementMask, &memo);

	// Floor Plan and Section - Floor Plan Display

	// Story visibility
	Utility::CreateVisibility (os, "", element.column.isAutoOnStoryVisibility, element.column.visibility);

	ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, isAutoOnStoryVisibility);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, visibility.showOnHome);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, visibility.showAllAbove);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, visibility.showAllBelow);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, visibility.showRelAbove);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, visibility.showRelBelow);

	// The display options (Projected, Projected with Overhead, Cut Only, Outlines Only, Overhead All or Symbolic Cut)
	if (os.Contains (Column::DisplayOptionName)) {
		GS::UniString displayOptionName;
		os.Get (Column::DisplayOptionName, displayOptionName);

		GS::Optional<API_ElemDisplayOptionsID> type = displayOptionNames.FindValue (displayOptionName);
		if (type.HasValue ()) {
			element.column.displayOption = type.Get ();
			ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, displayOption);
		}
	}

	// Show projection (To Floor Plan Range, To Absolute Display Limit, Entire Element)
	if (os.Contains (Column::ViewDepthLimitationName)) {
		GS::UniString viewDepthLimitationName;
		os.Get (Column::ViewDepthLimitationName, viewDepthLimitationName);

		GS::Optional<API_ElemViewDepthLimitationsID> type = viewDepthLimitationNames.FindValue (viewDepthLimitationName);
		if (type.HasValue ())
			element.column.viewDepthLimitation = type.Get ();

		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, viewDepthLimitation);
	}

	// Floor Plan and Section - Cut Surfaces

	// The pen index of column core contour line
	if (os.Contains (Column::corePen)) {
		os.Get (Column::corePen, element.column.corePen);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, corePen);
	}

	// The linetype name of column core contour line
	GS::UniString attributeName;
	if (os.Contains (Column::CoreLinetypeName)) {

		os.Get (Column::CoreLinetypeName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				element.column.contLtype = attribute.header.index;
		}
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, contLtype);
	}

	// The pen index of column veneer contour line
	if (os.Contains (Column::VeneerPenIndex)) {
		os.Get (Column::VeneerPenIndex, element.column.venLinePen);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, venLinePen);
	}

	// The linetype name of column veneer contour line
	if (os.Contains (Column::VeneerLinetypeName)) {

		os.Get (Column::VeneerLinetypeName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				element.column.venLineType = attribute.header.index;
		}
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, venLineType);
	}

	// Override cut fill and cut fill backgound pens
	if (CommandHelpers::SetCutfillPens(
		os,
		Column::OverrideCutFillPenIndex,
		Column::OverrideCutFillBackgroundPenIndex,
		element.column,
		elementMask)
		!= NoError)
		return Error;

	// Floor Plan and Section - Outlines

	// The pen index of column uncut contour line
	if (os.Contains (Column::UncutLinePenIndex)) {
		os.Get (Column::UncutLinePenIndex, element.column.belowViewLinePen);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, belowViewLinePen);
	}

	// The linetype name of column uncut contour line
	if (os.Contains (Column::UncutLinetypeName)) {

		os.Get (Column::UncutLinetypeName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				element.column.belowViewLineType = attribute.header.index;
		}
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, belowViewLineType);
	}

	// The pen index of column overhead contour line
	if (os.Contains (Column::OverheadLinePenIndex)) {
		os.Get (Column::OverheadLinePenIndex, element.column.aboveViewLinePen);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, aboveViewLinePen);
	}

	// The linetype name of column overhead contour line
	if (os.Contains (Column::OverheadLinetypeName)) {

		os.Get (Column::OverheadLinetypeName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				element.column.aboveViewLineType = attribute.header.index;
		}
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, aboveViewLineType);
	}

	// The pen index of column hidden contour line
	if (os.Contains (Column::HiddenLinePenIndex)) {
		os.Get (Column::HiddenLinePenIndex, element.column.hiddenLinePen);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, hiddenLinePen);
	}

	// The linetype name of column hidden contour line
	if (os.Contains (Column::HiddenLinetypeName)) {

		os.Get (Column::HiddenLinetypeName, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_LinetypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				element.column.hiddenLineType = attribute.header.index;
		}
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, hiddenLineType);
	}

	// Floor Plan and Section - Floor Plan Symbol

	// Symbol Type (Plain, Slash, X, Crosshair)
	if (os.Contains (Column::CoreSymbolTypeName)) {
		GS::UniString coreSymbolTypeName;
		os.Get (Column::CoreSymbolTypeName, coreSymbolTypeName);

		GS::Optional<short> type = coreSymbolTypeNames.FindValue (coreSymbolTypeName);
		if (type.HasValue ())
			element.column.coreSymbolType = type.Get ();

		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, coreSymbolType);
	}

	// Core Symbol Lengths
	if (os.Contains (Column::coreSymbolPar1)) {
		os.Get (Column::coreSymbolPar1, element.column.coreSymbolPar1);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, coreSymbolPar1);
	}

	if (os.Contains (Column::coreSymbolPar2)) {
		os.Get (Column::coreSymbolPar2, element.column.coreSymbolPar2);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, coreSymbolPar2);
	}

	// Core Symbol Pen
	if (os.Contains (Column::CoreSymbolPenIndex)) {
		os.Get (Column::CoreSymbolPenIndex, element.column.coreSymbolPen);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, coreSymbolPen);
	}

	// Floor Plan and Section - Cover Fills
	if (os.Contains (Column::useCoverFill)) {
		os.Get (Column::useCoverFill, element.column.useCoverFill);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, useCoverFill);
	}

	if (os.Contains (Column::useCoverFillFromSurface)) {
		os.Get (Column::useCoverFillFromSurface, element.column.useCoverFillFromSurface);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, useCoverFillFromSurface);
	}

	if (os.Contains (Column::coverFillForegroundPen)) {
		os.Get (Column::coverFillForegroundPen, element.column.coverFillForegroundPen);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, coverFillForegroundPen);
	}

	if (os.Contains (Column::coverFillBackgroundPen)) {
		os.Get (Column::coverFillBackgroundPen, element.column.coverFillBackgroundPen);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, coverFillBackgroundPen);
	}

	// Cover fill type
	if (os.Contains (Column::coverFillType)) {

		os.Get (Column::coverFillType, attributeName);

		if (!attributeName.IsEmpty ()) {
			API_Attribute attribute;
			BNZeroMemory (&attribute, sizeof (API_Attribute));
			attribute.header.typeID = API_FilltypeID;
			CHCopyC (attributeName.ToCStr (), attribute.header.name);

			if (NoError == ACAPI_Attribute_Get (&attribute))
				element.column.coverFillType = attribute.header.index;
		}
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, coverFillType);
	}

	// Cover Fill Transformation
	Utility::CreateCoverFillTransformation (os, element.column.coverFillOrientationComesFrom3D, element.column.coverFillTransformationType);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, coverFillOrientationComesFrom3D);
	ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, coverFillTransformationType);

	if (os.Contains (Column::CoverFillTransformationOrigoX)) {
		os.Get (Column::CoverFillTransformationOrigoX, element.column.coverFillTransformation.origo.x);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, coverFillTransformation.origo.x);
	}

	if (os.Contains (Column::CoverFillTransformationOrigoY)) {
		os.Get (Column::CoverFillTransformationOrigoY, element.column.coverFillTransformation.origo.y);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, coverFillTransformation.origo.y);
	}

	if (os.Contains (Column::CoverFillTransformationXAxisX)) {
		os.Get (Column::CoverFillTransformationXAxisX, element.column.coverFillTransformation.xAxis.x);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, coverFillTransformation.xAxis.x);
	}

	if (os.Contains (Column::CoverFillTransformationXAxisY)) {
		os.Get (Column::CoverFillTransformationXAxisY, element.column.coverFillTransformation.xAxis.y);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, coverFillTransformation.xAxis.y);
	}

	if (os.Contains (Column::CoverFillTransformationYAxisX)) {
		os.Get (Column::CoverFillTransformationYAxisX, element.column.coverFillTransformation.yAxis.x);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, coverFillTransformation.yAxis.x);
	}

	if (os.Contains (Column::CoverFillTransformationYAxisY)) {
		os.Get (Column::CoverFillTransformationYAxisY, element.column.coverFillTransformation.yAxis.y);
		ACAPI_ELEMENT_MASK_SET (elementMask, API_ColumnType, coverFillTransformation.yAxis.y);
	}

	return err;
}
#pragma endregion


GS::String CreateColumn::GetName () const
{
	return CreateColumnCommandName;
}


} // namespace AddOnCommands

