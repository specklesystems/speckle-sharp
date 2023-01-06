#include "CreateColumn.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Point.hpp"
#include "RealNumber.h"
#include "DGModule.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"

namespace AddOnCommands
{
static GSErrCode CreateNewColumn (API_Element& column, API_ElementMemo* memo)
{
	return ACAPI_Element_Create (&column, memo);
}


static GSErrCode ModifyExistingColumn (API_Element& column, API_Element& mask, API_ElementMemo* memo)
{
	return ACAPI_Element_Change (&column, &mask, memo,
		APIMemoMask_ColumnSegment |
		APIMemoMask_AssemblySegmentScheme |
		APIMemoMask_AssemblySegmentCut,
		true);
}


static GSErrCode GetColumnFromObjectState (const GS::ObjectState& os, API_Element& element, API_Element& columnMask, API_ElementMemo* memo)
{
	GSErrCode err = NoError;

	GS::UniString guidString;
	os.Get (ApplicationIdFieldName, guidString);
	element.header.guid = APIGuidFromString (guidString.ToCStr ());
#ifdef ServerMainVers_2600
	element.header.type.typeID = API_ColumnID;
#else
	element.header.typeID = API_ColumnID;
#endif
	err = Utility::GetBaseElementData (element, memo);
	if (err != NoError)
		return err;

	Objects::Point3D origoPos;
	if (os.Contains (Column::origoPos))
		os.Get (Column::origoPos, origoPos);
	element.column.origoPos = origoPos.ToAPI_Coord ();
	ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnType, origoPos);


	if (os.Contains (FloorIndexFieldName)) {
		os.Get (FloorIndexFieldName, element.header.floorInd);
		Utility::SetStoryLevel (origoPos.Z, element.header.floorInd, element.column.bottomOffset);
	} else {
		Utility::SetStoryLevelAndFloor (origoPos.Z, element.header.floorInd, element.column.bottomOffset);
	}
	ACAPI_ELEMENT_MASK_SET (columnMask, API_Elem_Head, floorInd);

