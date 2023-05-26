#ifndef LIBPART_IMPORT_MANAGER_HPP
#define LIBPART_IMPORT_MANAGER_HPP

#include "ModelInfo.hpp"
#include "AttributeManager.hpp"


class LibpartImportManager {
private:
	static LibpartImportManager*			instance;
	IO::Location*							libraryFolderLocation;
	GS::HashTable<GS::UInt64, API_LibPart>	cache;

	API_Attribute							defaultMaterialAttribute;
	GS::UniString							defaultMaterialName;
	UInt32									runningNumber;

protected:
	LibpartImportManager ();

public:
	~LibpartImportManager ();
	
	LibpartImportManager (LibpartImportManager&) = delete;
	void		operator=(const LibpartImportManager&) = delete;
	static LibpartImportManager*	GetInstance ();
	static void						DeleteInstance ();

	GSErrCode	GetLibpart (const ModelInfo& modelInfo, AttributeManager& attributeManager, API_LibPart& libPart);

	GSErrCode GetLibpartFromCache(const GS::Array<GS::UniString> modelIds, API_LibPart& libPart);

private:
	GSErrCode	CreateLibraryPart (const ModelInfo& modelInfo, AttributeManager& attributeManager, API_LibPart& libPart);
	GSErrCode	GetLocation (bool useEmbeddedLibrary, IO::Location*& libraryFolderLocation) const;
	GS::UInt64	GenerateFingerPrint (const GS::Array<GS::UniString>& hashIds) const;
};

#endif
