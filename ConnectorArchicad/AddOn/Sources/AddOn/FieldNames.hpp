#ifndef FIELD_NAMES_HPP
#define FIELD_NAMES_HPP

static const char* ApplicationIdFieldName = "applicationId";
static const char* ApplicationIdsFieldName = "applicationIds";
static const char* ParentElementIdFieldName = "parentApplicationId";
static const char* ElementFilterFieldName = "elementFilter";
static const char* ElementTypeFieldName = "elementType";
static const char* ElementTypesFieldName = "elementTypes";
static const char* ElementsFieldName = "elements";
static const char* SubElementsFieldName = "subElements";

static const char* FloorIndexFieldName = "floorIndex";
static const char* ShapeFieldName = "shape";

static const char* WallsFieldName = "walls";
static const char* WindowsFieldName = "windows";
static const char* SubelementModelsFieldName = "subelementModels";
static const char* DoorsFieldName = "doors";
static const char* BeamsFieldName = "beams";
static const char* ColumnsFieldName = "columns";
static const char* ObjectsFieldName = "objects";
static const char* SlabsFieldName = "slabs";
static const char* ZonesFieldName = "zones";
static const char* RoofsFieldName = "roofs";
static const char* ModelsFieldName = "models";

static const char* ShowOnStoriesFieldName = "showOnStories";
static const char* HomeStoryOnlyValueName = "homeStoryOnly";
static const char* HomeAndOneStoryUpValueName = "homeAndOneStoryUp";
static const char* HomeAndOneStoryDownValueName = "homeAndOneStoryDown";
static const char* HomeAndOneStoryUpAndDownValueName = "homeAndOneStoryUpAndDown";
static const char* OneStoryUpValueName = "oneStoryUp";
static const char* OneStoryDownValueName = "oneStoryDown";
static const char* AllStoriesValueName = "allStories";
static const char* AllRelevantStoriesValueName = "allRelevantStories";

namespace PartialObjects {
static const char* HoleData = "Holes";
static const char* SegmentData = "Segments";
static const char* SchemeData = "Schemes";
static const char* CutData = "Cuts";
}

