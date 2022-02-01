#ifndef TYPE_NAME_TABLES_HPP
#define TYPE_NAME_TABLES_HPP

#include "APIEnvir.h"
#include "ACAPinc.h"

extern const GS::HashTable<API_ElemTypeID, GS::UniString> elementNames;
extern const GS::HashTable<API_ModelElemStructureType, GS::UniString> structureTypeNames;

extern const GS::HashTable<API_WallTypeID, GS::UniString> wallTypeNames;
extern const GS::HashTable<short, GS::UniString> profileTypeNames;

extern const GS::HashTable<API_EdgeTrimID, GS::UniString> edgeAngleTypeNames;
extern const GS::HashTable<API_SlabReferencePlaneLocationID, GS::UniString> referencePlaneLocationNames;


#endif