#include "GetElementTypes.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "SchemaDefinitions\SchemaDefinitionBuilder.hpp"


namespace AddOnCommands {

static const char* ElementIdsFieldName = "elementIds";
static const char* ElementTypesFieldName = "elementTypes";

GS::String GetElementTypes::GetNamespace () const
{
	return CommandNamespace;
}


GS::String GetElementTypes::GetName () const
{
	return GetElementTypesCommandName;
}
	
		
GS::Optional<GS::UniString> GetElementTypes::GetSchemaDefinitions () const
{
	Json::SchemaDefinitionBuilder builder;
	builder.Add (Json::SchemaDefinitionProvider::ElementIdsSchema());
	builder.Add (Json::SchemaDefinitionProvider::ElementTypeSchema());
	return builder.Build();
}


GS::Optional<GS::UniString>	GetElementTypes::GetInputParametersSchema () const
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


GS::Optional<GS::UniString> GetElementTypes::GetResponseSchema () const
{
	return R"(
		{
			"type": "object",
			"properties" : {
				"elementTypes": {
					"type": "array",
					"description": "Container for element types.",
	  				"items": { "$ref": "#/definitions/ElementType" }
				}
			},
			"additionalProperties" : false,
			"required" : [ "elementTypes" ]
		}
	)";
}


API_AddOnCommandExecutionPolicy GetElementTypes::GetExecutionPolicy () const
{
	return API_AddOnCommandExecutionPolicy::ScheduleForExecutionOnMainThread; 
}


GS::ObjectState GetElementTypes::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const
{
	GS::Array<GS::UniString> ids;
	parameters.Get (ElementIdsFieldName, ids);
	
	GS::Array<API_Guid>	elementGuids = ids.Transform<API_Guid> ([] (const GS::UniString& idStr) { return APIGuidFromString (idStr.ToCStr ()); });

	GS::ObjectState retVal;

	const auto& listAdder = retVal.AddList<GS::UniString> (ElementTypesFieldName);
	for (const API_Guid& guid : elementGuids) {
		API_ElemTypeID elementTypeId = Utility::GetElementType(guid);
		listAdder(Utility::elementNames.Get(elementTypeId));
	}

	return retVal;
}


void GetElementTypes::OnResponseValidationFailed (const GS::ObjectState& /*response*/) const
{
}


}