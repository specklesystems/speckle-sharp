#include "GetElementTypes.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "SchemaDefinitionBuilder.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"

namespace AddOnCommands {


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
	  				"items": {
						"type": "object",
						"properties": {
							"elementId": { "$ref": "#/definitions/ElementId" },
							"elementType": { "$ref": "#/definitions/ElementType" }
						},
						"additionalProperties" : false,
						"required" : [ "elementId", "elementType" ]
					}
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
	
	GS::ObjectState result;

	const auto& listAdder = result.AddList<GS::ObjectState> (ElementTypesFieldName);
	for (const GS::UniString& id : ids) {
		API_Guid guid = APIGuidFromString (id.ToCStr ());
		API_ElemTypeID elementTypeId = Utility::GetElementType (guid);
		GS::UniString elemType = elementNames.Get (elementTypeId);
		GS::ObjectState listElem { ElementIdFieldName, id, ElementTypeFieldName, elemType };
		listAdder (listElem);
	}

	return result;
}


void GetElementTypes::OnResponseValidationFailed (const GS::ObjectState& /*response*/) const
{
}


}