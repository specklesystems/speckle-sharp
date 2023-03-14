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
	{ API_BasicStructure,			"Basic"},
	{ API_CompositeStructure,		"Composite"},
	{ API_ProfileStructure,			"Complex Profile"}
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


const GS::HashTable<API_WallReferenceLineLocationID, GS::UniString> referenceLineLocationNames
{
	{ APIWallRefLine_Outside,			"Outside"},
	{ APIWallRefLine_Center,			"Center"},
	{ APIWallRefLine_Inside,			"Inside"},
	{ APIWallRefLine_CoreOutside,		"Core Outside"},
	{ APIWallRefLine_CoreCenter,		"Core Center"},
	{ APIWallRefLine_CoreInside,		"Core Inside"}
};


const GS::HashTable<API_ElemDisplayOptionsID, GS::UniString> displayOptionNames
{
	{ API_Standard,						"Projected"},
	{ API_StandardWithAbstract,			"Projected with Overhead"},
	{ API_CutOnly,						"Cut Only"},
	{ API_OutLinesOnly,					"Outlines Only"},
	{ API_AbstractAll,					"Overhead All"},
	{ API_CutAll,						"Symbolic Cut"}
};


const GS::HashTable<API_ElemProjectionModesID, GS::UniString> projectionModeNames
{
	{ API_Symbolic,		"Symbolic"},
	{ API_Projected,	"Projected"},
	{ API_Hybrid,		"Hybrid"}
};


const GS::HashTable<API_ElemViewDepthLimitationsID, GS::UniString> viewDepthLimitationNames
{
	{ API_ToFloorPlanRange,		"To Floor Plan Range"},
	{ API_ToAbsoluteLimit,		"To Absolute Display Limit"},
	{ API_EntireElement,		"Entire Element"}
};


const GS::HashTable<API_ZoneRelID, GS::UniString> relationToZoneNames
{
	{ APIZRel_Boundary,			"Zone Boundary"},
	{ APIZRel_ReduceArea,		"Reduce Zone Area Only"},
	{ APIZRel_None,				"No Effect on Zones"}
};


const GS::HashTable<Int32, GS::UniString> beamFlagNames
{
	{ APIWBeam_RefMater,		"Override with Outside Face Surface"},
	{ APIWBeam_OppMater,		"Override with Inside Face Surface"},
	{ APIWBeam_HalfLog,			"Start with Half Log"},
	{ APIWBeam_QuadricLog,		"Square Logs"},
	{ APIWBeam_Stretched,		"Rounded on Both Sides"},
	{ APIWBeam_RightLog,		"Rounded on Outside Face"},
	{ APIWBeam_LeftLog,			"Rounded on Inside Face"}
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
	{APIAssemblySegment_Fixed, 			"Fixed"},
	{APIAssemblySegment_Proportional, 	"Proportional"},
};


const GS::HashTable<API_AssemblySegmentCutTypeID, GS::UniString> assemblySegmentCutTypeNames
{
	{APIAssemblySegmentCut_Horizontal, 	"Horizontal"},
	{APIAssemblySegmentCut_Vertical, 	"Vertical"},
	{APIAssemblySegmentCut_Custom, 		"Custom"}
};


const GS::HashTable<API_BHoleTypeID, GS::UniString> beamHoleTypeNames
{
	{APIBHole_Rectangular, 	"Rectangular" },
	{APIBHole_Circular, 	"Circular"}
};


const GS::HashTable<API_BeamVisibleLinesID, GS::UniString> beamVisibleLinesNames
{
	{APIBeamLineShowAlways, 	"Show Always" },
	{APIBeamLineHideAlways, 	"Hide Always"},
	{APIBeamLineByMVO,			"by MVO"}
};


const GS::HashTable<API_BeamShapeTypeID, GS::UniString> beamShapeTypeNames
{
	{ API_StraightBeam, 			"Straight" },
	{ API_HorizontallyCurvedBeam, 	"HorizontallyCurved"},
	{ API_VerticallyCurvedBeam, 	"VerticallyCurved"}
};


const GS::HashTable<API_WindowDoorDirectionTypes, GS::UniString> windowDoorDirectionTypeNames
{
	{ API_WDAssociativeToWall, 	"AssociativeToWall" },
	{ API_WDVertical, 			"Vertical"},
};


const GS::HashTable<API_VeneerTypeID, GS::UniString> venTypeNames
{
	{ APIVeneer_Core,		"Core"},
	{ APIVeneer_Finish,		"Finish"},
	{ APIVeneer_Other,		"Other"}
};


const GS::HashTable<short, GS::UniString> coreSymbolTypeNames
{
	{ 1,		"Plain"},
	{ 2,		"Slash"},
	{ 3,		"X"},
	{ 4,		"CrossHair"}
};
