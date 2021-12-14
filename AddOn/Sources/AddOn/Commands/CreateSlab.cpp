#include "CreateSlab.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "SchemaDefinitionBuilder.hpp"
#include "Utility.hpp"
#include "Objects/Polyline.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"


namespace AddOnCommands {


GS::String CreateSlab::GetNamespace () const
{
	return CommandNamespace;
}


GS::String CreateSlab::GetName () const
{
	return CreateSlabCommandName;
}
	
		
GS::Optional<GS::UniString> CreateSlab::GetSchemaDefinitions () const
{
	Json::SchemaDefinitionBuilder builder;
	builder.Add (Json::SchemaDefinitionProvider::SlabDataSchema ());
	builder.Add (Json::SchemaDefinitionProvider::ElementIdsSchema ());
	return builder.Build();
}


GS::Optional<GS::UniString>	CreateSlab::GetInputParametersSchema () const
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


GS::Optional<GS::UniString> CreateSlab::GetResponseSchema () const
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


API_AddOnCommandExecutionPolicy CreateSlab::GetExecutionPolicy () const
{
	return API_AddOnCommandExecutionPolicy::ScheduleForExecutionOnMainThread; 
}


GS::ObjectState CreateSlab::Execute (const GS::ObjectState& /*parameters*/, GS::ProcessControl& /*processControl*/) const
{
	//GS::Array<GS::UniString> ids;
	//parameters.Get (ElementIdsFieldName, ids);
	//GS::Array<API_Guid>	elementGuids = ids.Transform<API_Guid> ([] (const GS::UniString& idStr) { return APIGuidFromString (idStr.ToCStr ()); });

	GS::ObjectState result;
	
	//API_Element element;
	//API_ElementMemo elementMemo;
	//GSErrCode err;

	//const auto& listAdder = result.AddList<GS::ObjectState> (SlabsFieldName);
	//for (const API_Guid& guid : elementGuids) {

	//	BNZeroMemory (&element, sizeof (API_Element));
	//	BNZeroMemory (&elementMemo, sizeof (API_ElementMemo));

	//	element.header.guid = guid;
	//	err = ACAPI_Element_Get (&element);
	//	if (err != NoError) continue;

	//	if (element.header.typeID != API_SlabID) continue;

	//	err = ACAPI_Element_GetMemo (guid, &elementMemo);
	//	if (err != NoError) continue;

	//	listAdder (SerializeSlabType (element.slab, elementMemo));
	//}

	return result;
}


void CreateSlab::OnResponseValidationFailed (const GS::ObjectState& /*response*/) const
{
}


}