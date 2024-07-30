#include "GetDataCommand.hpp"
#include "ObjectState.hpp"
#include "FieldNames.hpp"
#include "Utility.hpp"
#include "PropertyExportManager.hpp"

#include "BM.hpp"

#include <map>
#include <vector>

namespace {

	/*!
	 Structure describing the surface area and volume quantities of a material in an element
	 */
	struct MaterialQuantity {
			//The material index
		API_AttributeIndex materialIndex{};
			//The net volume
		double volume = 0.0;
			//The net surface area
		double surfaceArea = 0.0;
	};
	
		///Array of elements
	using ElementArray = std::vector<API_Element>;
		///Array of material quantities
	using MaterialQuantArray = std::vector<MaterialQuantity>;
		///Array of quantities from a composite structure
	using CompositeQuantityArray = GS::Array<API_CompositeQuantity>;
		///Function to retrieve the material index from an element
	using MaterialGet = std::function<API_AttributeIndex(const API_Element&)>;
		///Function to set the volume mask in a quantities calculation mask
	using MaskSet = std::function<void(API_ElementQuantityMask&)>;
		///Function to get the volume from a quantity calculation result
	using QuantGet = std::function<double(const API_ElementQuantity&)>;
	
	
	/*!
	 Structure facilitating the measurement of material quantities in an element
	 Each member is a function providing key information for quantity take-offs
	 */
	struct QuantityManager {
			///Gets the element building material
		MaterialGet getMaterial;
			///Sets the volume take-off mask for the element
		MaskSet setVolumeMask;
			///Gets the volume for the element (for the above material)
		QuantGet getVolume;
			///Sets the surface area take-off mask for the element
		MaskSet setAreaMask;
			///Gets the surface area for the element (for the above material)
		QuantGet getArea;
	};
	
	/*!
	 Set a mask in a specified field (i.e. mark with a non-zero value to flag that this value should be calculated)
	 @param field A pointer to the target field
	 */
	void setMask(void* field) {
		*(reinterpret_cast<unsigned char*>(field)) = 0xFF;
	}
	
