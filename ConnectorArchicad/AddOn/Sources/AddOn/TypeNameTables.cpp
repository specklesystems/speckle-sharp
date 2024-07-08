#include "TypeNameTables.hpp"


const GS::HashTable<API_ElemType, GS::UniString> elementNames
{
	{ { API_ZombieElemID, APIVarId_Generic},	"InvalidType"},
	{ { API_WallID, APIVarId_Generic},			"Wall"},
	{ { API_ColumnID, APIVarId_Generic},		"Column"},
	{ { API_BeamID, APIVarId_Generic},			"Beam"},
	{ { API_WindowID, APIVarId_Generic},		"Window"},
	{ { API_DoorID, APIVarId_Generic},			"Door"},
	{ { API_ObjectID, APIVarId_Generic},		"Object"},
	{ { API_LampID, APIVarId_Generic},			"Lamp"},
	{ { API_SlabID, APIVarId_Generic},			"Slab"},
	{ { API_RoofID, APIVarId_Generic},			"Roof"},
	{ { API_MeshID, APIVarId_Generic},			"Mesh"},
	{ { API_ZoneID, APIVarId_Generic},			"Zone"},
	{ { API_CurtainWallID, APIVarId_Generic},	"CurtainWall"},
	{ { API_ShellID, APIVarId_Generic},			"Shell"},
	{ { API_SkylightID, APIVarId_Generic},		"Skylight"},
	{ { API_MorphID, APIVarId_Generic},			"Morph"},
	{ { API_StairID, APIVarId_Generic},			"Stair"},
	{ { API_RailingID, APIVarId_Generic},		"Railing"},
	{ { API_OpeningID, APIVarId_Generic},		"Opening"},
	{ { API_ObjectID, APIVarId_GridElement},	"GridElement"}
};


GSErrCode GetElementTypeName (const API_ElemType& elementType, GS::UniString& elementTypeName)
{
	// check typeID and variationID first
	if (elementNames.ContainsKey(elementType)) {
		elementTypeName = elementNames.Get (elementType);
		return NoError;
	}
	
	// check only typeID
	for (auto elementNameIt : elementNames)
	{
		if (elementNameIt.key->typeID == elementType.typeID) {
			elementTypeName = *(elementNameIt.value);
			return NoError;
		}
	}
	
	return Error;
}


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
#ifdef ServerMainVers_2700
	,
	{ APIZRel_SubtractFromZone,	"Subtract from Zones"}
#endif
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
	{ APIEdgeTrim_Vertical,			"Vertical"},
	{ APIEdgeTrim_Perpendicular,	"Perpendicular"},
	{ APIEdgeTrim_Horizontal,		"Horizontal"},
	{ APIEdgeTrim_CustomAngle,		"Custom Angle"},
	{ APIEdgeTrim_AlignWithCut,		"Align with Cut"}
};


const GS::HashTable<API_SlabReferencePlaneLocationID, GS::UniString> referencePlaneLocationNames
{
	{ APISlabRefPlane_Bottom,		"Bottom"},
	{ APISlabRefPlane_CoreBottom,	"Core Bottom"},
	{ APISlabRefPlane_CoreTop,		"Core Top"},
	{ APISlabRefPlane_Top,			"Top"}
};


const GS::HashTable<API_RoofClassID, GS::UniString> roofClassNames
{
	{ API_PlaneRoofID,	"Single-Plane"},
	{ API_PolyRoofID,	"Multi-Plane"}
};


const GS::HashTable<API_ShellClassID, GS::UniString> shellClassNames
{
	{ API_ExtrudedShellID,	"Extruded"},
	{ API_RevolvedShellID,	"Revolved"},
	{ API_RuledShellID,		"Ruled"}
};


const GS::HashTable<API_ShellBaseCutBodyTypeID, GS::UniString> shellBaseCutBodyTypeNames
{
	{ APIShellBaseCutBody_Editable,	"Editable"},
	{ APIShellBaseCutBody_ContoursDown,	"Contours Down"},
	{ APIShellBaseCutBody_PivotLinesDown,	"Pivot Lines Down"},
	{ APIShellBaseCutBody_UpwardsExtrusion,	"Upwards Extrusion"},
	{ APIShellBaseCutBody_DownwardsExtrusion,	"Downwards Extrusion"}
};


const GS::HashTable<API_ShellBaseContourEdgeTypeID, GS::UniString> shellBaseContourEdgeTypeNames
{
	{ APIShellBaseContour_Undefined,	"Undefined"},
	{ APIShellBaseContour_Ridge,		"Ridge"},
	{ APIShellBaseContour_Valley,		"Valley"},
	{ APIShellBaseContour_Gable,		"Gable"},
	{ APIShellBaseContour_Hip,			"Hip"},
	{ APIShellBaseContour_Eaves,		"Eaves"},
	{ APIShellBaseContour_Peak,			"Peak"},
	{ APIShellBaseContour_SideWall,		"SideWall"},
	{ APIShellBaseContour_EndWall,		"EndWall"},
	{ APIShellBaseContour_RTDome,		"RTDome"},
	{ APIShellBaseContour_RTHollow,		"RTHollow"}
};


