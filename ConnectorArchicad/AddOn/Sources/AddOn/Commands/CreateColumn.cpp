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

#pragma region Segment
	GS::ObjectState allSegments;
	if (os.Contains (PartialObjects::SegmentData))
		os.Get (PartialObjects::SegmentData, allSegments);

	for (UInt32 idx = 0; idx < element.column.nSegments; ++idx) {
		GS::ObjectState currentSegment;
		allSegments.Get (GS::String::SPrintf (AssemblySegmentData::SegmentName, idx + 1), currentSegment);

		memo->columnSegments[idx] = defaultColumnSegment;
		Utility::CreateOneSegmentData (currentSegment, memo->columnSegments[idx].assemblySegmentData, columnMask);
	}
#pragma endregion

	Utility::CreateAllSchemeData (os, element.column.nSchemes, element, columnMask, memo);
	Utility::CreateAllCutData (os, element.column.nCuts, element, columnMask, memo);

	return err;
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

