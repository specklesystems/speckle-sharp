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
static const char* SlabsFieldName = "slabs";
static const char* ZonesFieldName = "zones";
static const char* RoofsFieldName = "roofs";
static const char* ModelsFieldName = "models";


namespace Wall
{
static const char* StartPointFieldName = "startPoint";
static const char* EndPointFieldName = "endPoint";
static const char* HeightFieldName = "height";
static const char* ThicknessFieldName = "thickness";
static const char* FirstThicknessFieldName = "firstThickness";
static const char* SecondThicknessFieldName = "secondThickness";
static const char* ArcAngleFieldName = "arcAngle";
static const char* StructureFieldName = "structure";
static const char* GeometryMethodFieldName = "geometryMethod";
static const char* WallComplexityFieldName = "wallComplexity";
static const char* OutsideSlantAngleFieldName = "outsideSlantAngle";
static const char* InsideSlantAngleFieldName = "insideSlantAngle";
static const char* CompositeIndexFieldName = "compositeIndex";
static const char* BuildingMaterialIndexFieldName = "buildingMaterialIndex";
static const char* ProfileIndexFieldName = "profileIndex";
static const char* BaseOffsetFieldName = "baseOffset";
static const char* TopOffsetFieldName = "topOffset";
static const char* FlippedFieldName = "flipped";
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
// Naming
static const char* BeamSegmentName = "Segment #%d";
static const char* SchemeName = "Scheme #%d";
static const char* CutName = "Cut #%d";
static const char* HoleName = "Hole #%d";
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
// From API_BeamSegmentType
static const char* segmentData = "Segments";
static const char* circleBased = "circleBased";
static const char* profileAttrName = "profileAttrName";
static const char* buildingMaterial = "buildingMaterial";
static const char* nominalWidth = "nominalWidth";
static const char* nominalHeight = "nominalHeight";
static const char* isHomogeneous = "isHomogeneous";
static const char* endWidth = "endWidth";
static const char* endHeight = "endHeight";
static const char* isEndWidthAndHeightLinked = "isEndWidthAndHeightLinked";
static const char* isWidthAndHeightLinked = "isWidthAndHeightLinked";
// From API_AssemblySegmentSchemeData
static const char* schemeData = "Schemes";
static const char* lengthType = "lengthType";
static const char* fixedLength = "fixedLength";
static const char* lengthProportion = "lengthProportion";
// From API_AssemblySegmentCutData
static const char* cutData = "Cuts";
static const char* cutType = "cutType";
static const char* customAngle = "customAngle";
// From API_Beam_Hole
static const char* holeData = "Holes";
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
static const char* ColumnSegmentName = "Segment #%d";
static const char* SchemeName = "Scheme #%d";
static const char* CutName = "Cut #%d";

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
static const char* profileAttrName = "profileAttrName";
static const char* segmentData = "Segments";
static const char* schemeData = "Schemes";
static const char* cutData = "Cuts";
static const char* cutType = "cutType";
static const char* customAngle = "customAngle";
static const char* buildingMaterial = "buildingMaterial";

static const char* circleBased = "circleBased";
static const char* modelElemStructureType = "modelElemStructureType";
static const char* nominalHeight = "nominalHeight";
static const char* nominalWidth = "nominalWidth";
static const char* isWidthAndHeightLinked = "isWidthAndHeightLinked";
static const char* isHomogeneous = "isHomogeneous";
static const char* endWidth = "endWidth";
static const char* endHeight = "endHeight";
static const char* isEndWidthAndHeightLinked = "isEndWidthAndHeightLinked";

static const char* lengthType = "lengthType";
static const char* fixedLength = "fixedLength";
static const char* lengthProportion = "lengthProportion";

static const char* corePen = "corePen";
static const char* coreAnchor = "coreAnchor";
static const char* bottomOffset = "bottomOffset";
static const char* topOffset = "topOffset";
static const char* coreSymbolPar1 = "coreSymbolPar1";
static const char* coreSymbolPar2 = "coreSymbolPar2";
static const char* slantDirectionAngle = "slantDirectionAngle";
static const char* axisRotationAngle = "axisRotationAngle";
static const char* relativeTopStory = "relativeTopStory";
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
}


#endif
