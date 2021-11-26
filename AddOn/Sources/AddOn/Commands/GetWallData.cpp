#include "GetWallData.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "SchemaDefinitionBuilder.hpp"
#include "Utility.hpp"
#include "Objects/Point.hpp"


namespace AddOnCommands {

static const char* ElementIdsFieldName = "elementIds";
static const char* WallsFieldName = "walls";
static const char* ElementIdFieldName = "elementId";
static const char* BeginFieldName = "begin";
static const char* EndFieldName = "end";
static const char* HeightFieldName = "height";
static const char* ThicknessFieldName = "thickness";

GS::ObjectState WallTypeToObject (const API_WallType& wall)
{
	GS::ObjectState os;

	os.Add (ElementIdFieldName, APIGuidToString(wall.head.guid));
	os.Add (BeginFieldName, Objects::Point2D(wall.begC.x, wall.begC.y));
	os.Add (EndFieldName, Objects::Point2D(wall.endC.x, wall.endC.y));
	os.Add (HeightFieldName, wall.height);
	os.Add (ThicknessFieldName, wall.thickness);

	return os;
}

GS::String GetWallData::GetNamespace () const
{
	return CommandNamespace;
}


GS::String GetWallData::GetName () const
{
	return GetWallDataCommandName;
}
	
		
GS::Optional<GS::UniString> GetWallData::GetSchemaDefinitions () const
{
	Json::SchemaDefinitionBuilder builder;
	builder.Add (Json::SchemaDefinitionProvider::ElementIdsSchema ());
	builder.Add (Json::SchemaDefinitionProvider::WallDataSchema ());
	return builder.Build();
}


GS::Optional<GS::UniString>	GetWallData::GetInputParametersSchema () const
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


GS::Optional<GS::UniString> GetWallData::GetResponseSchema () const
{
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
	)";
}


API_AddOnCommandExecutionPolicy GetWallData::GetExecutionPolicy () const
{
	return API_AddOnCommandExecutionPolicy::ScheduleForExecutionOnMainThread; 
}


GS::ObjectState GetWallData::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const
{
	GS::Array<GS::UniString> ids;
	parameters.Get (ElementIdsFieldName, ids);
	GS::Array<API_Guid>	elementGuids = ids.Transform<API_Guid> ([] (const GS::UniString& idStr) { return APIGuidFromString (idStr.ToCStr ()); });

	GS::ObjectState result;
	
	API_Element element;
	GSErrCode err;
	const auto& listAdder = result.AddList<GS::ObjectState> (WallsFieldName);
	for (const API_Guid& guid : elementGuids) {

		BNZeroMemory(&element, sizeof(API_Element));
		element.header.guid = guid;
		err = ACAPI_Element_Get(&element);
		if (err != NoError) continue;

		if (element.header.typeID != API_WallID) continue;	//a security check

		listAdder (WallTypeToObject (element.wall));
	}

	return result;
}


void GetWallData::OnResponseValidationFailed (const GS::ObjectState& /*response*/) const
{
}


}