namespace Wall
{
// Wall geometry
static const char* BaseOffsetFieldName = "baseOffset";
static const char* StartPointFieldName = "startPoint";
static const char* EndPointFieldName = "endPoint";
static const char* WallComplexityFieldName = "wallComplexity";
static const char* StructureFieldName = "structure";
static const char* GeometryMethodFieldName = "geometryMethod";
static const char* BuildingMaterialNameFieldName = "buildingMaterialName";
static const char* CompositeNameFieldName = "compositeName";
static const char* ProfileNameFieldName = "profileName";
static const char* ArcAngleFieldName = "arcAngle";
static const char* ThicknessFieldName = "thickness";
static const char* FirstThicknessFieldName = "firstThickness";
static const char* SecondThicknessFieldName = "secondThickness";
static const char* OutsideSlantAngleFieldName = "outsideSlantAngle";
static const char* InsideSlantAngleFieldName = "insideSlantAngle";
static const char* HeightFieldName = "height";
static const char* PolyCanChangeFieldName = "polyWalllCornersCanChange";
// Wall and stories relation
static const char* TopOffsetFieldName = "topOffset";
static const char* RelativeTopStoryIndexFieldName = "relativeTopStory";
static const char* ReferenceLineLocationFieldName = "referenceLineLocation";
static const char* ReferenceLineOffsetFieldName = "referenceLineOffset";
static const char* OffsetFromOutsideFieldName = "offsetFromOutside";
static const char* ReferenceLineStartIndexFieldName = "referenceLineStartIndex";
static const char* ReferenceLineEndIndexFieldName = "referenceLineEndIndex";
static const char* FlippedFieldName = "flipped";
// Floor Plan and Section - Floor Plan Display
static const char* DisplayOptionNameFieldName = "displayOptionName";
static const char* ViewDepthLimitationNameFieldName = "showProjectionName";
// Floor Plan and Section - Cut Surfaces parameters
static const char* CutLinePenIndexFieldName = "cutLinePen";
static const char* CutLinetypeNameFieldName = "cutLinetype";
static const char* OverrideCutFillPenIndexFieldName = "overrideCutFillPen";
static const char* OverrideCutFillBackgroundPenIndexFieldName = "overrideCutFillBackgroundPen";
// Floor Plan and Section - Outlines parameters
static const char* UncutLinePenIndexFieldName = "uncutLinePen";
static const char* UncutLinetypeNameFieldName = "uncutLinetype";
static const char* OverheadLinePenIndexFieldName = "overheadLinePen";
static const char* OverheadLinetypeNameFieldName = "overheadLinetype";
// Model - Override Surfaces
static const char* ReferenceMaterialNameFieldName = "referenceMaterialName";
static const char* ReferenceMaterialStartIndexFieldName = "referenceMaterialStartIndex";
static const char* ReferenceMaterialEndIndexFieldName = "referenceMaterialEndIndex";
static const char* OppositeMaterialNameFieldName = "oppositeMaterialName";
static const char* OppositeMaterialStartIndexFieldName = "oppositeMaterialStartIndex";
static const char* OppositeMaterialEndIndexFieldName = "oppositeMaterialEndIndex";
static const char* SideMaterialNameFieldName = "sideMaterialName";
static const char* MaterialsChainedFieldName = "materialsChained";
static const char* InheritEndSurfaceFieldName = "inheritEndSurface";
static const char* AlignTextureFieldName = "alignTexture";
static const char* SequenceFieldName = "sequence";
// Model - Log Details (log height, start with half log, surface of horizontal edges, log shape)
static const char* LogHeightFieldName = "logHeight";
static const char* StartWithHalfLogFieldName = "startWithHalfLog";
static const char* SurfaceOfHorizontalEdgesFieldName = "surfaceOfHorizontalEdges";
static const char* LogShapeFieldName = "logShape";
// Model - Defines the relation of wall to zones (Zone Boundary, Reduce Zone Area Only, No Effect on Zones)
static const char* WallRelationToZoneNameFieldName = "wallRelationToZoneName";
// Does it have any embedded object?
static const char* HasDoorFieldName = "hasDoor";
static const char* HasWindowFieldName = "hasWindow";
}


namespace OpeningBase {
static const char* DoorFieldName = "Door #%d";
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
// Main
static const char* begC = "begC";
static const char* endC = "endC";
static const char* aboveViewLinePen = "aboveViewLinePen";
static const char* refPen = "refPen";
static const char* cutContourLinePen = "cutContourLinePen";
static const char* sequence = "sequence";
static const char* isAutoOnStoryVisibility = "isAutoOnStoryVisibility";
static const char* offset = "offset";
static const char* level = "level";
static const char* curveAngle = "curveAngle";
static const char* verticalCurveHeight = "verticalCurveHeight";
static const char* beamShape = "beamShape";
static const char* hiddenLinePen = "hiddenLinePen";
static const char* anchorPoint = "anchorPoint";
static const char* belowViewLinePen = "belowViewLinePen";
static const char* isFlipped = "isFlipped";
static const char* isSlanted = "isSlanted";
static const char* slantAngle = "slantAngle";
static const char* profileAngle = "profileAngle";
static const char* nSegments = "nSegments";
static const char* nCuts = "nCuts";
static const char* nSchemes = "nSchemes";
static const char* nProfiles = "nProfiles";
static const char* useCoverFill = "useCoverFill";
static const char* useCoverFillFromSurface = "useCoverFillFromSurface";
static const char* coverFillOrientationComesFrom3D = "coverFillOrientationComesFrom3D";
static const char* coverFillForegroundPen = "coverFillForegroundPen";
static const char* coverFillBackgroundPen = "coverFillBackgroundPen";
static const char* modelElemStructureType = "modelElemStructureType";
static const char* HoleName = "Hole #%d";
static const char* holeType = "holeType";
static const char* holeContourOn = "holeContourOn";
static const char* holeId = "holeId";
static const char* centerx = "centerx";
static const char* centerz = "centerz";
static const char* width = "width";
static const char* height = "height";
}

