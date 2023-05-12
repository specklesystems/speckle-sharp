#include "GetDataCommand.hpp"
#include "ObjectState.hpp"
#include "FieldNames.hpp"
#include "Utility.hpp"


namespace AddOnCommands
{


GS::UInt64 GetDataCommand::GetMemoMask () const
{
	return APIMemoMask_All;
}


GS::ObjectState GetDataCommand::Execute (const GS::ObjectState& parameters,
	GS::ProcessControl& /*processControl*/) const
{
	GS::Array<GS::UniString> ids;
	parameters.Get (FieldNames::ElementBase::ApplicationIds, ids);
	GS::Array<API_Guid> elementGuids = ids.Transform<API_Guid> (
		[] (const GS::UniString& idStr) { return APIGuidFromString (idStr.ToCStr ()); }
	);

	GS::ObjectState result;
	const auto& listAdder = result.AddList<GS::ObjectState> (GetFieldName ());
	for (const API_Guid& guid : elementGuids) {
		API_Element element{};
		API_ElementMemo memo{};

		element.header.guid = guid;

		GSErrCode err = ACAPI_Element_Get (&element);
		if (err != NoError) {
			continue;
		}

		API_ElemTypeID elementType = Utility::GetElementType (element.header);
		if (elementType != GetElemTypeID ())
		{
			continue;
		}

		err = ACAPI_Element_GetMemo (guid, &memo, GetMemoMask ());
		if (err != NoError) continue;

		GS::ObjectState os;
		err = SerializeElementType (element, memo, os);
		if (err != NoError) continue;

		listAdder (os);
	}

	return result;
}


}
