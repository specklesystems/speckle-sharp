#include "BaseCommand.hpp"
#include "ResourceIds.hpp"


namespace AddOnCommands {


GS::String BaseCommand::GetNamespace () const
{
	return CommandNamespace;
}


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