	/*!
	 Quantity management operation for handled Archicad element types (combines getter/setter for material, quants mask and quants values
	 NB: The API for elements is C structs with no abstraction or polymorphic behaviours. Rather than writing object wrappers for these structs,
	 the following collections of functions facilitate required getters/setters based on a type identifier
	 */
	std::map<API_ElemTypeID, QuantityManager> quantityManager = {
		{ API_BeamSegmentID,
			{
				[](const API_Element& element){ return element.beamSegment.assemblySegmentData.buildingMaterial; },
				[](API_ElementQuantityMask& mask){ setMask(&mask.beamSegment.volume); },
				[](const API_ElementQuantity& quant){ return quant.beamSegment.volume; },
				[](API_ElementQuantityMask& mask){ setMask(&mask.beamSegment.rightSurface); },
				[](const API_ElementQuantity& quant){ return quant.beamSegment.rightSurface; },
			}
		},
		{ API_ColumnSegmentID,
			{
				[](const API_Element& element){ return element.columnSegment.assemblySegmentData.buildingMaterial; },
				[](API_ElementQuantityMask& mask){ setMask(&mask.columnSegment.volume); },
				[](const API_ElementQuantity& quant){ return quant.columnSegment.volume; },
				[](API_ElementQuantityMask& mask){ setMask(&mask.columnSegment.coreNetSurface); },
				[](const API_ElementQuantity& quant){ return quant.columnSegment.coreNetSurface; },
			}
		},
		{ API_MeshID,
			{
				[](const API_Element& element){ return element.mesh.buildingMaterial; },
				[](API_ElementQuantityMask& mask){ setMask(&mask.mesh.volume); },
				[](const API_ElementQuantity& quant){ return quant.mesh.volume; },
				[](API_ElementQuantityMask& mask){ setMask(&mask.mesh.topSurface); },
				[](const API_ElementQuantity& quant){ return quant.mesh.topSurface; },
			}
		},
		{ API_MorphID,
			{
				[](const API_Element& element){ return element.morph.buildingMaterial; },
				[](API_ElementQuantityMask& mask){ setMask(&mask.morph.volume); },
				[](const API_ElementQuantity& quant){ return quant.morph.volume; },
				[](API_ElementQuantityMask& mask){ setMask(&mask.morph.surface); },
				[](const API_ElementQuantity& quant){ return quant.morph.surface; },
			}
		},
		{ API_RoofID,
			{
				[](const API_Element& element){ return element.roof.shellBase.buildingMaterial; },
				[](API_ElementQuantityMask& mask){ setMask(&mask.roof.volume); },
				[](const API_ElementQuantity& quant){ return quant.roof.volume; },
				[](API_ElementQuantityMask& mask){ setMask(&mask.roof.topSurface); },
				[](const API_ElementQuantity& quant){ return quant.roof.topSurface; },
			}
		},
		{ API_ShellID,
			{
				[](const API_Element& element){ return element.shell.shellBase.buildingMaterial; },
				[](API_ElementQuantityMask& mask){ setMask(&mask.shell.volume); },
				[](const API_ElementQuantity& quant){ return quant.shell.volume; },
				[](API_ElementQuantityMask& mask){ setMask(&mask.shell.referenceSurface); },
				[](const API_ElementQuantity& quant){ return quant.shell.referenceSurface; },
			}
		},
		{ API_SlabID,
			{
				[](const API_Element& element){ return element.slab.buildingMaterial; },
				[](API_ElementQuantityMask& mask){ setMask(&mask.slab.volume); },
				[](const API_ElementQuantity& quant){ return quant.slab.volume; },
				[](API_ElementQuantityMask& mask){ setMask(&mask.slab.topSurface); },
				[](const API_ElementQuantity& quant){ return quant.slab.topSurface; },
			}
		},
		{ API_WallID,
			{
				[](const API_Element& element){ return element.wall.buildingMaterial; },
				[](API_ElementQuantityMask& mask){ setMask(&mask.wall.volume); },
				[](const API_ElementQuantity& quant){ return quant.wall.volume; },
				[](API_ElementQuantityMask& mask){ setMask(&mask.wall.surface1); },
				[](const API_ElementQuantity& quant){ return quant.wall.surface1; },
			}
		},
	};
	
	
	/*!
	 Collect the individual segments from a segmented assembly (e.g. beam/column)
	 @param segments A pointer to the assembly segments
	 @return An array containing the individual segments
	 */
	template<typename T, typename Insert>
	auto getSegments(const T* segments, Insert insert) {
		ElementArray result;
		if (segments == nullptr)
			return result;
		for (auto segmentIndex = BMGetPtrSize((GSConstPtr) segments) / sizeof(T); segmentIndex--; ) {
			API_Element element{};
			insert(segments[segmentIndex], element);
			result.push_back(element);
		}
		return result;
	} //getBeamSegments
	
	
	/*!
	 Get the type identifier for an element
	 @param element The target element
	 @return The element type ID, e.g. `API_WallID`
	 */
	API_ElemTypeID getTypeID(const API_Element& element) {
#ifdef ServerMainVers_2600
		return element.header.type.typeID;
#else
		return element.header.typeID;
#endif
	}
	
	
	/*!
	 Determine if the specified element is an assembly, i.e. made of multiple parts (sub-elements)
	 @return True if the element is an assembly
	 */
	bool isAssembly(const API_Element& element) {
		auto typeID = getTypeID(element);
		return (typeID == API_BeamID) || (typeID == API_ColumnID);
	} //isAssembly
	
	
	/*!
	 Get the constituent parts of a specified element, e.g. the segments in a beam/column
	 @param element The source element
	 @param memo The source element memo
	 @return An array of element parts (empty if the element is not made of parts - the original element is never included)
	 */
	auto getElementParts(const API_Element& element, const API_ElementMemo& memo) {
		switch (getTypeID(element)) {
			case API_BeamID:
				return getSegments(memo.beamSegments, [](const API_BeamSegmentType& segment, API_Element& element){ element.beamSegment = segment; });
			case API_ColumnID:
				return getSegments(memo.columnSegments, [](const API_ColumnSegmentType& segment, API_Element& element){ element.columnSegment = segment; });
			default:
				break;
		}
		return ElementArray{};
	} //getElementParts
	
	
	/*!
	 Get the structure type for a specified element, e.g. how materials are arranged in the element body
	 @param element The target element
	 @return The element structural type
	 */
	auto getStructureType(const API_Element& element) {
		switch (getTypeID(element)) {
			case API_BeamSegmentID:
				return element.beamSegment.assemblySegmentData.modelElemStructureType;
			case API_ColumnSegmentID:
				return element.columnSegment.assemblySegmentData.modelElemStructureType;
			case API_RoofID:
				return element.roof.shellBase.modelElemStructureType;
			case API_ShellID:
				return element.shell.shellBase.modelElemStructureType;
			case API_SlabID:
				return element.slab.modelElemStructureType;
			case API_WallID:
				return element.wall.modelElemStructureType;
			default:
				return API_BasicStructure;
		}
	} //getStructureType
	
	
	/*!
	 Collect material quantities from the composite materials of a specified element
	 @param element The target element to export the properties from
	 @param elementQuantity Quantities extracted from the target element (out)
	 @param extendedQuantity Optional extended quantities calculated for some element types
	 @param quantityMask Mask to determine which quantities are required (minimise calculation time)
	 @return An error code (NoError = success)
	 */
	GS::ErrCode measureQuantities(const API_Element& element, API_ElementQuantity& elementQuantity, API_Quantities& extendedQuantity,
						   const API_QuantitiesMask& quantityMask) {
		extendedQuantity.elements = &elementQuantity;
		GS::Array<API_ElemPartQuantity> elementPartQuantities;
		API_QuantityPar quantityParameters{};
		quantityParameters.minOpeningSize = Eps;
		MaterialQuantArray result;
		return ACAPI_Element_GetQuantities(element.header.guid, &quantityParameters, &extendedQuantity, &quantityMask);
	} //measureQuantities
	
	
	/*!
	 Collect material quantities from the basic (single homogeneous) material of a specified element
	 @param element The target element to export the properties from
	 @return An array of material quantities collected from the element
	 */
	auto collectBasicQuantitites(const API_Element& element) {
		MaterialQuantArray result;
			//First determine that this element type has a suitable material definition and supports quantity take-offs for volume and area
		auto manager = quantityManager.find(getTypeID(element));
		if (manager != quantityManager.end()) {
			API_ElementQuantity elementQuantity{};
			API_Quantities extendedQuantity{};
			API_QuantitiesMask quantityMask{};
				//Set the appropriate masks for material volume/area quantity takeoffs
			manager->second.setVolumeMask(quantityMask.elements);
			manager->second.setAreaMask(quantityMask.elements);
			measureQuantities(element, elementQuantity, extendedQuantity, quantityMask);
				//Create a material quantity from the quantity takeoff
			result.push_back({
				manager->second.getMaterial(element),
				manager->second.getVolume(elementQuantity),
				manager->second.getArea(elementQuantity)
			});
		}
		return result;
	} //collectBasicQuantitites
	
	
	/*!
	 Collect material quantities from the composite materials of a specified element
	 @param element The target element to export the properties from
	 @return An array of material quantities collected from the element composite structure
	 */
	auto collectCompositeQuantities(const API_Element& element) {
		API_ElementQuantity elementQuantity{};
		API_Quantities extendedQuantity{};
		CompositeQuantityArray compositeQuantity{};
		extendedQuantity.composites = &compositeQuantity;
		API_QuantitiesMask quantityMask{};
			//Set the appropriate masks for composite material volume/area quantity takeoffs
		setMask(&quantityMask.composites.buildMatIndices);
		setMask(&quantityMask.composites.volumes);
		setMask(&quantityMask.composites.projectedArea);
		measureQuantities(element, elementQuantity, extendedQuantity, quantityMask);
		MaterialQuantArray result;
			//Create material quantities from the quantity takeoff (one oer skin in the composite structure)
		for (auto& skinQuant : compositeQuantity)
			result.push_back({skinQuant.buildMatIndices, skinQuant.volumes, skinQuant.projectedArea});
		return result;
	} //collectCompositeQuantities
	
	
	/*!
	 Get the material quantities for a specified element
	 @param element The source element
	 @param memo The memo data attached to the element
	 @return An array of material quantities extracted from the element
	 */
	MaterialQuantArray getQuantity(const API_Element& element, const API_ElementMemo& memo) {
		if (isAssembly(element)) {
			MaterialQuantArray result;
				//Get the constituent parts of the assembly and process each as an independent element
			auto parts = getElementParts (element, memo);
			if (!parts.empty()) {
				API_ElementMemo partMemo{};	//NB: The memo is not used for the quantity take-offs in part elements, so an empty structure is fine
				for (auto& part : parts) {
					auto partMaterials = getQuantity(part, partMemo);
					result.insert(result.end(), std::make_move_iterator(partMaterials.begin()), std::make_move_iterator(partMaterials.end()));
				}
			}
			return result;
		}
		switch (getStructureType(element)) {
			case API_BasicStructure:
				return collectBasicQuantitites(element);
			case API_CompositeStructure:
				return collectCompositeQuantities(element);
			case API_ProfileStructure:
				break;
		}
		return MaterialQuantArray{};
	} //getQuantity
	
	
	/*!
	 Serialise a specified material attribute for export
	 @param materialIndex The target material index
	 @param serialiser A serialiser for the exported data
	 @return NoError if the export serialisation completed without errors
	 */
	GS::ErrCode exportMaterial(API_AttributeIndex materialIndex, GS::ObjectState& serialiser) {
			//Attempt to load the material attribute using the index
		API_Attribute attribute{};
		attribute.header.index = materialIndex;
		attribute.header.typeID = API_BuildingMaterialID;
		auto error = ACAPI_Attribute_Get(&attribute);
		if (error != NoError)
			return error;
		serialiser.Add(FieldNames::Material::Name, GS::UniString{attribute.header.name});
		return NoError;
	} //exportMaterial
	
	
	/*!
	 Serialise the material quantities of a specified element for export
	 @param element The target element
	 @param memo The memo data attached to the element
	 @param serialiser A serialiser for the exported data
	 @return NoError if the export serialisation completed without errors
	 */
	GS::ErrCode exportMaterialQuantities(const API_Element& element, const API_ElementMemo& memo, GS::ObjectState& serialiser) {
		auto materialQuants = getQuantity(element, memo);
		if (materialQuants.empty())
			return NoError;
		const auto& serialMaterialQuants = serialiser.AddList<GS::ObjectState> (FieldNames::ElementBase::MaterialQuantities);
		for (auto& quantity : materialQuants) {
			GS::ObjectState serialMaterialQuant, serialMaterial;
			auto error = exportMaterial(quantity.materialIndex, serialMaterial);
			if (error != NoError)
				return error;
			serialMaterialQuant.Add(FieldNames::ElementBase::Quantity::Material, serialMaterial);
			serialMaterialQuant.Add(FieldNames::ElementBase::Quantity::Volume, quantity.volume);
			serialMaterialQuant.Add(FieldNames::ElementBase::Quantity::Area, quantity.surfaceArea);
			serialMaterialQuant.Add(FieldNames::ElementBase::Quantity::Units, GS::UniString{"m"});
			serialMaterialQuants(serialMaterialQuant);
		}
		return NoError;
	} //exportMaterialQuantities
	
}


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


