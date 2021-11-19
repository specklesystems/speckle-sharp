#include "GetElementTypes.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"


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
	return GS::NoValue; 
}


GS::Optional<GS::UniString>	GetElementTypes::GetInputParametersSchema () const
{
	return GS::NoValue; 
}


GS::Optional<GS::UniString> GetElementTypes::GetResponseSchema () const
{
	return GS::NoValue; 
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

	const auto& listAdder = retVal.AddList<GS::Int32> (ElementTypesFieldName);
	for (const API_Guid& guid : elementGuids) {
		listAdder ( GS::Int32 (Utility::GetElementType (guid)));
	}

	return retVal;
}


void GetElementTypes::OnResponseValidationFailed (const GS::ObjectState& /*response*/) const
{
}


}