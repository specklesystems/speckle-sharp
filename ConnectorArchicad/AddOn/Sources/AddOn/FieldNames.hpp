#ifndef FIELD_NAMES_HPP
#define FIELD_NAMES_HPP

namespace FieldNames
{

static const char* ApplicationId = "applicationId";
static const char* ApplicationIds = "applicationIds";
static const char* ParentElementId = "parentApplicationId";
static const char* ElementFilter = "elementFilter";
static const char* ElementType = "elementType";
static const char* ElementTypes = "elementTypes";
static const char* Elements = "elements";
static const char* SubElements = "subElements";

static const char* FloorIndex = "floorIndex";
static const char* Shape = "shape";

static const char* Walls = "walls";
static const char* Windows = "windows";
static const char* SubelementModels = "subelementModels";
static const char* Doors = "doors";
static const char* Beams = "beams";
static const char* Columns = "columns";
static const char* Objects = "objects";
static const char* Slabs = "slabs";
static const char* Zones = "zones";
static const char* Roofs = "roofs";
static const char* Models = "models";

static const char* ShowOnStories = "showOnStories";
static const char* VisibilityContData = "visibilityCont";
static const char* VisibilityFillData = "visibilityFill";
static const char* ShowOnHome = "showOnHome";
static const char* ShowAllAbove = "showAllAbove";
static const char* ShowAllBelow = "showAllBelow";
static const char* ShowRelAbove = "showRelAbove";
static const char* ShowRelBelow = "showRelBelow";
static const char* HomeStoryOnlyValueName = "homeStoryOnly";
static const char* HomeAndOneStoryUpValueName = "homeAndOneStoryUp";
static const char* HomeAndOneStoryDownValueName = "homeAndOneStoryDown";
static const char* HomeAndOneStoryUpAndDownValueName = "homeAndOneStoryUpAndDown";
static const char* OneStoryUpValueName = "oneStoryUp";
static const char* OneStoryDownValueName = "oneStoryDown";
static const char* AllStoriesValueName = "allStories";
static const char* AllRelevantStoriesValueName = "allRelevantStories";
static const char* CustomStoriesValueName = "custom";

static const char* CoverFillTransformationType = "coverFillTransformationType";
static const char* HatchOrientationType = "coverFillTransformationType";
static const char* LinkToProjectOriginValueName = "linkToProjectOrigin";
static const char* LinkToFillOriginValueName = "linkToFillOrigin";
static const char* CustomDistortionValueName = "customDistortion";
static const char* ThreeDDistortionValueName = "3DDistortion";


namespace AssemblySegment {
static const char* SegmentName = "Segment #%d";
static const char* SchemeName = "Scheme #%d";
static const char* CutName = "Cut #%d";

static const char* HoleData = "Holes";
static const char* SegmentData = "Segments";
static const char* SchemeData = "Schemes";
static const char* CutData = "Cuts";
}


namespace Wall
{
// Wall geometry
static const char* BaseOffset = "baseOffset";
static const char* StartPoint = "startPoint";
static const char* EndPoint = "endPoint";
static const char* WallComplexity = "wallComplexity";
static const char* Structure = "structure";
static const char* GeometryMethod = "geometryMethod";
static const char* BuildingMaterialName = "buildingMaterialName";
static const char* CompositeName = "compositeName";
static const char* ProfileName = "profileName";
static const char* ArcAngle = "arcAngle";
static const char* Thickness = "thickness";
static const char* FirstThickness = "firstThickness";
static const char* SecondThickness = "secondThickness";
static const char* OutsideSlantAngle = "outsideSlantAngle";
static const char* InsideSlantAngle = "insideSlantAngle";
static const char* Height = "height";
static const char* PolyCanChange = "polyWalllCornersCanChange";
// Wall and stories relation
static const char* TopOffset = "topOffset";
static const char* RelativeTopStoryIndex = "relativeTopStory";
static const char* ReferenceLineLocation = "referenceLineLocation";
static const char* ReferenceLineOffset = "referenceLineOffset";
static const char* OffsetFromOutside = "offsetFromOutside";
static const char* ReferenceLineStartIndex = "referenceLineStartIndex";
static const char* ReferenceLineEndIndex = "referenceLineEndIndex";
static const char* Flipped = "flipped";
// Floor Plan and Section - Floor Plan Display
static const char* DisplayOptionName = "displayOptionName";
static const char* ViewDepthLimitationName = "showProjectionName";
// Floor Plan and Section - Cut Surfaces
static const char* CutLinePenIndex = "cutLinePen";
static const char* CutLinetypeName = "cutLinetype";
static const char* OverrideCutFillPenIndex = "overrideCutFillPen";
static const char* OverrideCutFillBackgroundPenIndex = "overrideCutFillBackgroundPen";
// Floor Plan and Section - Outlines
static const char* UncutLinePenIndex = "uncutLinePen";
static const char* UncutLinetypeName = "uncutLinetype";
static const char* OverheadLinePenIndex = "overheadLinePen";
static const char* OverheadLinetypeName = "overheadLinetype";
// Model - Override Surfaces
static const char* ReferenceMaterialName = "referenceMaterialName";
static const char* ReferenceMaterialStartIndex = "referenceMaterialStartIndex";
static const char* ReferenceMaterialEndIndex = "referenceMaterialEndIndex";
static const char* OppositeMaterialName = "oppositeMaterialName";
static const char* OppositeMaterialStartIndex = "oppositeMaterialStartIndex";
static const char* OppositeMaterialEndIndex = "oppositeMaterialEndIndex";
static const char* SideMaterialName = "sideMaterialName";
static const char* MaterialsChained = "materialsChained";
static const char* InheritEndSurface = "inheritEndSurface";
static const char* AlignTexture = "alignTexture";
static const char* Sequence = "sequence";
// Model - Log Details (log height, start with half log, surface of horizontal edges, log shape)
static const char* LogHeight = "logHeight";
static const char* StartWithHalfLog = "startWithHalfLog";
static const char* SurfaceOfHorizontalEdges = "surfaceOfHorizontalEdges";
static const char* LogShape = "logShape";
// Model - Defines the relation of wall to zones (Zone Boundary, Reduce Zone Area Only, No Effect on Zones)
static const char* WallRelationToZoneName = "wallRelationToZoneName";
// Does it have any embedded object?
static const char* HasDoor = "hasDoor";
static const char* HasWindow = "hasWindow";
}


namespace OpeningBase {
static const char* Door = "Door #%d";
//
static const char* guid = "guid";
// OpeningBase
static const char* width = "width";
static const char* height = "height";
static const char* subFloorThickness = "subFloorThickness";
static const char* reflected = "reflected";
static const char* oSide = "oSide";
static const char* refSide = "refSide";

static const char* buildingMaterial = "buildingMaterial";
static const char* libraryPart = "libraryPart";

static const char* revealDepthFromSide = "revealDepthFromSide";
static const char* jambDepthHead = "jambDepthHead";
static const char* jambDepth = "jambDepth";
static const char* jambDepth2 = "jambDepth2";
static const char* objLoc = "objLoc";
static const char* lower = "lower";
static const char* directionType = "directionType";

static const char* startPoint = "startPoint";
static const char* dirVector = "dirVector";
}


namespace Beam
{
// Positioning
static const char* begC = "begC";
static const char* endC = "endC";
static const char* level = "level";
static const char* isSlanted = "isSlanted";
static const char* slantAngle = "slantAngle";
static const char* beamShape = "beamShape";
static const char* sequence = "sequence";
static const char* curveAngle = "curveAngle";
static const char* verticalCurveHeight = "verticalCurveHeight";
static const char* isFlipped = "isFlipped";
// End Cuts
static const char* nCuts = "nCuts";
// Rreference Axis
static const char* anchorPoint = "anchorPoint";
static const char* offset = "offset";
static const char* profileAngle = "profileAngle";
// Segment
static const char* segments = "segments";
static const char* nSegments = "nSegments";
static const char* nProfiles = "nProfiles";


namespace BeamSegment
{
	// Segment override materials
	static const char* LeftMaterial = "leftMaterial";
	static const char* TopMaterial = "topMaterial";
	static const char* RightMaterial = "rightMaterial";
	static const char* BottomMaterial = "bottomMaterial";
	static const char* EndsMaterial = "endsMaterial";
	// Segment - The overridden materials are chained
	static const char* MaterialsChained = "materialChained";
	// Segment
	static const char* segmentData = "assemblySegmentData";
}


// Scheme
static const char* nSchemes = "nSchemes";
// Hole
static const char* HoleName = "Hole #%d";
static const char* holeType = "holeType";
static const char* holeContourOn = "holeContourOn";
static const char* holeId = "holeId";
static const char* centerx = "centerx";
static const char* centerz = "centerz";
static const char* width = "width";
static const char* height = "height";
// Floor Plan and Section - Floor Plan Display
static const char* DisplayOptionName = "displayOptionName";
static const char* UncutProjectionModeName = "uncutProjectionMode";
static const char* OverheadProjectionModeName = "overheadProjectionMode";
static const char* ViewDepthLimitationName = "showProjectionName";
// Floor Plan and Section - Cut Surfaces
static const char* cutContourLinePen = "cutContourLinePen";
static const char* CutContourLinetypeName = "cutContourLineType";
static const char* OverrideCutFillPenIndex = "overrideCutFillPen";
static const char* OverrideCutFillBackgroundPenIndex = "overrideCutFillBackgroundPen";
// Floor Plan and Section - Outlines
static const char* ShowOutlineIndex = "showOutline";
static const char* UncutLinePenIndex = "uncutLinePen";
static const char* UncutLinetypeName = "uncutLinetype";
static const char* OverheadLinePenIndex = "overheadLinePen";
static const char* OverheadLinetypeName = "overheadLinetype";
static const char* HiddenLinePenIndex = "hiddenLinePen";
static const char* HiddenLinetypeName = "hiddenLinetype";
// Floor Plan and Section - Symbol
static const char* ShowReferenceAxisIndex = "showReferenceAxis";
static const char* refPen = "referencePen";
static const char* refLtype = "referenceLinetype";
// Floor Plan and Section - Cover Fills
static const char* useCoverFill = "useCoverFill";
static const char* useCoverFillFromSurface = "useCoverFillFromSurface";
static const char* coverFillForegroundPen = "coverFillForegroundPen";
static const char* coverFillBackgroundPen = "coverFillBackgroundPen";
static const char* coverFillType = "coverFillType";
static const char* CoverFillTransformationOrigoX = "coverFillTransformationOrigoX";
static const char* CoverFillTransformationOrigoY = "coverFillTransformationOrigoY";
static const char* CoverFillTransformationXAxisX = "coverFillTransformationXAxisX";
static const char* CoverFillTransformationXAxisY = "coverFillTransformationXAxisY";
static const char* CoverFillTransformationYAxisX = "coverFillTransformationYAxisX";
static const char* CoverFillTransformationYAxisY = "coverFillTransformationYAxisY";
}


namespace Column
{
// Positioning - geometry
static const char* origoPos = "origoPos";
static const char* height = "height";
// Positioning - story relation
static const char* bottomOffset = "bottomOffset";
static const char* topOffset = "topOffset";
static const char* relativeTopStory = "relativeTopStory";
// Positioning - slanted column
static const char* isSlanted = "isSlanted";
static const char* slantAngle = "slantAngle";
static const char* slantDirectionAngle = "slantDirectionAngle";
static const char* isFlipped = "isFlipped";
// Positioning - wrapping
static const char* Wrapping = "wrapping";
// Positioning - Defines the relation of column to zones (Zone Boundary, Reduce Zone Area Only, No Effect on Zones)
static const char* ColumnRelationToZoneName = "columnRelationToZoneName";
// End Cuts
static const char* nCuts = "nCuts";
// Reference Axis
static const char* coreAnchor = "coreAnchor";
static const char* axisRotationAngle = "axisRotationAngle";
// Segment
static const char* segments = "segments";
static const char* nSegments = "nSegments";
static const char* nProfiles = "nProfiles";


namespace ColumnSegment
{
	// Segment - Veneer attributes
	static const char* VenType = "veneerType";
	static const char* VenBuildingMaterial = "veneerBuildingMaterial";
	static const char* VenThick = "veneerThick";
	// Segment - The extrusion overridden material name
	static const char* ExtrusionSurfaceMaterial = "extrusionSurfaceMaterial";
	// Segment - The ends overridden material name
	static const char* EndsSurfaceMaterial = "endsSurfaceMaterial";
	// Segment - The overridden materials are chained
	static const char* MaterialsChained = "materialChained";
	// Segment
	static const char* segmentData = "assemblySegmentData";
}


// Scheme
static const char* nSchemes = "nSchemes";
// Floor Plan and Section - Floor Plan Display
static const char* DisplayOptionName = "displayOptionName";
static const char* ViewDepthLimitationName = "showProjectionName";
// Floor Plan and Section - Cut Surfaces
static const char* corePen = "corePen";
static const char* CoreLinetypeName = "contLtype";
static const char* VeneerPenIndex = "venLinePen";
static const char* VeneerLinetypeName = "venLineType";
static const char* OverrideCutFillPenIndex = "overrideCutFillPen";
static const char* OverrideCutFillBackgroundPenIndex = "overrideCutFillBackgroundPen";
// Floor Plan and Section - Outlines
static const char* UncutLinePenIndex = "uncutLinePen";
static const char* UncutLinetypeName = "uncutLinetype";
static const char* OverheadLinePenIndex = "overheadLinePen";
static const char* OverheadLinetypeName = "overheadLinetype";
static const char* HiddenLinePenIndex = "hiddenLinePen";
static const char* HiddenLinetypeName = "hiddenLinetype";
// Floor Plan and Section - Floor Plan Symbol
static const char* CoreSymbolTypeName = "coreSymbolTypeName";
static const char* coreSymbolPar1 = "coreSymbolPar1";
static const char* coreSymbolPar2 = "coreSymbolPar2";
static const char* CoreSymbolPenIndex = "coreSymbolPen";
// Floor Plan and Section - Cover Fills
static const char* useCoverFill = "useCoverFill";
static const char* useCoverFillFromSurface = "useCoverFillFromSurface";
static const char* coverFillForegroundPen = "coverFillForegroundPen";
static const char* coverFillBackgroundPen = "coverFillBackgroundPen";
static const char* coverFillType = "coverFillType";
static const char* CoverFillTransformationOrigoX = "coverFillTransformationOrigoX";
static const char* CoverFillTransformationOrigoY = "coverFillTransformationOrigoY";
static const char* CoverFillTransformationXAxisX = "coverFillTransformationXAxisX";
static const char* CoverFillTransformationXAxisY = "coverFillTransformationXAxisY";
static const char* CoverFillTransformationYAxisX = "coverFillTransformationYAxisX";
static const char* CoverFillTransformationYAxisY = "coverFillTransformationYAxisY";
}


namespace AssemblySegmentData {
static const char* circleBased = "circleBased";
static const char* modelElemStructureType = "modelElemStructureType";
static const char* nominalHeight = "nominalHeight";
static const char* nominalWidth = "nominalWidth";
static const char* isWidthAndHeightLinked = "isWidthAndHeightLinked";
static const char* isHomogeneous = "isHomogeneous";
static const char* endWidth = "endWidth";
static const char* endHeight = "endHeight";
static const char* isEndWidthAndHeightLinked = "isEndWidthAndHeightLinked";
static const char* buildingMaterial = "buildingMaterial";
static const char* profileAttrName = "profileAttrName";
}


namespace AssemblySegmentSchemeData {
static const char* lengthType = "lengthType";
static const char* fixedLength = "fixedLength";
static const char* lengthProportion = "lengthProportion";
}


namespace AssemblySegmentCutData {
static const char* cutType = "cutType";
static const char* customAngle = "customAngle";
}


namespace Object
{
// Main
static const char* pos = "pos";
}


namespace Slab
{
// Geometry and positioning
static const char* Thickness = "thickness";
static const char* Structure = "structure";
static const char* CompositeName = "compositeName";
static const char* BuildingMaterialName = "buildingMaterialName";
static const char* ReferencePlaneLocation = "referencePlaneLocation";
// Edge trims
static const char* EdgeAngleType = "edgeAngleType";
static const char* EdgeAngle = "edgeAngle";
// Floor Plan and Section - Cut Surfaces
static const char* sectContPen = "sectContPen";
static const char* sectContLtype = "sectContLtype";
static const char* cutFillPen = "cutFillPen";
static const char* cutFillBackgroundPen = "cutFillBackgroundPen";
// Floor Plan and Section - Outlines
static const char* contourPen = "contourPen";
static const char* contourLineType = "contourLineType";
static const char* hiddenContourLinePen = "hiddenContourLinePen";
static const char* hiddenContourLineType = "hiddenContourLineType";
// Floor Plan and Section - Cover Fills
static const char* useFloorFill = "useFloorFill";
static const char* floorFillPen = "floorFillPen";
static const char* floorFillBGPen = "floorFillBGPen";
static const char* floorFillName = "floorFillName";
static const char* use3DHatching = "use3DHatching";
static const char* hatchOrientation = "hatchOrientation";
static const char* hatchOrientationOrigoX = "hatchOrientationOrigoX";
static const char* hatchOrientationOrigoY = "hatchOrientationOrigoY";
static const char* hatchOrientationXAxisX = "hatchOrientationXAxisX";
static const char* hatchOrientationXAxisY = "hatchOrientationXAxisY";
static const char* hatchOrientationYAxisX = "hatchOrientationYAxisX";
static const char* hatchOrientationYAxisY = "hatchOrientationYAxisY";
// Model
static const char* topMat = "topMat";
static const char* sideMat = "sideMat";
static const char* botMat = "botMat";
static const char* materialsChained = "materialsChained";
}


namespace Room
{
static const char* Name = "name";
static const char* Number = "number";
static const char* BasePoint = "basePoint";
static const char* Height = "height";
static const char* Area = "area";
static const char* Volume = "volume";
}


namespace Model
{
static const char* Vertices = "vertices";
static const char* VertexX = "x";
static const char* VertexY = "y";
static const char* VertexZ = "z";
static const char* Polygons = "polygons";
static const char* Materials = "materials";
static const char* PointIds = "pointIds";
static const char* MaterialName = "name";
static const char* Transparency = "transparency";
static const char* AmbientColor = "ambientColor";
static const char* EmissionColor = "emissionColor";
static const char* Material = "material";
static const char* Model = "model";
static const char* Ids = "ids";
static const char* Edges = "edges";
}


}


#endif
