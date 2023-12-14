#include "GetDataCommand.hpp"
#include "ObjectState.hpp"
#include "FieldNames.hpp"
#include "Utility.hpp"
#include "PropertyExportManager.hpp"


namespace AddOnCommands
{


GS::ErrCode SerializePropertyGroups (const GS::Array<API_PropertyDefinition>& definitions, const GS::Array<API_Property>& properties, std::function<void (const GS::ObjectState&)> propertyGroupListAdder)
{
	GS::ErrCode err = NoError;

	GS::HashTable<API_Guid, GS::Pair<GS::UniString, GS::Array<API_PropertyDefinition>>> propertiesByGroup;

	for (UInt32 i = 0; i < definitions.GetSize (); i++) {
		API_PropertyDefinition definition = definitions[i];

		API_PropertyGroup group;
		group.guid = definition.groupGuid;
		err = ACAPI_Property_GetPropertyGroup (group);
		if (err != NoError)
			continue;

		const GS::UniString& groupName = group.name;
		const API_Guid& groupID = group.guid;

		if (propertiesByGroup.ContainsKey (groupID)) {
			propertiesByGroup[groupID].second.Push (definition);
		} else {
			GS::Array<API_PropertyDefinition> definitionsByGroup;
			definitionsByGroup.Push (definition);
			propertiesByGroup.Add (groupID, GS::Pair<GS::UniString, GS::Array<API_PropertyDefinition>> (groupName, definitionsByGroup));
		}
	}

	for (auto& propertyByGroup : propertiesByGroup) {
		const API_Guid& groupID = *propertyByGroup.key;

		GS::ObjectState propertyGroupsOs;
		propertyGroupsOs.Add (FieldNames::ElementBase::PropertyGroup::Name, propertyByGroup.value->first);
		const auto& propertyListAdder = propertyGroupsOs.AddList<GS::ObjectState> (FieldNames::ElementBase::PropertyGroup::PropertList);

		bool propertyAdded = false;
		for (auto& apiProperty : properties) {
			if (apiProperty.definition.groupGuid != groupID)
				continue;
			
			if (apiProperty.status == API_Property_HasValue && apiProperty.value.variantStatus == API_VariantStatusNormal) {
				switch (apiProperty.definition.collectionType) {
					case API_PropertySingleCollectionType:
					case API_PropertySingleChoiceEnumerationCollectionType:
					{
						GS::ObjectState propertyOs;
						propertyOs.Add (FieldNames::ElementBase::Property::Name, apiProperty.definition.name);

						switch (apiProperty.value.singleVariant.variant.type) {
						case API_PropertyIntegerValueType:
							propertyOs.Add (FieldNames::ElementBase::Property::Value, apiProperty.value.singleVariant.variant.intValue);
							break;
						case API_PropertyRealValueType:
							if (GS::ClassifyDouble (apiProperty.value.singleVariant.variant.doubleValue) == GS::DoubleClass::Normal)
								propertyOs.Add (FieldNames::ElementBase::Property::Value, apiProperty.value.singleVariant.variant.doubleValue);
							break;
						case API_PropertyStringValueType:
							propertyOs.Add (FieldNames::ElementBase::Property::Value, apiProperty.value.singleVariant.variant.uniStringValue);
							break;
						case API_PropertyBooleanValueType:
							propertyOs.Add (FieldNames::ElementBase::Property::Value, apiProperty.value.singleVariant.variant.boolValue);
							break;
						case API_PropertyGuidValueType:
							for (auto& possibleEnumValue : apiProperty.definition.possibleEnumValues) {
								if (possibleEnumValue.keyVariant.guidValue == apiProperty.value.singleVariant.variant.guidValue) {
									propertyOs.Add (FieldNames::ElementBase::Property::Value, possibleEnumValue.displayVariant.uniStringValue);
									break;
								}
							}
							break;
						default:
							continue;
						}

						propertyListAdder (propertyOs);
						propertyAdded = true;
						break;
					}
					case API_PropertyListCollectionType:
					case API_PropertyMultipleChoiceEnumerationCollectionType:
					{
						if (apiProperty.value.listVariant.variants.GetSize () == 0)
							continue;

						GS::ObjectState propertyOs;
						propertyOs.Add (FieldNames::ElementBase::Property::Name, apiProperty.definition.name);

						switch (apiProperty.value.listVariant.variants[0].type) {
						case API_PropertyIntegerValueType:
						{
							const auto& valueListAdder = propertyOs.AddList<int> (FieldNames::ElementBase::Property::Values);
							for (auto value : apiProperty.value.listVariant.variants) {
								valueListAdder (value.intValue);
							}
							break;
						}
						case API_PropertyRealValueType:
						{
							const auto& valueListAdder = propertyOs.AddList<double> (FieldNames::ElementBase::Property::Values);
							for (auto value : apiProperty.value.listVariant.variants) {
								if (GS::ClassifyDouble (value.doubleValue) == GS::DoubleClass::Normal)
									valueListAdder (value.doubleValue);
							}
							break;
						}
						case API_PropertyStringValueType:
						{
							const auto& valueListAdder = propertyOs.AddList<GS::UniString> (FieldNames::ElementBase::Property::Values);
							for (auto value : apiProperty.value.listVariant.variants) {
								valueListAdder (value.uniStringValue);
							}
							break;
						}
						case API_PropertyBooleanValueType:
						{
							const auto& valueListAdder = propertyOs.AddList<bool> (FieldNames::ElementBase::Property::Values);
							for (auto value : apiProperty.value.listVariant.variants) {
								valueListAdder (value.boolValue);
							}
							break;
						}
						case API_PropertyGuidValueType:
						{
							const auto& valueListAdder = propertyOs.AddList<GS::UniString> (FieldNames::ElementBase::Property::Values);
							for (auto& possibleEnumValue : apiProperty.definition.possibleEnumValues) {
								for (auto value : apiProperty.value.listVariant.variants) {
									if (possibleEnumValue.keyVariant.guidValue == value.guidValue) {
										valueListAdder (possibleEnumValue.displayVariant.uniStringValue);
										break;
									}
								}
							}
							break;
						}
						default:
							continue;
						}

						propertyListAdder (propertyOs);
						propertyAdded = true;
						break;
					}
					case API_PropertyUndefinedCollectionType:
						continue;
				}
			}
		}

		if (propertyAdded)
			propertyGroupListAdder (propertyGroupsOs);
	}

	return NoError;
}


GS::ErrCode GetDataCommand::ExportProperties (const API_Element& element, const bool& sendProperties, const bool& sendListingParameters, const GS::Array<GS::Pair<API_Guid, API_Guid>>& systemItemPairs, GS::ObjectState& os) const
{
	GS::ErrCode err = NoError;

	if (!sendProperties && !sendListingParameters)
		return NoError;

	GS::Array<API_PropertyDefinition> elementDefinitions;
	GS::Array < GS::Pair<API_ElemComponentID, GS::Array<API_PropertyDefinition>>> componentsDefinitions;

	err = PropertyExportManager::GetInstance ()->GetElementDefinitions (element, sendProperties, sendListingParameters, systemItemPairs, elementDefinitions, componentsDefinitions);
	if (err != NoError)
		return false;

	// element properties
	{
		GS::Array<API_Property> properties;
		err = ACAPI_Element_GetPropertyValues (element.header.guid, elementDefinitions, properties);
		if (err == NoError && !properties.IsEmpty ()) {
			const auto& propertyGroupListAdder = os.AddList<GS::ObjectState> (FieldNames::ElementBase::ElementProperties);
			err = SerializePropertyGroups (elementDefinitions, properties, propertyGroupListAdder);
			if (err != NoError)
				return err;
		}
	}

	// components properties
	auto componentPropertyListAdder = os.AddList<GS::ObjectState> (FieldNames::ElementBase::ComponentProperties);

	UInt32 componentNumber (1);
	for (auto& componentDefinitions : componentsDefinitions) {
		GS::Array<API_Property> properties;
		err = ACAPI_ElemComponent_GetPropertyValues (componentDefinitions.first, componentDefinitions.second, properties);
		if (err != NoError || properties.IsEmpty ())
			continue;

		GS::ObjectState componentPropertiesOs;
		componentPropertiesOs.Add (FieldNames::ElementBase::ComponentProperty::Name, GS::String::SPrintf ("Component %d", componentNumber++));
		std::function<void (const GS::ObjectState&)> propertyGroupListAdder = componentPropertiesOs.AddList<GS::ObjectState> (FieldNames::ElementBase::ComponentProperty::PropertyGroups);
		
		err = SerializePropertyGroups (componentDefinitions.second, properties, propertyGroupListAdder);
		if (err != NoError)
			continue;

		componentPropertyListAdder (componentPropertiesOs);
	}

	return NoError;
}


GS::ErrCode GetDataCommand::ExportClassificationsAndProperties (const API_Element& element, GS::ObjectState& os, const bool& sendProperties, const bool& sendListingParameters) const
{
	GS::ErrCode err = NoError;

	{
		GS::UniString typeName;
		err = Utility::GetLocalizedElementTypeName (element.header, typeName);
		if (err != NoError)
			return err;
		
		os.Add(FieldNames::ElementBase::ElementType, typeName);
	}
	
	GS::Array<GS::Pair<API_Guid, API_Guid>> systemItemPairs;
	err = ACAPI_Element_GetClassificationItems (element.header.guid, systemItemPairs);
	if (err != NoError)
		return err;

	if (systemItemPairs.GetSize () != 0) {
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

			if (!item.id.IsEmpty ())
				classificationOs.Add (FieldNames::ElementBase::Classification::Code, item.id);

			if (!item.name.IsEmpty ())
				classificationOs.Add (FieldNames::ElementBase::Classification::Name, item.name);

			classificationListAdder (classificationOs);
		}
	}

