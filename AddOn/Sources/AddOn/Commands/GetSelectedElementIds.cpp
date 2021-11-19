#include "GetSelectedElementIds.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"


namespace AddOnCommands {


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
	return GS::NoValue; 
}


GS::Optional<GS::UniString>	GetSelectedElementIds::GetInputParametersSchema () const
{
	return GS::NoValue; 
}


GS::Optional<GS::UniString> GetSelectedElementIds::GetResponseSchema () const
{
	return GS::NoValue; 
}


API_AddOnCommandExecutionPolicy GetSelectedElementIds::GetExecutionPolicy () const 
{
	return API_AddOnCommandExecutionPolicy::ScheduleForExecutionOnMainThread; 
}


GS::ObjectState GetSelectedElementIds::Execute (const GS::ObjectState& /*parameters*/, GS::ProcessControl& /*processControl*/) const
{
	return GS::ObjectState ();
}


void GetSelectedElementIds::OnResponseValidationFailed (const GS::ObjectState& /*response*/) const
{
}


}