	if (os.Contains (Column::height))
		os.Get (Column::height, element.column.height);
	ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnType, height);

	if (os.Contains (Column::aboveViewLinePen))
		os.Get (Column::aboveViewLinePen, element.column.aboveViewLinePen);
	ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnType, aboveViewLinePen);

	if (os.Contains (Column::isAutoOnStoryVisibility))
		os.Get (Column::isAutoOnStoryVisibility, element.column.isAutoOnStoryVisibility);
	ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnType, isAutoOnStoryVisibility);

	if (os.Contains (Column::hiddenLinePen))
		os.Get (Column::hiddenLinePen, element.column.hiddenLinePen);
	ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnType, hiddenLinePen);

	if (os.Contains (Column::belowViewLinePen))
		os.Get (Column::belowViewLinePen, element.column.belowViewLinePen);
	ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnType, belowViewLinePen);

	if (os.Contains (Column::isFlipped))
		os.Get (Column::isFlipped, element.column.isFlipped);
	ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnType, isFlipped);

	if (os.Contains (Column::isSlanted))
		os.Get (Column::isSlanted, element.column.isSlanted);
	ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnType, isSlanted);

	if (os.Contains (Column::slantAngle))
		os.Get (Column::slantAngle, element.column.slantAngle);
	ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnType, slantAngle);

	if (os.Contains (Column::nSegments))
		os.Get (Column::nSegments, element.column.nSegments);
	ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnType, nSegments);

	if (os.Contains (Column::nCuts))
		os.Get (Column::nCuts, element.column.nCuts);
	ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnType, nCuts);

	if (os.Contains (Column::nSchemes))
		os.Get (Column::nSchemes, element.column.nSchemes);
	ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnType, nSchemes);

	if (os.Contains (Column::nProfiles))
		os.Get (Column::nProfiles, element.column.nProfiles);
	ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnType, nProfiles);

	if (os.Contains (Column::useCoverFill))
		os.Get (Column::useCoverFill, element.column.useCoverFill);
	ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnType, useCoverFill);

	if (os.Contains (Column::useCoverFillFromSurface))
		os.Get (Column::useCoverFillFromSurface, element.column.useCoverFillFromSurface);
	ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnType, useCoverFillFromSurface);

	if (os.Contains (Column::coverFillOrientationComesFrom3D))
		os.Get (Column::coverFillOrientationComesFrom3D, element.column.coverFillOrientationComesFrom3D);
	ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnType, coverFillOrientationComesFrom3D);

	if (os.Contains (Column::coverFillForegroundPen))
		os.Get (Column::coverFillForegroundPen, element.column.coverFillForegroundPen);
	ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnType, coverFillForegroundPen);

	if (os.Contains (Column::corePen))
		os.Get (Column::corePen, element.column.corePen);
	ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnType, corePen);

	if (os.Contains (Column::coreAnchor))
		os.Get (Column::coreAnchor, element.column.coreAnchor);
	ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnType, coreAnchor);

	if (os.Contains (Column::bottomOffset))
		os.Get (Column::bottomOffset, element.column.bottomOffset);
	ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnType, bottomOffset);

	if (os.Contains (Column::topOffset))
		os.Get (Column::topOffset, element.column.topOffset);
	ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnType, topOffset);

	if (os.Contains (Column::coreSymbolPar1))
		os.Get (Column::coreSymbolPar1, element.column.coreSymbolPar1);
	ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnType, coreSymbolPar1);

	if (os.Contains (Column::coreSymbolPar2))
		os.Get (Column::coreSymbolPar2, element.column.coreSymbolPar2);
	ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnType, coreSymbolPar2);

	if (os.Contains (Column::slantDirectionAngle))
		os.Get (Column::slantDirectionAngle, element.column.slantDirectionAngle);
	ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnType, slantDirectionAngle);

	if (os.Contains (Column::axisRotationAngle))
		os.Get (Column::axisRotationAngle, element.column.axisRotationAngle);
	ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnType, axisRotationAngle);

	if (os.Contains (Column::relativeTopStory))
		os.Get (Column::relativeTopStory, element.column.relativeTopStory);
	ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnType, relativeTopStory);

	API_ColumnSegmentType defaultColumnSegment;
	if (memo->columnSegments != nullptr) {
		defaultColumnSegment = memo->columnSegments[0];
		memo->columnSegments = (API_ColumnSegmentType*) BMAllocatePtr ((element.column.nSegments) * sizeof (API_ColumnSegmentType), ALLOCATE_CLEAR, 0);
	} else {
		return Error;
	}

	API_AssemblySegmentSchemeData defaultColumnSegmentScheme;
	if (memo->assemblySegmentSchemes != nullptr) {
		defaultColumnSegmentScheme = memo->assemblySegmentSchemes[0];
		memo->assemblySegmentSchemes = (API_AssemblySegmentSchemeData*) BMAllocatePtr ((element.column.nSchemes) * sizeof (API_AssemblySegmentSchemeData), ALLOCATE_CLEAR, 0);
	} else {
		return Error;
	}

	API_AssemblySegmentCutData defaultColumnSegmentCut;
	if (memo->assemblySegmentCuts != nullptr) {
		defaultColumnSegmentCut = memo->assemblySegmentCuts[0];
		memo->assemblySegmentCuts = (API_AssemblySegmentCutData*) BMAllocatePtr ((element.column.nCuts) * sizeof (API_AssemblySegmentCutData), ALLOCATE_CLEAR, 0);
	} else {
		return Error;
	}

