#ifndef LIBPART_IMPORT_MANAGER_HPP
#define LIBPART_IMPORT_MANAGER_HPP

#include "ModelInfo.hpp"
#include "AttributeManager.hpp"


class LibpartImportManager {
private:
	GS::HashTable<GS::UInt64, API_LibPart> cache;

	API_Attribute defaultMaterialAttribute;
	GS::UniString defaultMaterialName;
	
public:
	LibpartImportManager ();

	GSErrCode GetLibpart (const ModelInfo& modelInfo, AttributeManager& attributeManager, API_LibPart& libPart);

private:
	GSErrCode	CreateLibraryPart (const ModelInfo& modelInfo, AttributeManager& attributeManager, API_LibPart& libPart);
	GSErrCode	GetLocation (IO::Location*& loc, bool useEmbeddedLibrary) const;
	GS::UInt64	GenerateFingerPrint (const GS::Array<GS::UniString>& hashIds) const;
};

#endif
