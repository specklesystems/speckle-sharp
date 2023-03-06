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
	parameters.Get (ApplicationIds, ids);

	GS::ObjectState result;

	const auto& listAdder = result.AddList<GS::ObjectState> (ElementTypes);
	for (const GS::UniString& id : ids) {
		API_Guid guid = APIGuidFromString (id.ToCStr ());
		API_ElemTypeID elementTypeId = Utility::GetElementType (guid);
		GS::UniString elemType = elementNames.Get (elementTypeId);
		GS::ObjectState listElem{ApplicationId, id, ElementType, elemType};
		listAdder (listElem);
	}

	return result;
}


}