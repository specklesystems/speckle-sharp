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

extern const GS::HashTable<API_AssemblySegmentLengthTypeID, GS::UniString> segmentLengthTypeNames;
extern const GS::HashTable<API_AssemblySegmentCutTypeID, GS::UniString> assemblySegmentCutTypeNames;
extern const GS::HashTable<API_BHoleTypeID, GS::UniString> beamHoleTypeNames;
extern const GS::HashTable<API_BeamShapeTypeID, GS::UniString> beamShapeTypeNames;

#endif