#pragma region Segment
	GS::ObjectState allSegments;
	if (os.Contains (Column::segmentData))
		os.Get (Column::segmentData, allSegments);

	for (UInt32 idx = 0; idx < element.column.nSegments; ++idx) {
		GS::ObjectState currentSegment;
		allSegments.Get (GS::String::SPrintf (Column::ColumnSegmentName, idx + 1), currentSegment);

		memo->columnSegments[idx] = defaultColumnSegment;
		if (!currentSegment.IsEmpty ()) {

			if (currentSegment.Contains (Column::circleBased))
				currentSegment.Get (Column::circleBased, memo->columnSegments[idx].assemblySegmentData.circleBased);
			ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnSegmentType, assemblySegmentData.circleBased);

			if (currentSegment.Contains (Column::nominalHeight))
				currentSegment.Get (Column::nominalHeight, memo->columnSegments[idx].assemblySegmentData.nominalHeight);
			ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnSegmentType, assemblySegmentData.nominalHeight);

			if (currentSegment.Contains (Column::nominalWidth))
				currentSegment.Get (Column::nominalWidth, memo->columnSegments[idx].assemblySegmentData.nominalWidth);
			ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnSegmentType, assemblySegmentData.nominalWidth);

			if (currentSegment.Contains (Column::isWidthAndHeightLinked))
				currentSegment.Get (Column::isWidthAndHeightLinked, memo->columnSegments[idx].assemblySegmentData.isWidthAndHeightLinked);
			ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnSegmentType, assemblySegmentData.isWidthAndHeightLinked);

			if (currentSegment.Contains (Column::isHomogeneous))
				currentSegment.Get (Column::isHomogeneous, memo->columnSegments[idx].assemblySegmentData.isHomogeneous);
			ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnSegmentType, assemblySegmentData.isHomogeneous);

			if (currentSegment.Contains (Column::endWidth))
				currentSegment.Get (Column::endWidth, memo->columnSegments[idx].assemblySegmentData.endWidth);
			ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnSegmentType, assemblySegmentData.endWidth);

			if (currentSegment.Contains (Column::endHeight))
				currentSegment.Get (Column::endHeight, memo->columnSegments[idx].assemblySegmentData.endHeight);
			ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnSegmentType, assemblySegmentData.endHeight);

			if (currentSegment.Contains (Column::isEndWidthAndHeightLinked))
				currentSegment.Get (Column::isEndWidthAndHeightLinked, memo->columnSegments[idx].assemblySegmentData.isEndWidthAndHeightLinked);
			ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnSegmentType, assemblySegmentData.isEndWidthAndHeightLinked);

			if (currentSegment.Contains (Column::modelElemStructureType)) {
				API_ModelElemStructureType realStructureType = API_BasicStructure;
				GS::UniString structureName;
				currentSegment.Get (Column::modelElemStructureType, structureName);

				GS::Optional<API_ModelElemStructureType> tmpStructureType = structureTypeNames.FindValue (structureName);
				if (tmpStructureType.HasValue ())
					realStructureType = tmpStructureType.Get ();
				memo->columnSegments[idx].assemblySegmentData.modelElemStructureType = realStructureType;
			}
			ACAPI_ELEMENT_MASK_SET (columnMask, API_ColumnSegmentType, assemblySegmentData.modelElemStructureType);

			if (currentSegment.Contains (Column::profileAttrName)) {
				GS::UniString attrName;
				currentSegment.Get (Column::profileAttrName, attrName);

				if (!attrName.IsEmpty ()) {
					API_Attribute attrib;
					BNZeroMemory (&attrib, sizeof (API_Attribute));
					attrib.header.typeID = API_ProfileID;
					CHCopyC (attrName.ToCStr (), attrib.header.name);
					err = ACAPI_Attribute_Get (&attrib);

					if (err == NoError)
						memo->columnSegments[idx].assemblySegmentData.profileAttr = attrib.header.index;
				}
			}

			if (currentSegment.Contains (Column::buildingMaterial)) {
				GS::UniString attrName;
				currentSegment.Get (Column::buildingMaterial, attrName);

				if (!attrName.IsEmpty ()) {
					API_Attribute attrib;
					BNZeroMemory (&attrib, sizeof (API_Attribute));
					attrib.header.typeID = API_BuildingMaterialID;
					CHCopyC (attrName.ToCStr (), attrib.header.name);
					err = ACAPI_Attribute_Get (&attrib);

					if (err == NoError)
						memo->columnSegments[idx].assemblySegmentData.buildingMaterial = attrib.header.index;
				}
			}
		}
	}
#pragma endregion

