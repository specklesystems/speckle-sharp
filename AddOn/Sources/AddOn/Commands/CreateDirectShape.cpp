#include "CreateDirectShape.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"


namespace AddOnCommands {


GS::String CreateDirectShape::GetNamespace () const
{
	return CommandNamespace;
}


GS::String CreateDirectShape::GetName () const
{
	return CreateDirectShapesCommandName;
}
	
		
GS::ObjectState CreateDirectShape::Execute (const GS::ObjectState& /*parameters*/, GS::ProcessControl& /*processControl*/) const
{
	GS::ObjectState result;
	return result;
}


//static private GS::Optional<API_Guid> CreateDirectShape (const GS::ObjectState& /*os*/)
//{
//	return GS::NoValue;
//}


}