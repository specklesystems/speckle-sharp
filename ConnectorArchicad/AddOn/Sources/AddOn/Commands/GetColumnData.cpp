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
			API_ColumnSegmentType columnSegment = memo.columnSegments[idx];

			currentSegment.Add (Column::circleBased, columnSegment.assemblySegmentData.circleBased);
			currentSegment.Add (Column::modelElemStructureType, structureTypeNames.Get (columnSegment.assemblySegmentData.modelElemStructureType));
			currentSegment.Add (Column::nominalHeight, columnSegment.assemblySegmentData.nominalHeight);
			currentSegment.Add (Column::nominalWidth, columnSegment.assemblySegmentData.nominalWidth);
			currentSegment.Add (Column::isWidthAndHeightLinked, columnSegment.assemblySegmentData.isWidthAndHeightLinked);
			currentSegment.Add (Column::isHomogeneous, columnSegment.assemblySegmentData.isHomogeneous);
			currentSegment.Add (Column::endWidth, columnSegment.assemblySegmentData.endWidth);
			currentSegment.Add (Column::endHeight, columnSegment.assemblySegmentData.endHeight);
			currentSegment.Add (Column::isEndWidthAndHeightLinked, columnSegment.assemblySegmentData.isEndWidthAndHeightLinked);

			API_Attribute attrib;
			switch (columnSegment.assemblySegmentData.modelElemStructureType) {
			case API_CompositeStructure:
				DBASSERT (columnSegment.assemblySegmentData.modelElemStructureType != API_CompositeStructure)
					break;
			case API_BasicStructure:
				BNZeroMemory (&attrib, sizeof (API_Attribute));
				attrib.header.typeID = API_BuildingMaterialID;
				attrib.header.index = columnSegment.assemblySegmentData.buildingMaterial;
				ACAPI_Attribute_Get (&attrib);

				currentSegment.Add (Column::buildingMaterial, GS::UniString{attrib.header.name});
				break;
			case API_ProfileStructure:
				BNZeroMemory (&attrib, sizeof (API_Attribute));
				attrib.header.typeID = API_ProfileID;
				attrib.header.index = columnSegment.assemblySegmentData.profileAttr;
				ACAPI_Attribute_Get (&attrib);

				currentSegment.Add (Column::profileAttrName, GS::UniString{attrib.header.name});
				break;
			default:
				break;
			}
			allSegments.Add (GS::String::SPrintf (Column::ColumnSegmentName, idx + 1), currentSegment);
		}

		os.Add (Column::segmentData, allSegments);
	}

	// Scheme
	if (memo.assemblySegmentSchemes != nullptr) {
		GS::ObjectState allSchemes;

		GSSize schemesCount = BMGetPtrSize (reinterpret_cast<GSPtr>(memo.assemblySegmentSchemes)) / sizeof (API_AssemblySegmentSchemeData);
		DBASSERT (schemesCount == elem.column.nSchemes)

			for (GSSize idx = 0; idx < schemesCount; ++idx) {
				GS::ObjectState currentScheme;
				API_AssemblySegmentSchemeData columnAssemblySegmentScheme = memo.assemblySegmentSchemes[idx];

				currentScheme.Add (Column::lengthType, segmentLengthTypeNames.Get (columnAssemblySegmentScheme.lengthType));
				currentScheme.Add (Column::fixedLength, columnAssemblySegmentScheme.fixedLength);
				currentScheme.Add (Column::lengthProportion, columnAssemblySegmentScheme.lengthProportion);

				allSchemes.Add (GS::String::SPrintf (Column::SchemeName, idx + 1), currentScheme);
			}

		os.Add (Column::schemeData, allSchemes);
	}

	// Cut
	if (memo.assemblySegmentCuts != nullptr) {
		GS::ObjectState allCuts;

		GSSize cutsCount = BMGetPtrSize (reinterpret_cast<GSPtr>(memo.assemblySegmentCuts)) / sizeof (API_AssemblySegmentCutData);
		DBASSERT (cutsCount == elem.column.nCuts)

			for (GSSize idx = 0; idx < cutsCount; ++idx) {
				GS::ObjectState currentCut;
				API_AssemblySegmentCutData assemblySegmentCuts = memo.assemblySegmentCuts[idx];

				currentCut.Add (Column::cutType, assemblySegmentCutTypeNames.Get (assemblySegmentCuts.cutType));
				currentCut.Add (Column::customAngle, assemblySegmentCuts.customAngle);

				allCuts.Add (GS::String::SPrintf (Column::CutName, idx + 1), currentCut);
			}

		os.Add (Column::cutData, allCuts);
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
