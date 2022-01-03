#include "GetSlabData.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Polyline.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"


namespace AddOnCommands {


GS::ObjectState SerializeSlabType (const API_SlabType& slab, const API_ElementMemo& memo)
{
	GS::ObjectState os;

	os.Add (ElementIdFieldName, APIGuidToString (slab.head.guid));

	os.Add (FloorIndexFieldName, slab.head.floorInd);

	double level = Utility::GetStoryLevel (slab.head.floorInd) + slab.level;
	os.Add (Slab::ShapeFieldName, Objects::ElementShape (slab.poly, memo, level));

	os.Add (Slab::StructureFieldName, structureTypeNames.Get (slab.modelElemStructureType));

	os.Add (Slab::ThicknessFieldName, slab.thickness);

	if ((BMGetHandleSize ((GSHandle) memo.edgeTrims) / sizeof (API_EdgeTrim) >= 1) &&
		(*(memo.edgeTrims))[1].sideType == APIEdgeTrim_CustomAngle) {
		double angle = (*(memo.edgeTrims))[1].sideAngle;
		os.Add (Slab::EdgeAngleTypeFieldName, edgeAngleTypeNames.Get (APIEdgeTrim_CustomAngle));
		os.Add (Slab::EdgeAngleFieldName, angle);
	} else {
		os.Add (Slab::EdgeAngleTypeFieldName, edgeAngleTypeNames.Get (APIEdgeTrim_Perpendicular));
	}

	os.Add (Slab::ReferencePlaneLocationFieldName, referencePlaneLocationNames.Get (slab.referencePlaneLocation));

	return os;
}


GS::String GetSlabData::GetName () const
{
	return GetSlabDataCommandName;
}


GS::ObjectState GetSlabData::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const
{
	GS::Array<GS::UniString> ids;
	parameters.Get (ElementIdsFieldName, ids);
	GS::Array<API_Guid>	elementGuids = ids.Transform<API_Guid> ([] (const GS::UniString& idStr) { return APIGuidFromString (idStr.ToCStr ()); });

	GS::ObjectState result;

	const auto& listAdder = result.AddList<GS::ObjectState> (SlabsFieldName);
	for (const API_Guid& guid : elementGuids) {

		API_Element element {};
		API_ElementMemo elementMemo {};

		element.header.guid = guid;
		GSErrCode err = ACAPI_Element_Get (&element);
		if (err != NoError) continue;

		if (element.header.typeID != API_SlabID) continue;

		err = ACAPI_Element_GetMemo (guid, &elementMemo);
		if (err != NoError) continue;

		listAdder (SerializeSlabType (element.slab, elementMemo));
	}

	return result;
}


}