#ifndef RESOURCE_STRINGS_HPP
#define RESOURCE_STRINGS_HPP

#include "UniString.hpp"


namespace ResourceStrings {

enum class ElementTypeStringItems {
	WallString = 1,
	ColumnString,
	BeamString,
	WindowString,
	DoorString,
	ObjectString,
	LampString,
	SlabString,
	RoofString,
	MeshString,
	DimensionString,
	RadialDimensionString,
	LevelDimensionString,
	AngleDimensionString,
	TextString,
	LabelString,
	ZoneString,
	HatchString,
	LineString,
	PolyLineString,
	ArcString,
	CircleString,
	SplineString,
	HotspotString,
	CutPlaneString,
	CameraString,
	CamSetString,
	GroupString,
	SectElemString,
	DrawingString,
	PictureString,
	DetailString,
	ElevationString,
	InteriorElevationString,
	WorksheetString,
	HotlinkString,
	CurtainWallString,
	CurtainWallSegmentString,
	CurtainWallFrameString,
	CurtainWallPanelString,
	CurtainWallJunctionString,
	CurtainWallAccessoryString,
	ShellString,
	SkylightString,
	MorphString,
	ChangeMarkerString,
	StairString,
	RiserString,
	TreadString,
	StairStructureString,
	RailingString,
	RailingToprailString,
	RailingHandrailString,
	RailingRailString,
	RailingPostString,
	RailingInnerPostString,
	RailingBalusterString,
	RailingPanelString,
	RailingSegmentString,
	RailingNodeString,
	RailingBalusterSetString,
	RailingPatternString,
	RailingToprailEndString,
	RailingHandrailEndString,
	RailingRailEndString,
	RailingToprailConnectionString,
	RailingHandrailConnectionString,
	RailingRailConnectionString,
	RailingEndFinishString,
	AnalyticalSupportString,
	AnalyticalLinkString,
	BeamSegmentString,
	ColumnSegmentString,
	OpeningString
};

const GS::UniString& GetElementTypeStringFromResource (const ElementTypeStringItems& resourceItemId);
const GS::UniString& GetFixElementTypeStringFromResource (const ElementTypeStringItems& resourceItemId);

}

#endif
