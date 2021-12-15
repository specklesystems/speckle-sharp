#include "CreateDirectShape.hpp"
#include "ResourceIds.hpp"


namespace AddOnCommands {


GS::String CreateDirectShape::GetNamespace () const
{
	return CommandNamespace;
}


GS::String CreateDirectShape::GetName () const
{
	return CreateDirectShapesCommandName;
}
	
		
GS::Optional<GS::UniString> CreateDirectShape::GetSchemaDefinitions () const
{
	return GS::NoValue;
}


GS::Optional<GS::UniString>	CreateDirectShape::GetInputParametersSchema () const
{
	return GS::NoValue;
}


GS::Optional<GS::UniString> CreateDirectShape::GetResponseSchema () const
{
	return GS::NoValue;
}


API_AddOnCommandExecutionPolicy CreateDirectShape::GetExecutionPolicy () const
{
	return API_AddOnCommandExecutionPolicy::ScheduleForExecutionOnMainThread; 
}


GS::ObjectState CreateDirectShape::Execute (const GS::ObjectState& /*parameters*/, GS::ProcessControl& /*processControl*/) const
{
	
}


void CreateDirectShape::OnResponseValidationFailed (const GS::ObjectState& /*response*/) const
{
}


//static private GS::Optional<API_Guid> CreateDirectShape (const GS::ObjectState& /*os*/)
//{
//	return GS::NoValue;
//}


}