#ifndef UTILITY_HPP
#define UTILITY_HPP

#include "APIEnvir.h"
#include "ACAPinc.h"


namespace Utility {

API_ElemTypeID GetElementType (const API_Guid& guid);

bool IsElement3D (const API_Guid& guid);



}


#endif