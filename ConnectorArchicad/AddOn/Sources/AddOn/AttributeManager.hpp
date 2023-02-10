#ifndef ATTRIBUTE_MANAGER_HPP
#define ATTRIBUTE_MANAGER_HPP

#include "ModelInfo.hpp"

class AttributeManager {
private:
	GS::HashTable< GS::UniString, API_Attribute> cache;

public:
	GSErrCode GetMaterial (const ModelInfo::Material& material, API_Attribute& attribute);
	GSErrCode GetDefaultMaterial (API_Attribute& attribute, GS::UniString& name);
};

#endif
