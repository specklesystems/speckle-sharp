#include "GetElementTypes.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"
using namespace FieldNames;


namespace AddOnCommands {


GS::String GetElementTypes::GetName () const
{
	return GetElementTypesCommandName;
}


GS::ObjectState GetElementTypes::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const
{
	GS::Array<GS::UniString> ids;
	parameters.Get (ElementBase::ApplicationIds, ids);

	GS::ObjectState result;

	const auto& listAdder = result.AddList<GS::ObjectState> (ElementBase::ElementTypes);
	for (const GS::UniString& id : ids) {
		API_Guid guid = APIGuidFromString (id.ToCStr ());
		API_ElemType elementType = Utility::GetElementType (guid);
		GS::UniString elemType = elementNames.Get (elementType);
		GS::ObjectState listElem{ElementBase::ApplicationId, id, ElementBase::ElementType, elemType};
		listAdder (listElem);
	}

	return result;
}


}
