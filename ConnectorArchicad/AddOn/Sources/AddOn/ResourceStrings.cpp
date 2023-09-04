#include "ResourceStrings.hpp"

#include "ResourceIds.hpp"

// from GSRoot
#include "RS.hpp"

//from API
#include "ACAPinc.h"


template<class EnumType>
const GS::UniString& TGetStringFromResource (const int& resourceID, const EnumType& resourceItemId)
{
	static GS::HashTable<EnumType, GS::UniString> resourceStringCache;

	if (!resourceStringCache.ContainsKey (resourceItemId))
		resourceStringCache.Add (resourceItemId, RSGetIndString (resourceID, (UInt32) resourceItemId, ACAPI_GetOwnResModule ()));

	return resourceStringCache.Get(resourceItemId);
}


const GS::UniString& ResourceStrings::GetElementTypeStringFromResource (const ElementTypeStringItems& resourceItemId)
{
	return TGetStringFromResource<ResourceStrings::ElementTypeStringItems> (ID_ELEMENT_TYPE_STRINGS, resourceItemId);
}


const GS::UniString& ResourceStrings::GetFixElementTypeStringFromResource (const ElementTypeStringItems& resourceItemId)
{
	return TGetStringFromResource<ResourceStrings::ElementTypeStringItems> (ID_FIX_ELEMENT_TYPE_STRINGS, resourceItemId);
}
