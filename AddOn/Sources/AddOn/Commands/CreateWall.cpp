#include "CreateWall.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "SchemaDefinitionBuilder.hpp"
#include "Utility.hpp"
#include "Objects/Point.hpp"
#include "RealNumber.h"
#include "DGModule.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"

namespace AddOnCommands {


GS::String CreateWall::GetNamespace () const
{
	return CommandNamespace;
}


GS::String CreateWall::GetName () const
{
	return CreateWallCommandName;
}
	
		
GS::Optional<GS::UniString> CreateWall::GetSchemaDefinitions () const
{
	Json::SchemaDefinitionBuilder builder;
	builder.Add (Json::SchemaDefinitionProvider::ElementIdsSchema ());
	builder.Add (Json::SchemaDefinitionProvider::WallDataSchema ());
	return builder.Build();
}


GS::Optional<GS::UniString>	CreateWall::GetInputParametersSchema () const
{
	return GS::NoValue;
	/*	//TMP for DEV
	return R"(
		{
			"type": "object",
			"properties" : {
				"walls": {
					"type": "array",
					"items": { "$ref": "#/definitions/WallData" }
				}
			},
			"additionalProperties" : false,
			"required" : [ "walls" ]
		}
	)";*/
}


GS::Optional<GS::UniString> CreateWall::GetResponseSchema () const
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


API_AddOnCommandExecutionPolicy CreateWall::GetExecutionPolicy () const
{
	return API_AddOnCommandExecutionPolicy::ScheduleForExecutionOnMainThread; 
}


GS::ObjectState CreateWall::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const
{
	GS::Array<GS::ObjectState> walls;
	parameters.Get(WallsFieldName, walls);

	GS::ObjectState result;
	
	
	const auto& listAdder = result.AddList<GS::UniString> (ElementIdsFieldName);

	//GSErrCode err = NoError;
	//API_Element wallElement;
	//wallElement.header.typeID = API_WallID;

	//err = ACAPI_Element_GetDefaults(&wallElement, nullptr);
	//if (err != NoError) return result;

	for (const GS::ObjectState& wall : walls) {
		GS::UniString elemId;
		wall.Get (ElementIdFieldName, elemId);
		listAdder (elemId);
	}

	/*
	GS::Array<GS::UniString> ids;
	parameters.Get (ElementIdsFieldName, ids);
	GS::Array<API_Guid>	elementGuids = ids.Transform<API_Guid> ([] (const GS::UniString& idStr) { return APIGuidFromString (idStr.ToCStr ()); });

	GS::ObjectState result;
	
	API_Element element;
	GSErrCode err;
	const auto& listAdder = result.AddList<GS::ObjectState> (WallsFieldName);
	for (const API_Guid& guid : elementGuids) {

		BNZeroMemory (&element, sizeof (API_Element));
		element.header.guid = guid;
		err = ACAPI_Element_Get (&element);
		if (err != NoError) continue;

		if (element.header.typeID != API_WallID) continue;	//a security check

		listAdder (SerializeWallType (element.wall));
	}

	return result;
	*/
	DG::InformationAlert (GS::UniString ("I'm not doing anything useful right now,"), GS::UniString ("but that's okay."), "OK");

	return result;
}


void CreateWall::OnResponseValidationFailed (const GS::ObjectState& /*response*/) const
{
}


}