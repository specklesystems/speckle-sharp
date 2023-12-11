#ifndef PROPERTY_EXPORT_MANAGER_HPP
#define PROPERTY_EXPORT_MANAGER_HPP

#include "APIEnvir.h"
#include "ACAPinc.h"

#include "Utility.hpp"


class PropertyExportManager {
private:
	static PropertyExportManager* instance;

	struct PropertyGroupFilter {
		GS::HashSet<API_Guid> elementPropertiesFilter;
		GS::HashSet<API_Guid> componentPropertyGroupFilter;
	};

	PropertyGroupFilter propertyGroupFilter;
	GS::HashSet<API_ElemType> complexElementsToSkipFromComponentListing;

	GS::HashTable<GS::UInt64, GS::Pair<GS::Array<API_PropertyDefinition>, GS::Array<API_PropertyDefinition>>> cache;

protected:
	PropertyExportManager ();

public:
	PropertyExportManager (PropertyExportManager&) = delete;
	void operator=(const PropertyExportManager&) = delete;
	static PropertyExportManager* GetInstance ();
	static void					DeleteInstance ();

	GSErrCode	GetElementDefinitions (const API_Element& element, const bool& sendProperties, const bool& sendListingParameters, const GS::Array<GS::Pair<API_Guid, API_Guid>>& systemItemPairs, GS::Array<API_PropertyDefinition>& elementsDefinitions, GS::Array < GS::Pair<API_ElemComponentID, GS::Array<API_PropertyDefinition>>>& componentsDefinitions);
};

#endif