#pragma region Scheme
	GS::ObjectState allSchemes;
	if (os.Contains (Column::schemeData))
		os.Get (Column::schemeData, allSchemes);

	for (UInt32 idx = 0; idx < element.column.nSchemes; ++idx) {
		if (!allSchemes.IsEmpty ()) {
			GS::ObjectState currentScheme;
			allSchemes.Get (GS::String::SPrintf (Column::SchemeName, idx + 1), currentScheme);

			memo->assemblySegmentSchemes[idx] = defaultColumnSegmentScheme;
			if (!currentScheme.IsEmpty ()) {

				if (currentScheme.Contains (Column::lengthType)) {
					API_AssemblySegmentLengthTypeID lengthType = APIAssemblySegment_Fixed;
					GS::UniString lengthTypeName;
					currentScheme.Get (Column::lengthType, lengthTypeName);

					GS::Optional<API_AssemblySegmentLengthTypeID> type = segmentLengthTypeNames.FindValue (lengthTypeName);
					if (type.HasValue ())
						lengthType = type.Get ();
					memo->assemblySegmentSchemes[idx].lengthType = lengthType;

					if (lengthType == APIAssemblySegment_Fixed && currentScheme.Contains (Column::fixedLength)) {
						currentScheme.Get (Column::fixedLength, memo->assemblySegmentSchemes[idx].fixedLength);
						memo->assemblySegmentSchemes[idx].lengthProportion = 0.0;
					} else if (lengthType == APIAssemblySegment_Proportional && currentScheme.Contains (Column::lengthProportion)) {
						currentScheme.Get (Column::lengthProportion, memo->assemblySegmentSchemes[idx].lengthProportion);
						memo->assemblySegmentSchemes[idx].fixedLength = 0.0;
					}
				}
			}
		}
	}
#pragma endregion

#pragma region Cut
	GS::ObjectState allCuts;
	if (os.Contains (Column::cutData))
		os.Get (Column::cutData, allCuts);

	for (UInt32 idx = 0; idx < element.column.nCuts; ++idx) {
		GS::ObjectState currentCut;
		allCuts.Get (GS::String::SPrintf (Column::CutName, idx + 1), currentCut);

		memo->assemblySegmentCuts[idx] = defaultColumnSegmentCut;
		if (!currentCut.IsEmpty ()) {

			if (currentCut.Contains (Column::cutType)) {
				API_AssemblySegmentCutTypeID realCutType = APIAssemblySegmentCut_Vertical;
				GS::UniString structureName;
				currentCut.Get (Column::cutType, structureName);

				GS::Optional<API_AssemblySegmentCutTypeID> tmpCutType = assemblySegmentCutTypeNames.FindValue (structureName);
				if (tmpCutType.HasValue ())
					realCutType = tmpCutType.Get ();
				memo->assemblySegmentCuts[idx].cutType = realCutType;
			}
			if (currentCut.Contains (Column::customAngle)) {
				currentCut.Get (Column::customAngle, memo->assemblySegmentCuts[idx].customAngle);
			}
		}
	}
#pragma endregion

	return NoError;
}
#pragma endregion

GS::String CreateColumn::GetName () const
{
	return CreateColumnCommandName;
}

GS::ObjectState CreateColumn::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const
{
	GS::ObjectState result;

	GS::Array<GS::ObjectState> columns;
	parameters.Get (ColumnsFieldName, columns);

	const auto& listAdder = result.AddList<GS::UniString> (ApplicationIdsFieldName);

	ACAPI_CallUndoableCommand ("CreateSpeckleColumn", [&] () -> GSErrCode {
		for (const GS::ObjectState& columnOs : columns) {
			API_Element column{};
			API_Element columnMask{};
			API_ElementMemo memo{}; // Neccessary for column

			GSErrCode err = GetColumnFromObjectState (columnOs, column, columnMask, &memo);
			if (err != NoError)
				continue;

			bool columnExists = Utility::ElementExists (column.header.guid);
			if (columnExists) {
				err = ModifyExistingColumn (column, columnMask, &memo);
			} else {
				err = CreateNewColumn (column, &memo);
			}

			if (err == NoError) {
				GS::UniString elemId = APIGuidToString (column.header.guid);
				listAdder (elemId);
			}

			ACAPI_DisposeElemMemoHdls (&memo);
		}
		return NoError;
		});

	return result;
}
}

