#ifndef UTILITY_HPP
#define UTILITY_HPP

#include "APIEnvir.h"
#include "ACAPinc.h"


namespace Utility {

static const GS::HashTable<API_ElemTypeID, GS::UniString> elementNames;

API_ElemTypeID GetElementType (const API_Guid& guid);

bool IsElement3D (const API_Guid& guid);



}


#endif