/*!
 Export attached Archicad properties, listing properties (calculated) and material quantities from a specified element
 @param element The target element to export the properties from
 @param sendProperties True to export the Archicad properties attached to the element
 @param sendListingParameters True to export calculated listing parameters from the element, e.g. top/bottom surface area etc
 @param systemItemPairs Array pairing a classification system ID against a classification item ID (attached to the target element)
 @param os A collector/serialiser for the exported data
 @return NoError if the export was successful
 */
GS::ErrCode GetDataCommand::ExportProperties (const API_Element& element, const bool& sendProperties, const bool& sendListingParameters, const GS::Array<GS::Pair<API_Guid, API_Guid>>& systemItemPairs, GS::ObjectState& os) const
{
	if (!sendProperties && !sendListingParameters)
		return NoError;

	GS::Array<API_PropertyDefinition> elementDefinitions;
	GS::Array < GS::Pair<API_ElemComponentID, GS::Array<API_PropertyDefinition>>> componentsDefinitions;

	GS::ErrCode err = PropertyExportManager::GetInstance ()->GetElementDefinitions (element, sendProperties, sendListingParameters, systemItemPairs, elementDefinitions, componentsDefinitions);
	if (err != NoError)
		return err;

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
#ifdef ServerMainVers_2700
		err = ACAPI_Element_GetPropertyValues (componentDefinitions.first, componentDefinitions.second, properties);
#else
		err = ACAPI_ElemComponent_GetPropertyValues (componentDefinitions.first, componentDefinitions.second, properties);
#endif
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


/*!
 Serialise the attributes, properties and quantities of a specified element
 
 elem: The target element
 memo: Memo data attached to the element
 os: A collector/serialiser for the exported data
 sendProperties: True to export the Archicad properties attached to the element
 sendListingParameters: True to export calculated listing parameters from the element, e.g. top/bottom surface area etc
 
 return: NoError if the serialisation was successful
 */
GS::ErrCode GetDataCommand::SerializeElementType(const API_Element& elem, const API_ElementMemo& memo, GS::ObjectState& os, const bool& sendProperties, const bool& sendListingParameters) const
{
	os.Add(FieldNames::ElementBase::ApplicationId, APIGuidToString (elem.header.guid));

	API_Attribute attribute;
	BNZeroMemory (&attribute, sizeof (API_Attribute));
	attribute.header.typeID = API_LayerID;
	attribute.header.index = elem.header.layer;
	if (ACAPI_Attribute_Get (&attribute) == NoError) {
		os.Add(FieldNames::ElementBase::Layer, GS::UniString{attribute.header.name});
	}
	auto err = exportMaterialQuantities (elem, memo, os);
	if (err != NoError)
		return err;
	return ExportClassificationsAndProperties (elem, os, sendProperties, sendListingParameters);
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