namespace Column
{
static const char* origoPos = "origoPos";
static const char* height = "height";
static const char* aboveViewLinePen = "aboveViewLinePen";
static const char* isAutoOnStoryVisibility = "isAutoOnStoryVisibility";
static const char* hiddenLinePen = "hiddenLinePen";
static const char* belowViewLinePen = "belowViewLinePen";
static const char* isFlipped = "isFlipped";
static const char* isSlanted = "isSlanted";
static const char* slantAngle = "slantAngle";
static const char* nSegments = "nSegments";
static const char* nCuts = "nCuts";
static const char* nSchemes = "nSchemes";
static const char* nProfiles = "nProfiles";
static const char* useCoverFill = "useCoverFill";
static const char* useCoverFillFromSurface = "useCoverFillFromSurface";
static const char* coverFillOrientationComesFrom3D = "coverFillOrientationComesFrom3D";
static const char* coverFillForegroundPen = "coverFillForegroundPen";
static const char* corePen = "corePen";
static const char* coreAnchor = "coreAnchor";
static const char* bottomOffset = "bottomOffset";
static const char* topOffset = "topOffset";
static const char* coreSymbolPar1 = "coreSymbolPar1";
static const char* coreSymbolPar2 = "coreSymbolPar2";
static const char* slantDirectionAngle = "slantDirectionAngle";
static const char* axisRotationAngle = "axisRotationAngle";
static const char* relativeTopStory = "relativeTopStory";

static const char* segmentData = "Segments";
static const char* schemeData = "Schemes";
static const char* cutData = "Cuts";
}

namespace AssemblySegmentData {
static const char* SegmentName = "Segment #%d";

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
static const char* SchemeName = "Scheme #%d";

static const char* lengthType = "lengthType";
static const char* fixedLength = "fixedLength";
static const char* lengthProportion = "lengthProportion";
}

namespace AssemblySegmentCutData {
static const char* CutName = "Cut #%d";

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
static const char* StructureFieldName = "structure";
static const char* ThicknessFieldName = "thickness";
static const char* EdgeAngleTypeFieldName = "edgeAngleType";
static const char* EdgeAngleFieldName = "edgeAngle";
static const char* ReferencePlaneLocationFieldName = "referencePlaneLocation";
static const char* CompositeIndexFieldName = "compositeIndex";
static const char* BuildingMaterialIndexFieldName = "buildingMaterialIndex";
}


namespace Room
{
static const char* NameFieldName = "name";
static const char* NumberFieldName = "number";
static const char* BasePointFieldName = "basePoint";
static const char* HeightFieldName = "height";
static const char* AreaFieldName = "area";
static const char* VolumeFieldName = "volume";
}


namespace Model
{
static const char* VerticesFieldName = "vertices";
static const char* VertexXFieldName = "x";
static const char* VertexYFieldName = "y";
static const char* VertexZFieldName = "z";
static const char* PolygonsFieldName = "polygons";
static const char* MaterialsFieldName = "materials";
static const char* PointIdsFieldName = "pointIds";
static const char* MaterialNameFieldName = "name";
static const char* TransparencyFieldName = "transparency";
static const char* AmbientColorFieldName = "ambientColor";
static const char* EmissionColorFieldName = "emissionColor";
static const char* MaterialFieldName = "material";
static const char* ModelFieldName = "model";
static const char* IdsFieldName = "ids";
static const char* EdgesFieldName = "edges";
}


#endif
