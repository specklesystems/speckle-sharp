#include "BaseCommand.hpp"


namespace AddOnCommands {


GS::Optional<GS::UniString> BaseCommand::GetSchemaDefinitions () const
{
	return GS::NoValue;
}


GS::Optional<GS::UniString>	BaseCommand::GetInputParametersSchema () const
{
	return GS::NoValue;
}


GS::Optional<GS::UniString> BaseCommand::GetResponseSchema () const
{
	return GS::NoValue;
}


API_AddOnCommandExecutionPolicy BaseCommand::GetExecutionPolicy () const
{
	return API_AddOnCommandExecutionPolicy::ScheduleForExecutionOnMainThread; 
}


void BaseCommand::OnResponseValidationFailed (const GS::ObjectState& /*response*/) const
{
}


}