const GS::HashTable<API_MorphingRuleID, GS::UniString> morphingRuleNames
{
	{ APIMorphingRule_Paired,	"Paired"},
	{ APIMorphingRule_Smooth,	"Smooth"}
};


const GS::HashTable<API_PolyRoofSegmentAngleTypeID, GS::UniString> polyRoofSegmentAngleTypeNames
{
	{ APIPolyRoof_SegmentAngleTypeSloped,	"Sloped"},
	{ APIPolyRoof_SegmentAngleTypeGable,	"Gable"}
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


const GS::HashTable<API_VerticalLinkID, GS::UniString> verticalLinkTypeNames
{
	{ API_NoLink,						"No Link"},
	{ API_LinkSillToWallBottom,			"Link Sill To Wall Bottom"},
	{ API_LinkSillToStory,				"Link Sill To Story"},
	{ API_LinkHeaderToWallBottom,		"Link Header To Wall Bottom"},
	{ API_LinkHeaderToStory,			"Link Header To Story"},
	{ API_LinkSkylightToRoofPivot,		"Link Skylight To Roof Pivot"},
	{ API_LinkSkylightToStory,			"Link Skylight To Story"},
	{ API_LinkSkylightToShellBase,		"Link Skylight To Shell Base"},
	{ API_LinkSillToWallTop,			"Link Sill To Wall Top"},
	{ API_LinkHeaderToWallTop,			"Link Header To Wall Top"}
};


const GS::HashTable<API_SkylightFixModeID, GS::UniString> skylightFixModeNames
{
	{ APISkylightFixMode_Horizontal,	"Horizontal"},
	{ APISkylightFixMode_Vertical,		"Vertical"}
};


const GS::HashTable<API_SkylightAnchorID, GS::UniString> skylightAnchorNames
{
	{ APISkylightAnchor_BC,		"Bottom center"},
	{ APISkylightAnchor_BL,		"Bottom left"},
	{ APISkylightAnchor_BR,		"Bottom right"},
	{ APISkylightAnchor_TC,		"Top center"},
	{ APISkylightAnchor_TL,		"Top left"},
	{ APISkylightAnchor_TR,		"Top right"}
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


const GS::HashTable<API_OpeningFloorPlanDisplayModeTypeID, GS::UniString> openingFloorPlanDisplayModeNames
{
	{ API_OpeningSymbolic,			"Symbolic"},
	{ API_OpeningSymbolicCut,		"Symbolic Cut"},
	{ API_OpeningSymbolicOverhead,	"Symbolic Overhead"}
};


const GS::HashTable<API_OpeningFloorPlanConnectionModeTypeID, GS::UniString> openingFloorPlanConnectionModeNames
{
	{ API_OpeningDisconnected,		"Disconnected"},
	{ API_OpeningConnected,			"Connected"}
};


const GS::HashTable<API_OpeningFloorPlanOutlinesStyleTypeID, GS::UniString> openingOutlinesStyleNames
{
	{ API_OpeningHideBorder,			"Hide Border"},
	{ API_OpeningShowUncutBorder,		"Show Uncut Border"},
	{ API_OpeningShowOverheadBorder,	"Show Overhead Border"}
};


const GS::HashTable<API_OpeningBasePolygonTypeTypeID, GS::UniString> openingBasePolygonTypeNames
{
	{ API_OpeningBasePolygonRectangular,	"Rectangular"},
	{ API_OpeningBasePolygonCircular,		"Circular"},
	{ API_OpeningBasePolygonCustom,			"Custom"}
};


const GS::HashTable<API_AnchorID, GS::UniString> openingAnchorNames
{
	{ APIAnc_LT,		"Left Top"},
	{ APIAnc_MT,		"Middle Top"},
	{ APIAnc_RT,		"Right Top"},
	{ APIAnc_LM,		"Left Middle"},
	{ APIAnc_MM,		"Middle Middle"},
	{ APIAnc_RM,		"Right Middle"},
	{ APIAnc_LB,		"Left Bottom"},
	{ APIAnc_MB,		"Middle Bottom"},
	{ APIAnc_RB,		"Right Bottom"}
};


const GS::HashTable<API_OpeningLimitTypeTypeID, GS::UniString> openingLimitTypeNames
{
	{ API_OpeningLimitInfinite,		"Infinite"},
	{ API_OpeningLimitFinite,		"Finite"},
	{ API_OpeningLimitHalfInfinite,	"Half Infinite"}
};


const GS::HashTable<API_OpeningLinkedStatusTypeID, GS::UniString> openingLinkedStatusNames
{
	{ API_OpeningLinked,		"Linked"},
	{ API_OpeningNotLinked,		"Not Linked"}
};