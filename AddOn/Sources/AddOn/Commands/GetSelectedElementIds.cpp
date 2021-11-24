#include "GetSelectedElementIds.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "SchemaDefinitionBuilder.hpp"


namespace AddOnCommands {

static const char* SelectedElementIdsField = "selectedElementIds";


GS::String GetSelectedElementIds::GetNamespace () const
{
	return CommandNamespace;
}


GS::String GetSelectedElementIds::GetName () const
{
	return GetSelectedElementIdsCommandName;
}
	
		
GS::Optional<GS::UniString> GetSelectedElementIds::GetSchemaDefinitions () const
{
	Json::SchemaDefinitionBuilder builder { GS::Array<GS::UniString> { Json::SchemaDefinitionProvider::ElementIdsSchema () } };
	return builder.Build ();
}


GS::Optional<GS::UniString>	GetSelectedElementIds::GetInputParametersSchema () const
{
	return GS::NoValue; 
}


GS::Optional<GS::UniString> GetSelectedElementIds::GetResponseSchema () const
{
	return R"(
		{
			"type": "object",
			"properties" : {
				"selectedElementIds": { "$ref": "#/definitions/ElementIds" }
			},
			"additionalProperties" : false,
			"required" : [ "selectedElementIds" ]
		}
	)";
}


API_AddOnCommandExecutionPolicy GetSelectedElementIds::GetExecutionPolicy () const 
{
	return API_AddOnCommandExecutionPolicy::ScheduleForExecutionOnMainThread; 
}


GS::Array<API_Guid> GetSelectedElementGuids ()
{
	GSErrCode				err;
	GS::Array<API_Guid>		elementGuids;
	API_SelectionInfo		selectionInfo;
	GS::Array<API_Neig>		selNeigs;

	err = ACAPI_Selection_Get (&selectionInfo, &selNeigs, true);
	if (err == NoError) {
		if (selectionInfo.typeID != API_SelEmpty) {
			for (const API_Neig& neig : selNeigs) {
				const API_Guid& elemGuid = neig.guid;
				if (Utility::IsElement3D (elemGuid)) {
					elementGuids.Push (elemGuid);
				}
			}
		}
	}

	BMKillHandle ((GSHandle *) &selectionInfo.marquee.coords);

	return elementGuids;
}


GS::ObjectState GetSelectedElementIds::Execute (const GS::ObjectState& /*parameters*/, GS::ProcessControl& /*processControl*/) const
{

	GS::Array<API_Guid>	elementGuids = GetSelectedElementGuids ();

	GS::ObjectState retVal;

	const auto& listAdder = retVal.AddList<GS::UniString> (SelectedElementIdsField);
	for (const API_Guid& guid : elementGuids) {
		listAdder (APIGuidToString (guid));
	}

	return retVal;
}


void GetSelectedElementIds::OnResponseValidationFailed (const GS::ObjectState& /*response*/) const
{
}


}