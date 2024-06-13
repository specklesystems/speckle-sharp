#ifndef TYPE_NAME_TABLES_HPP
#define TYPE_NAME_TABLES_HPP

#include "APIEnvir.h"
#include "ACAPinc.h"

#include "Utility.hpp"

extern const GS::HashTable<API_ElemType, GS::UniString> elementNames;
GSErrCode GetElementTypeName (const API_ElemType& elementType, GS::UniString& elementTypeName);

extern const GS::HashTable<API_ModelElemStructureType, GS::UniString> structureTypeNames;

extern const GS::HashTable<API_WallTypeID, GS::UniString> wallTypeNames;
extern const GS::HashTable<API_WallReferenceLineLocationID, GS::UniString> referenceLineLocationNames;
extern const GS::HashTable<short, GS::UniString> profileTypeNames;
extern const GS::HashTable<API_ElemDisplayOptionsID, GS::UniString> displayOptionNames;
extern const GS::HashTable<API_ElemProjectionModesID, GS::UniString> projectionModeNames;
extern const GS::HashTable<API_ElemViewDepthLimitationsID, GS::UniString > viewDepthLimitationNames;
extern const GS::HashTable<API_ZoneRelID, GS::UniString > relationToZoneNames;
extern const GS::HashTable<Int32, GS::UniString> beamFlagNames;

extern const GS::HashTable<API_EdgeTrimID, GS::UniString> edgeAngleTypeNames;
extern const GS::HashTable<API_SlabReferencePlaneLocationID, GS::UniString> referencePlaneLocationNames;

extern const  GS::HashTable<API_RoofClassID, GS::UniString> roofClassNames;
extern const  GS::HashTable<API_ShellClassID, GS::UniString> shellClassNames;
extern const  GS::HashTable<API_ShellBaseCutBodyTypeID, GS::UniString> shellBaseCutBodyTypeNames;
extern const  GS::HashTable<API_MorphingRuleID, GS::UniString> morphingRuleNames;
extern const  GS::HashTable<API_ShellBaseContourEdgeTypeID, GS::UniString> shellBaseContourEdgeTypeNames;
extern const  GS::HashTable<API_PolyRoofSegmentAngleTypeID, GS::UniString> polyRoofSegmentAngleTypeNames;

extern const GS::HashTable<API_AssemblySegmentLengthTypeID, GS::UniString> segmentLengthTypeNames;
extern const GS::HashTable<API_AssemblySegmentCutTypeID, GS::UniString> assemblySegmentCutTypeNames;
extern const GS::HashTable<API_BHoleTypeID, GS::UniString> beamHoleTypeNames;
extern const GS::HashTable<API_BeamVisibleLinesID, GS::UniString> beamVisibleLinesNames;
extern const GS::HashTable<API_BeamShapeTypeID, GS::UniString> beamShapeTypeNames;
extern const GS::HashTable<API_WindowDoorDirectionTypes, GS::UniString> windowDoorDirectionTypeNames;
extern const GS::HashTable<API_VerticalLinkID, GS::UniString> verticalLinkTypeNames;

extern const GS::HashTable<API_SkylightFixModeID, GS::UniString> skylightFixModeNames;
extern const GS::HashTable<API_SkylightAnchorID, GS::UniString> skylightAnchorNames;

extern const GS::HashTable<API_VeneerTypeID, GS::UniString> venTypeNames;

extern const GS::HashTable<short, GS::UniString> coreSymbolTypeNames;

extern const GS::HashTable<API_OpeningFloorPlanDisplayModeTypeID, GS::UniString> openingFloorPlanDisplayModeNames;
extern const GS::HashTable<API_OpeningFloorPlanConnectionModeTypeID, GS::UniString> openingFloorPlanConnectionModeNames;
extern const GS::HashTable<API_OpeningFloorPlanOutlinesStyleTypeID, GS::UniString> openingOutlinesStyleNames;
extern const GS::HashTable<API_OpeningBasePolygonTypeTypeID, GS::UniString> openingBasePolygonTypeNames;
extern const GS::HashTable<API_AnchorID, GS::UniString> openingAnchorNames;

extern const GS::HashTable<API_OpeningLimitTypeTypeID, GS::UniString> openingLimitTypeNames;
extern const GS::HashTable<API_OpeningLinkedStatusTypeID, GS::UniString> openingLinkedStatusNames;
#endif
