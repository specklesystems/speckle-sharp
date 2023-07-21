#ifndef CLASSIFICATION_IMPORT_MANAGER_HPP
#define CLASSIFICATION_IMPORT_MANAGER_HPP

#include "ModelInfo.hpp"

class ClassificationImportManager {
private:
	static ClassificationImportManager* instance;

	GS::HashTable< GS::UniString, GS::HashTable<GS::UniString, API_Guid>> cache;

protected:
	ClassificationImportManager ();

public:
	ClassificationImportManager (ClassificationImportManager&) = delete;
	void		operator=(const ClassificationImportManager&) = delete;
	static ClassificationImportManager* GetInstance ();
	static void					DeleteInstance ();

	GSErrCode	GetItem (const GS::UniString& systemName, const GS::UniString& itemID, API_Guid& itemGuid);
};

#endif
