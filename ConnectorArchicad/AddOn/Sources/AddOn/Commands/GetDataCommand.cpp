#include "GetDataCommand.hpp"
#include "ObjectState.hpp"
#include "FieldNames.hpp"
#include "Utility.hpp"


namespace AddOnCommands
{

	
GS::ErrCode GetDataCommand::ExportClassificationsAndProperties (const API_Element& elem, GS::ObjectState& os) const
{
	GS::ErrCode err = NoError;

	{
		GS::UniString typeName;
		err = Utility::GetLocalizedElementTypeName (elem.header, typeName);
		if (err != NoError)
			return err;
		
		os.Add(FieldNames::ElementBase::ElementType, typeName);
	}
	
	GS::Array<GS::Pair<API_Guid, API_Guid>> systemItemPairs;
	err = ACAPI_Element_GetClassificationItems (elem.header.guid, systemItemPairs);
	if (err != NoError)
		return err;

	const auto& classificationListAdder = os.AddList<GS::ObjectState> (FieldNames::ElementBase::Classifications);
	for (const auto& systemItemPair : systemItemPairs) {
		GS::ObjectState classificationOs;
		API_ClassificationSystem system;
		system.guid = systemItemPair.first;
		err = ACAPI_Classification_GetClassificationSystem (system);
		if (err != NoError)
			break;

		classificationOs.Add (FieldNames::ElementBase::Classification::System, system.name);
		
		API_ClassificationItem item;
		item.guid = systemItemPair.second;
		err = ACAPI_Classification_GetClassificationItem (item);
		if (err != NoError)
			break;

		if (!item.id.IsEmpty())
			classificationOs.Add (FieldNames::ElementBase::Classification::Code, item.id);

		if (!item.name.IsEmpty())
			classificationOs.Add (FieldNames::ElementBase::Classification::Name, item.name);

		classificationListAdder (classificationOs);
	}

	return err;
}


GS::UInt64 GetDataCommand::GetMemoMask () const
{
	return APIMemoMask_All;
}


GS::ErrCode GetDataCommand::SerializeElementType(const API_Element& elem, const API_ElementMemo& /*memo*/, GS::ObjectState& os) const
{
	GS::ErrCode err = NoError;

	os.Add(FieldNames::ElementBase::ApplicationId, APIGuidToString (elem.header.guid));

	err = ExportClassificationsAndProperties (elem, os);

	return err;
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

		// check for elem type
		if (API_ZombieElemID != GetElemTypeID ()) {
			API_ElemTypeID elementType = Utility::GetElementType (element.header);
			if (elementType != GetElemTypeID ())
			{
				continue;
			}
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
