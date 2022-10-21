#include "TypeNameTables.hpp"


const GS::HashTable<API_ElemTypeID, GS::UniString> elementNames
{
	{ API_ZombieElemID,					"InvalidType"},
	{ API_WallID,						"Wall"},
	{ API_ColumnID,						"Column"},
	{ API_BeamID,						"Beam"},
	{ API_WindowID,						"Window"},
	{ API_DoorID,						"Door"},
	{ API_ObjectID,						"Object"},
	{ API_LampID,						"Lamp"},
	{ API_SlabID,						"Slab"},
	{ API_RoofID,						"Roof"},
	{ API_MeshID,						"Mesh"},
	{ API_ZoneID,						"Zone"},
	{ API_CurtainWallID,				"CurtainWall"},
	{ API_ShellID,						"Shell"},
	{ API_SkylightID,					"Skylight"},
	{ API_MorphID,						"Morph"},
	{ API_StairID,						"Stair"},
	{ API_RailingID,					"Railing"},
	{ API_OpeningID,					"Opening"}
};


const GS::HashTable<API_ModelElemStructureType, GS::UniString> structureTypeNames
{
	{ API_BasicStructure,				"Basic"},
	{ API_CompositeStructure,			"Composite"},
	{ API_ProfileStructure,				"Complex Profile"}
};


const GS::HashTable<API_WallTypeID, GS::UniString> wallTypeNames
{
	{ APIWtyp_Normal,		"Straight"},
	{ APIWtyp_Trapez,		"Trapezoid"},
	{ APIWtyp_Poly,			"Polygonal"}
};


const GS::HashTable<short, GS::UniString> profileTypeNames
{
	{ APISect_Normal,		"Straight"},
	{ APISect_Poly,			"Profiled"},
	{ APISect_Slanted,		"Slanted"},
	{ APISect_Trapez,		"Double Slanted"}
};


const GS::HashTable<API_EdgeTrimID, GS::UniString> edgeAngleTypeNames
{
	{ APIEdgeTrim_CustomAngle,		"Custom Angle"},
	{ APIEdgeTrim_Perpendicular,	"Perpendicular"}
};


const GS::HashTable<API_SlabReferencePlaneLocationID, GS::UniString> referencePlaneLocationNames
{
	{ APISlabRefPlane_Bottom,		"Bottom"},
	{ APISlabRefPlane_CoreBottom,	"Core Bottom"},
	{ APISlabRefPlane_CoreTop,		"Core Top"},
	{ APISlabRefPlane_Top,			"Top"}
};


const GS::HashTable<API_AssemblySegmentLengthTypeID, GS::UniString> segmentLengthTypeNames
{
	{APIAssemblySegment_Fixed, "Fixed"},
	{APIAssemblySegment_Proportional, "Proportional"},
};


const GS::HashTable<API_AssemblySegmentCutTypeID, GS::UniString> assemblySegmentCutTypeNames
{
	{APIAssemblySegmentCut_Horizontal, "Horizontal"},
	{APIAssemblySegmentCut_Vertical, "Vertical"},
	{APIAssemblySegmentCut_Custom, "Custom"}
};


const GS::HashTable<API_BHoleTypeID, GS::UniString> beamHoleTypeNames
{
	{APIBHole_Rectangular, "Rectangular" },
	{APIBHole_Circular, "Circular"}
};


const GS::HashTable<API_BeamShapeTypeID, GS::UniString> beamShapeTypeNames
{
	{ API_StraightBeam, "Straight" },
	{ API_HorizontallyCurvedBeam, "HorizontallyCurved"},
	{ API_VerticallyCurvedBeam, "VerticallyCurved"}
};