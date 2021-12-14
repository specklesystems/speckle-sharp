#include "GetSlabData.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "SchemaDefinitionBuilder.hpp"
#include "Utility.hpp"
#include "Objects/Polyline.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"


namespace AddOnCommands {


GS::ObjectState SerializeSlabType (const API_SlabType& slab, const API_ElementMemo& memo)
{
	GS::ObjectState os;

	// The ID of the slab
	os.Add (ElementIdFieldName, APIGuidToString (slab.head.guid));

	// The floor index of the slab
	os.Add (FloorIndexFieldName, slab.head.floorInd);

	// The slab's shape
	double level = Utility::GetStoryLevel (slab.head.floorInd) + slab.level;
	os.Add (Slab::ShapeFieldName, Objects::ElementShape (slab.poly, memo, level));

	// The structure type of the slab
	os.Add (Slab::StructureFieldName, structureTypeNames.Get (slab.modelElemStructureType));

	// The thickness of the slab
	os.Add (Slab::ThicknessFieldName, slab.thickness);

	//The edge type and edge angle of the slab
	if ((BMGetHandleSize ((GSHandle) memo.edgeTrims) / sizeof (API_EdgeTrim) >= 1) &&
		(*(memo.edgeTrims))[1].sideType == APIEdgeTrim_CustomAngle) {
		double angle = (*(memo.edgeTrims))[1].sideAngle;
		os.Add (Slab::EdgeAngleTypeFieldName, edgeAngleTypeNames.Get (APIEdgeTrim_CustomAngle));
		os.Add (Slab::EdgeAngleFieldName, angle);
	} else {
		os.Add (Slab::EdgeAngleTypeFieldName, edgeAngleTypeNames.Get (APIEdgeTrim_Perpendicular));
	}

	// The reference plane location of the slab
	os.Add (Slab::ReferencePlaneLocationFieldName, referencePlaneLocationNames.Get (slab.referencePlaneLocation));

	return os;
}

GS::String GetSlabData::GetNamespace () const
{
	return CommandNamespace;
}


GS::String GetSlabData::GetName () const
{
	return GetSlabDataCommandName;
}
	
		
GS::Optional<GS::UniString> GetSlabData::GetSchemaDefinitions () const
{
	Json::SchemaDefinitionBuilder builder;
	builder.Add (Json::SchemaDefinitionProvider::ElementIdsSchema ());
	builder.Add (Json::SchemaDefinitionProvider::SlabDataSchema ());
	return builder.Build();
}


GS::Optional<GS::UniString>	GetSlabData::GetInputParametersSchema () const
{
	return R"(
		{
			"type": "object",
			"properties" : {
				"elementIds": { "$ref": "#/definitions/ElementIds" }
			},
			"additionalProperties" : false,
			"required" : [ "elementIds" ]
		}
	)";
}


GS::Optional<GS::UniString> GetSlabData::GetResponseSchema () const
{
	return R"(
		{
			"type": "object",
			"properties" : {
				"slabs": {
					"type": "array",
					"items": { "$ref": "#/definitions/SlabData" }
				}
			},
			"additionalProperties" : false,
			"required" : [ "slabs" ]
		}
	)";
}


API_AddOnCommandExecutionPolicy GetSlabData::GetExecutionPolicy () const
{
	return API_AddOnCommandExecutionPolicy::ScheduleForExecutionOnMainThread; 
}


GS::ObjectState GetSlabData::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const
{
	GS::Array<GS::UniString> ids;
	parameters.Get (ElementIdsFieldName, ids);
	GS::Array<API_Guid>	elementGuids = ids.Transform<API_Guid> ([] (const GS::UniString& idStr) { return APIGuidFromString (idStr.ToCStr ()); });

	GS::ObjectState result;
	
	API_Element element;
	API_ElementMemo elementMemo;
	GSErrCode err;

	const auto& listAdder = result.AddList<GS::ObjectState> (SlabsFieldName);
	for (const API_Guid& guid : elementGuids) {

		BNZeroMemory (&element, sizeof (API_Element));
		BNZeroMemory (&elementMemo, sizeof (API_ElementMemo));

		element.header.guid = guid;
		err = ACAPI_Element_Get (&element);
		if (err != NoError) continue;

		if (element.header.typeID != API_SlabID) continue;

		err = ACAPI_Element_GetMemo (guid, &elementMemo);
		if (err != NoError) continue;

		listAdder (SerializeSlabType (element.slab, elementMemo));
	}

	return result;
}


void GetSlabData::OnResponseValidationFailed (const GS::ObjectState& /*response*/) const
{
}


}