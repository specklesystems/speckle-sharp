#include "ClassificationImportManager.hpp"


ClassificationImportManager* ClassificationImportManager::instance = nullptr;

ClassificationImportManager* ClassificationImportManager::GetInstance ()
{
	if (nullptr == instance) {
		instance = new ClassificationImportManager;
	}
	return instance;
}


void ClassificationImportManager::DeleteInstance ()
{
	if (nullptr != instance) {
		delete instance;
		instance = nullptr;
	}
}


ClassificationImportManager::ClassificationImportManager () {}


GSErrCode AddToCache (GS::Array<API_ClassificationItem> items, GS::HashTable<GS::UniString, API_Guid>& cache)
{
	for (auto& item : items) {
		cache.Add (item.id, item.guid);
		
		GS::Array<API_ClassificationItem> childrenItems;
		GSErrCode err = ACAPI_Classification_GetClassificationItemChildren (item.guid, childrenItems);
		if (err != NoError)
			return err;

		AddToCache (childrenItems, cache);
	}
	
	return NoError;
}


GSErrCode	ClassificationImportManager::GetItem (const GS::UniString& systemName, const GS::UniString& code, API_Guid& itemGuid)
{
	GS::ErrCode err = NoError;

	if (!cache.ContainsKey (systemName)) {
		API_ClassificationSystem targetSystem{};
		{
			GS::Array<API_ClassificationSystem> systems;
			err = ACAPI_Classification_GetClassificationSystems (systems);
			if (err != NoError)
				return err;

			err = Cancel;
			for (auto& system : systems) {
				if (system.name == systemName) {
					targetSystem = system;
					err = NoError;
					break;
				}
			}
			
			if (err != NoError)
				return err;
		}

		GS::Array<API_ClassificationItem> rootItems;
		err = ACAPI_Classification_GetClassificationSystemRootItems (targetSystem.guid, rootItems);
		if (err != NoError)
			return err;
		
		GS::HashTable<GS::UniString, API_Guid> systemCache;
		AddToCache (rootItems, systemCache);
		cache.Add(systemName, systemCache);
	}
	
	if (cache[systemName].Get (code, &itemGuid))
		return NoError;

	return Error;
}