	return ExportProperties (element, sendProperties, sendListingParameters, systemItemPairs, os);
}


GS::UInt64 GetDataCommand::GetMemoMask () const
{
	return APIMemoMask_All;
}


GS::ErrCode GetDataCommand::SerializeElementType(const API_Element& elem, const API_ElementMemo& /*memo*/, GS::ObjectState& os, const bool& sendProperties, const bool& sendListingParameters) const
{
	GS::ErrCode err = NoError;

	os.Add(FieldNames::ElementBase::ApplicationId, APIGuidToString (elem.header.guid));

	API_Attribute attribute;
	BNZeroMemory (&attribute, sizeof (API_Attribute));
	attribute.header.typeID = API_LayerID;
	attribute.header.index = elem.header.layer;
	if (NoError == ACAPI_Attribute_Get (&attribute)) {
		os.Add(FieldNames::ElementBase::Layer, GS::UniString{attribute.header.name});
	}

	err = ExportClassificationsAndProperties (elem, os, sendProperties, sendListingParameters);

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

	bool sendProperties = false;
	bool sendListingParameters = false;
	parameters.Get (FieldNames::ElementBase::SendProperties, sendProperties);
	parameters.Get (FieldNames::ElementBase::SendListingParameters, sendListingParameters);

	GS::ObjectState result;
	const auto& listAdder = result.AddList<GS::ObjectState> (GetFieldName ());
	for (const API_Guid& guid : elementGuids) {
		API_Element element{};
		API_ElementMemo memo{};

		element.header.guid = guid;

		GSErrCode err = ACAPI_Element_Get (&element);
		if (err != NoError)
			continue;

		// check for elem type
		if (API_ZombieElemID != GetElemTypeID ()) {
			API_ElemTypeID elementType = Utility::GetElementType (element.header).typeID;
			if (elementType != GetElemTypeID ()) {
				continue;
			}
		}

		err = ACAPI_Element_GetMemo (guid, &memo, GetMemoMask ());
		if (err != NoError)
			continue;

		GS::ObjectState os;
		err = SerializeElementType (element, memo, os, sendProperties, sendListingParameters);
		if (err != NoError)
			continue;
		
		err = SerializeElementType (element, memo, os);
		if (err != NoError)
			continue;

		listAdder (os);
	}

	return result;
}


}
