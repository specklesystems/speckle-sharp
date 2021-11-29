#ifndef UTILITY_HPP
#define UTILITY_HPP

#include "APIEnvir.h"
#include "ACAPinc.h"


namespace Utility {

extern const GS::HashTable<API_ElemTypeID, GS::UniString> elementNames;

API_ElemTypeID GetElementType (const API_Guid& guid);

bool IsElement3D (const API_Guid& guid);

GS::Array<API_StoryType> GetStoryItems ();

double GetStoryLevel (short floorNumber);

}


#endif