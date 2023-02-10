#include "GetColumnData.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Point.hpp"
#include "RealNumber.h"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"

namespace AddOnCommands
{

static GS::ObjectState SerializeColumnType (const API_Element& elem, const API_ElementMemo& memo)
{
	GS::ObjectState os;

	os.Add (ApplicationIdFieldName, APIGuidToString (elem.column.head.guid));
	os.Add (FloorIndexFieldName, elem.column.head.floorInd);

	double z = Utility::GetStoryLevel (elem.column.head.floorInd) + elem.column.bottomOffset;
	os.Add (Column::origoPos, Objects::Point3D (elem.column.origoPos.x, elem.column.origoPos.y, z));

	os.Add (Column::height, elem.column.height);
	os.Add (Column::aboveViewLinePen, elem.column.aboveViewLinePen);
	os.Add (Column::isAutoOnStoryVisibility, elem.column.isAutoOnStoryVisibility);
	os.Add (Column::hiddenLinePen, elem.column.hiddenLinePen);
	os.Add (Column::belowViewLinePen, elem.column.belowViewLinePen);
	os.Add (Column::isFlipped, elem.column.isFlipped);
	os.Add (Column::isSlanted, elem.column.isSlanted);
	os.Add (Column::slantAngle, elem.column.slantAngle);
	os.Add (Column::nSegments, elem.column.nSegments);
	os.Add (Column::nCuts, elem.column.nCuts);
	os.Add (Column::nSchemes, elem.column.nSchemes);
	os.Add (Column::nProfiles, elem.column.nProfiles);
	os.Add (Column::useCoverFill, elem.column.useCoverFill);
	os.Add (Column::useCoverFillFromSurface, elem.column.useCoverFillFromSurface);
	os.Add (Column::coverFillOrientationComesFrom3D, elem.column.coverFillOrientationComesFrom3D);
	os.Add (Column::coverFillForegroundPen, elem.column.coverFillForegroundPen);
	os.Add (Column::corePen, elem.column.corePen);
	os.Add (Column::coreAnchor, elem.column.coreAnchor);
	os.Add (Column::bottomOffset, elem.column.bottomOffset);
	os.Add (Column::topOffset, elem.column.topOffset);
	os.Add (Column::coreSymbolPar1, elem.column.coreSymbolPar1);
	os.Add (Column::coreSymbolPar2, elem.column.coreSymbolPar2);
	os.Add (Column::slantDirectionAngle, elem.column.slantDirectionAngle);
	os.Add (Column::axisRotationAngle, elem.column.axisRotationAngle);
	os.Add (Column::relativeTopStory, elem.column.relativeTopStory);

	// Segment
	if (memo.columnSegments != nullptr) {
		GS::ObjectState allSegments;

		GSSize segmentsCount = BMGetPtrSize (reinterpret_cast<GSPtr>(memo.columnSegments)) / sizeof (API_ColumnSegmentType);
		DBASSERT (segmentsCount == elem.column.nSegments);

		for (GSSize idx = 0; idx < segmentsCount; ++idx) {
			GS::ObjectState currentSegment;
			Utility::GetSegmentData (memo.columnSegments[idx].assemblySegmentData, currentSegment);
			allSegments.Add (GS::String::SPrintf (AssemblySegmentData::SegmentName, idx + 1), currentSegment);
		}

		os.Add (PartialObjects::SegmentData, allSegments);
	}

	// Scheme
	if (memo.assemblySegmentSchemes != nullptr) {
		GS::ObjectState allSchemes;
		Utility::GetAllSchemeData (memo.assemblySegmentSchemes, allSchemes);
		os.Add (PartialObjects::SchemeData, allSchemes);
	}

	// Cut
	if (memo.assemblySegmentCuts != nullptr) {
		GS::ObjectState allCuts;
		Utility::GetAllCutData (memo.assemblySegmentCuts, allCuts);
		os.Add (PartialObjects::CutData, allCuts);
	}

	return os;
}


GS::String GetColumnData::GetName () const
{
	return GetColumnDataCommandName;
}


GS::ObjectState GetColumnData::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const
{
	/*BASED ON THE GIVEN (SELECTED) OBJECTS, READ THEIR GUIDS AND EXECUTE SERIALIZATION ON THEM*/
	GS::ObjectState result;
	GS::Array<GS::UniString> ids;
	parameters.Get (ApplicationIdsFieldName, ids);
	GS::Array<API_Guid> elementGuids = ids.Transform<API_Guid> ([] (const GS::UniString& idStr) { return APIGuidFromString (idStr.ToCStr ()); });

	const auto& listAdder = result.AddList<GS::ObjectState> (ColumnsFieldName);
	for (const API_Guid& guid : elementGuids) {
		API_Element element{};
		element.header.guid = guid;

		GSErrCode err = ACAPI_Element_Get (&element);
		if (err != NoError)
			continue;

		API_ElementMemo memo{};
		ACAPI_Element_GetMemo (guid, &memo,
			APIMemoMask_ColumnSegment |
			APIMemoMask_AssemblySegmentScheme |
			APIMemoMask_AssemblySegmentCut);
		if (err != NoError)
			continue;

#ifdef ServerMainVers_2600
		if (element.header.type.typeID != API_ColumnID)
#else
		if (element.header.typeID != API_ColumnID)
#endif
		{
			continue;
		}

		listAdder (SerializeColumnType (element, memo));
	}

	return result;
}


} // namespace AddOnCommands
