#include "GetElementTypes.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"


namespace AddOnCommands {


GS::String GetElementTypes::GetName () const
{
	return GetElementTypesCommandName;
}


GS::ObjectState GetElementTypes::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const
{
	GS::Array<GS::UniString> ids;
	parameters.Get (ApplicationIdsFieldName, ids);

	GS::ObjectState result;

	const auto& listAdder = result.AddList<GS::ObjectState> (ElementTypesFieldName);
	for (const GS::UniString& id : ids) {
		API_Guid guid = APIGuidFromString (id.ToCStr ());
		API_ElemTypeID elementTypeId = Utility::GetElementType (guid);
		GS::UniString elemType = elementNames.Get (elementTypeId);
		GS::ObjectState listElem{ApplicationIdFieldName, id, ElementTypeFieldName, elemType};
		listAdder (listElem);
	}

	return result;
}


}