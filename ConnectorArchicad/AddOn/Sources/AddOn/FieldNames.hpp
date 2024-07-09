#ifndef FIELD_NAMES_HPP
#define FIELD_NAMES_HPP

namespace FieldNames
{
	
	
	namespace ApplicationObject {
		static const char* ApplicationObjects = "applicationObjects";
		static const char* Status = "Status";
		static const char* StateCreated = "Created";
		static const char* StateSkipped = "Skipped";
		static const char* StateUpdated = "Updated";
		static const char* StateFailed = "Failed";
		static const char* StateRemoved = "Removed";
		static const char* StateUnknown = "Unknown";
		static const char* OriginalId = "OriginalId";
		static const char* CreatedIds = "CreatedIds";
		static const char* Log = "Log";
	}
	
	namespace ElementBase
	{
		static const char* Id = "id";
		static const char* ApplicationId = "applicationId";
		static const char* ApplicationIds = "applicationIds";
		static const char* ParentElementId = "parentApplicationId";
		static const char* ElementFilter = "elementFilter";
		static const char* FilterBy = "filterBy";
		static const char* ElementType = "elementType";
		static const char* ElementTypes = "elementTypes";
		static const char* Elements = "elements";
		static const char* SubElements = "subElements";
		static const char* Level = "level";
		static const char* Layer = "layer";
		static const char* Shape = "shape";
		static const char* Shape1 = "shape1";
		static const char* Shape2 = "shape2";
		
		static const char* Classifications = "classifications";
		namespace Classification
		{
			static const char* System = "system";
			static const char* Code = "code"; // id is reserved for Speckle id
			static const char* Name = "name";
		}
		
		static const char* MaterialQuantities = "materialQuantities";
		static const char* ElementProperties = "elementProperties";
		static const char* ComponentProperties = "componentProperties";
		static const char* SendListingParameters = "sendListingParameters";
		static const char* SendProperties = "sendProperties";
		namespace Quantity
		{
			static const char* Material = "material";
			static const char* Volume = "volume";
			static const char* Area = "area";
			static const char* Units = "units";
		}
		namespace ComponentProperty
		{
			static const char* Name = "name";
			static const char* PropertyGroups = "propertyGroups";
		}
		namespace PropertyGroup
		{
			static const char* Name = "name";
			static const char* PropertList = "propertyList";
		}
		namespace Property
		{
			static const char* Name = "name";
			static const char* Value = "value";
			static const char* Values = "values";
			static const char* Units = "units";
		}
	}
	
	static const char* Elements = "elements";
	static const char* Beams = "beams";
	static const char* Columns = "columns";
	static const char* DirectShapes = "directShapes";
	static const char* Doors = "doors";
	static const char* GridElements = "gridElements";
	static const char* Objects = "objects";
	static const char* MeshModels = "meshModels";
	static const char* Roofs = "roofs";
	static const char* Shells = "shells";
	static const char* Skylights = "skylights";
	static const char* Slabs = "slabs";
	static const char* Walls = "walls";
	static const char* Windows = "windows";
	static const char* Zones = "zones";
	static const char* Openings = "openings";
	
	static const char* Models = "models";
	static const char* SubelementModels = "subelementModels";
	
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
	
	
	namespace PivotPolyEdge {
		static const char* EdgeName = "Roof Pivot Poly Edge #%d";
		static const char* EdgeData = "roofPivotPolyEdges";
	}
	
	
	namespace PivotPolyEdgeData {
		static const char* NumLevelEdgeData = "nLevelEdgeData";
	}
	
	
	namespace LevelEdge {
		static const char* LevelEdgeName = "Roof Level #%d";
		static const char* LevelEdgeData = "roofLevels";
	}
	
	
	namespace RoofSegmentData {
		static const char* LevelAngle = "edgeLevelAngle";
		static const char* TopMaterial = "topMaterial";
		static const char* BottomMaterial = "bottomMaterial";
		static const char* CoverFillType = "coverFillType";
		static const char* EavesOverhang = "eavesOverhang";
		static const char* AngleType = "angleType";
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
		static const char* VerticalLinkTypeName = "verticalLinkTypeName";
		static const char* VerticalLinkStoryIndex = "verticalLinkStoryIndex";
		static const char* WallCutUsing = "wallCutUsing";
		
		static const char* PenIndex = "pen";
		static const char* LineTypeName = "lineTypeName";
		static const char* SectFillName = "sectFill";
		static const char* SectFillPenIndex = "sectFillPen";
		static const char* SectBackgroundPenIndex = "sectBackgroundPen";
		static const char* SectContPenIndex = "sectContPen";
		static const char* CutLineTypeName = "cutLineType";
		static const char* AboveViewLineTypeName = "aboveViewLineType";
		static const char* AboveViewLinePenIndex = "aboveViewLinePen";
		static const char* BelowViewLinePenIndex = "belowViewLinePen";
		static const char* BelowViewLineTypeName = "belowViewLineType";
		static const char* UseObjectPens = "useObjectPens";
		static const char* UseObjLinetypes = "useObjLinetypes";
		static const char* UseObjMaterials = "useObjMaterials";
		static const char* UseObjSectAttrs = "useObjSectAttrs";
		static const char* BuildingMaterialName = "buildingMaterial";
		static const char* LibraryPart = "libraryPart";
		static const char* DisplayOptionName = "displayOptionName";
		
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
		static const char* transform = "transform";
	}
	
	
	namespace Opening
	{
			// Floor Plan Parameters
		static const char* FloorPlanDisplayMode = "floorPlanDisplayMode";
		static const char* ConnectionMode = "connectionMode";
		
			// Cut Surfaces Parameters
		static const char* CutSurfacesUseLineOfCutElements = "cutsurfacesUseLineOfCutElements";
		static const char* CutSurfacesLinePenIndex = "cutsurfacesLinePenIndex";
		static const char* CutSurfacesLineIndex = "cutsurfacesLineIndex";
		
			// Outlines Parameters
		static const char* OutlinesStyle = "outlinesStyle";
		static const char* OutlinesUseLineOfCutElements = "outlinesUseLineOfCutElements"; // => Cut Surfaces Parameters-ben is megtalálható
		static const char* OutlinesUncutLineIndex = "outlinesUncutLineIndex";
		static const char* OutlinesOverheadLineIndex = "outlinesOverheadLineIndex";
		static const char* OutlinesUncutLinePenIndex = "outlinesUncutLinePenIndex";
		static const char* OutlinesOverheadLinePenIndex = "outlinesOverheadLinePenIndex";
		
			// Opening Cover Fills Parameters
		static const char* UseCoverFills = "useCoverFills";
		static const char* UseFillsOfCutElements = "useFillsOfCutElements";
		static const char* CoverFillIndex = "coverFillIndex";
		static const char* CoverFillPenIndex = "coverFillPenIndex";
		static const char* CoverFillBackgroundPenIndex = "coverFillBackgroundPenIndex";
		static const char* CoverFillOrientation = "coverFillOrientation";
		
			// Cover Fill Transformation Parameters
		static const char* CoverFillTransformationOrigoX = "coverFillTransformationOrigoX";
		static const char* CoverFillTransformationOrigoY = "coverFillTransformationOrigoY";
		static const char* CoverFillTransformationOrigoZ = "coverFillTransformationOrigoZ";
		static const char* CoverFillTransformationXAxisX = "coverFillTransformationXAxisX";
		static const char* CoverFillTransformationXAxisY = "coverFillTransformationXAxisY";
		static const char* CoverFillTransformationXAxisZ = "coverFillTransformationXAxisZ";
		static const char* CoverFillTransformationYAxisX = "coverFillTransformationYAxisX";
		static const char* CoverFillTransformationYAxisY = "coverFillTransformationYAxisY";
		static const char* CoverFillTransformationYAxisZ = "coverFillTransformationYAxisZ";
		
			// Reference Axis Parameters
		static const char* ShowReferenceAxis = "showReferenceAxis";
		static const char* ReferenceAxisPenIndex = "referenceAxisPenIndex";
		static const char* ReferenceAxisLineTypeIndex = "referenceAxisLineTypeIndex";
		static const char* ReferenceAxisOverhang = "referenceAxisOverhang";
		
			// Extrusion Geometry Parameters
		static const char* ExtrusionGeometryBasePoint = "extrusionGeometryBasePoint";
		static const char* ExtrusionGeometryXAxis = "extrusionGeometryXAxis";
		static const char* ExtrusionGeometryYAxis = "extrusionGeometryYAxis";
		static const char* ExtrusionGeometryZAxis = "extrusionGeometryZAxis";
		static const char* BasePolygonType = "basePolygonType";
		static const char* Width = "width";
		static const char* Height = "height";
		static const char* Constraint = "constraint";
		static const char* Anchor = "anchor";
		static const char* AnchorIndex = "anchorIndex";
		static const char* AnchorAltitude = "anchorAltitude";
		static const char* LimitType = "limitType";
		static const char* ExtrusionStartOffSet = "extrusionStartOffSet";
		static const char* FiniteBodyLength = "finiteBodyLength";
		static const char* LinkedStatus = "linkedStatus";
		static const char* NCoords = "nCoords";
		static const char* NSubPolys = "nSubPolys";
		static const char* NArcs = "nArcs";
	}
	
	
	namespace GridElement
	{
			// Main
		static const char* begin = "begin";
		static const char* end = "end";
		static const char* angle = "angle";
		static const char* markerText = "markerText";
		static const char* isArc = "isArc";
		static const char* arcAngle = "arcAngle";
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
	
	
	namespace Roof
	{
			// Geometry and positioning
		static const char* RoofClassName = "roofClassName";
		static const char* PlaneRoofAngle = "planeRoofAngle";
		static const char* BaseLine = "baseLine";
		static const char* BegC = "begC";
		static const char* EndC = "endC";
		static const char* PosSign = "posSign";
		static const char* PivotPolygon = "pivotPolygon";
		
			// Level
		static const char* levels = "levels";
		static const char* LevelNum = "levelNum";
		
		namespace RoofLevel {
			static const char* LevelName = "Level #%d";
			static const char* levelData = "levels";
		}
		
		namespace LevelData {
			static const char* LevelHeight = "levelHeight";
			static const char* LevelAngle = "levelAngle";
		}
		
		static const char* Thickness = "thickness";
		static const char* Structure = "structure";
		static const char* CompositeName = "compositeName";
		static const char* BuildingMaterialName = "buildingMaterialName";
			// Edge trims
		static const char* EdgeAngleType = "edgeAngleType";
		static const char* EdgeAngle = "edgeAngle";
			// Floor Plan and Section - Floor Plan Display
		static const char* DisplayOptionName = "displayOptionName";
		static const char* ViewDepthLimitationName = "showProjectionName";
			// Floor Plan and Section - Cut Surfaces
		static const char* SectContPen = "sectContPen";
		static const char* SectContLtype = "sectContLtype";
		static const char* CutFillPen = "cutFillPen";
		static const char* CutFillBackgroundPen = "cutFillBackgroundPen";
			// Floor Plan and Section - Outlines
		static const char* ContourPen = "contourPen";
		static const char* ContourLineType = "contourLineType";
		static const char* OverheadLinePen = "overheadLinePen";
		static const char* OverheadLinetype = "overheadLinetype";
			// Floor Plan and Section - Cover Fills
		static const char* UseFloorFill = "useFloorFill";
		static const char* FloorFillPen = "floorFillPen";
		static const char* FloorFillBGPen = "floorFillBGPen";
		static const char* FloorFillName = "floorFillName";
		static const char* Use3DHatching = "use3DHatching";
		static const char* UseFillLocBaseLine = "useFillLocBaseLine";
		static const char* UseSlantedFill = "useSlantedFill";
		static const char* HatchOrientation = "hatchOrientation";
		static const char* HatchOrientationOrigoX = "hatchOrientationOrigoX";
		static const char* HatchOrientationOrigoY = "hatchOrientationOrigoY";
		static const char* HatchOrientationXAxisX = "hatchOrientationXAxisX";
		static const char* HatchOrientationXAxisY = "hatchOrientationXAxisY";
		static const char* HatchOrientationYAxisX = "hatchOrientationYAxisX";
		static const char* HatchOrientationYAxisY = "hatchOrientationYAxisY";
			// Model
		static const char* TopMat = "topMat";
		static const char* SideMat = "sideMat";
		static const char* BotMat = "botMat";
		static const char* MaterialsChained = "materialsChained";
		static const char* TrimmingBodyName = "trimmingBodyName";
	}
	
	
	namespace Shell
	{
			// Geometry and positioning
		static const char* ShellClassName = "shellClassName";
		
		static const char* BasePlane = "basePlane";
		
		static const char* Flipped = "flipped";
		static const char* HasContour = "hasContour";
		static const char* NumHoles = "numHoles";
		static const char* ShellContourName = "shellContour #%d";
		static const char* ShellContourData = "shellContours";
		static const char* ShellContourPlane = "shellContourPlane";
		static const char* ShellContourPoly = "shellContourPoly";
		static const char* ShellContourHeight = "shellContourHeight";
		static const char* ShellContourID = "shellContourID";
		static const char* ShellContourSideTypeName = "sideTypeName";
		static const char* ShellContourSideAngle = "sideAngle";
		static const char* ShellContourEdgeTypeName = "edgeTypeName";
		static const char* ShellContourEdgeSideMaterial = "edgeSideMaterial";
		static const char* ShellContourEdgeName = "shellContourEdge #%d";
		static const char* ShellContourEdgeData = "shellContourEdges";
		static const char* DefaultEdgeType = "defaultEdgeType";
		
		static const char* SlantAngle = "slantAngle";
		static const char* RevolutionAngle = "revolutionAngle";
		static const char* DistortionAngle = "distortionAngle";
		static const char* SegmentedSurfaces = "segmentedSurfaces";
		static const char* ShapePlaneTilt = "shapePlaneTilt";
		static const char* BegPlaneTilt = "begPlaneTilt";
		static const char* EndPlaneTilt = "endPlaneTilt";
		static const char* AxisBase = "axisBase";
		static const char* Plane1 = "plane1";
		static const char* Plane2 = "plane2";
		static const char* BegC = "begC";
		static const char* BegAngle = "begAngle";
		static const char* ExtrusionVector = "extrusionVector";
		static const char* ShapeDirection = "shapeDirection";
		static const char* DistortionVector = "distortionVector";
		static const char* MorphingRuleName = "morphingRuleName";
		
		static const char* BegShapeEdge = "begShapeEdge";
		static const char* BegShapeEdgeTrimSideType = "begShapeEdgeTrimSideType";
		static const char* BegShapeEdgeTrimSideAngle = "begShapeEdgeTrimSideAngle";
		static const char* BegShapeEdgeSideMaterial = "begShapeEdgeSideMaterial";
		static const char* BegShapeEdgeType = "begShapeEdgeType";
		static const char* EndShapeEdge = "endShapeEdge";
		static const char* EndShapeEdgeTrimSideType = "endShapeEdgeTrimSideType";
		static const char* EndShapeEdgeTrimSideAngle = "endShapeEdgeTrimSideAngle";
		static const char* EndShapeEdgeSideMaterial = "endShapeEdgeSideMaterial";
		static const char* EndShapeEdgeType = "endShapeEdgeType";
		
		static const char* ExtrudedEdge1 = "extrudedEdge1";
		static const char* ExtrudedEdgeTrimSideType1 = "extrudedEdgeTrimSideType1";
		static const char* ExtrudedEdgeTrimSideAngle1 = "extrudedEdgeTrimSideAngle1";
		static const char* ExtrudedEdgeSideMaterial1 = "extrudedEdgeSideMaterial1";
		static const char* ExtrudedEdgeType1 = "extrudedEdgeType1";
		static const char* ExtrudedEdge2 = "extrudedEdge2";
		static const char* ExtrudedEdgeTrimSideType2 = "extrudedEdgeTrimSideType2";
		static const char* ExtrudedEdgeTrimSideAngle2 = "extrudedEdgeTrimSideAngle2";
		static const char* ExtrudedEdgeSideMaterial2 = "extrudedEdgeSideMaterial2";
		static const char* ExtrudedEdgeType2 = "extrudedEdgeType2";
		
		static const char* RevolvedEdge1 = "revolvedEdge1";
		static const char* RevolvedEdgeTrimSideType1 = "revolvedEdgeTrimSideType1";
		static const char* RevolvedEdgeTrimSideAngle1 = "revolvedEdgeTrimSideAngle1";
		static const char* RevolvedEdgeSideMaterial1 = "revolvedEdgeSideMaterial1";
		static const char* RevolvedEdgeType1 = "revolvedEdgeType1";
		static const char* RevolvedEdge2 = "revolvedEdge2";
		static const char* RevolvedEdgeTrimSideType2 = "revolvedEdgeTrimSideType2";
		static const char* RevolvedEdgeTrimSideAngle2 = "revolvedEdgeTrimSideAngle2";
		static const char* RevolvedEdgeSideMaterial2 = "revolvedEdgeSideMaterial2";
		static const char* RevolvedEdgeType2 = "revolvedEdgeType2";
		
		static const char* RuledEdge1 = "ruledEdge1";
		static const char* RuledEdgeTrimSideType1 = "ruledEdgeTrimSideType1";
		static const char* RuledEdgeTrimSideAngle1 = "ruledEdgeTrimSideAngle1";
		static const char* RuledEdgeSideMaterial1 = "ruledEdgeSideMaterial1";
		static const char* RuledEdgeType1 = "ruledEdgeType1";
		static const char* RuledEdge2 = "ruledEdge2";
		static const char* RuledEdgeTrimSideType2 = "ruledEdgeTrimSideType2";
		static const char* RuledEdgeTrimSideAngle2 = "ruledEdgeTrimSideAngle2";
		static const char* RuledEdgeSideMaterial2 = "ruledEdgeSideMaterial2";
		static const char* RuledEdgeType2 = "ruledEdgeType2";
		
		static const char* Thickness = "thickness";
		static const char* Structure = "structure";
		static const char* CompositeName = "compositeName";
		static const char* BuildingMaterialName = "buildingMaterialName";
			// Edge trims
		static const char* EdgeAngleType = "edgeAngleType";
		static const char* EdgeAngle = "edgeAngle";
			// Floor Plan and Section - Floor Plan Display
		static const char* DisplayOptionName = "displayOptionName";
		static const char* ViewDepthLimitationName = "showProjectionName";
			// Floor Plan and Section - Cut Surfaces
		static const char* SectContPen = "sectContPen";
		static const char* SectContLtype = "sectContLtype";
		static const char* CutFillPen = "cutFillPen";
		static const char* CutFillBackgroundPen = "cutFillBackgroundPen";
			// Floor Plan and Section - Outlines
		static const char* ContourPen = "contourPen";
		static const char* ContourLineType = "contourLineType";
		static const char* OverheadLinePen = "overheadLinePen";
		static const char* OverheadLinetype = "overheadLinetype";
			// Floor Plan and Section - Cover Fills
		static const char* UseFloorFill = "useFloorFill";
		static const char* FloorFillPen = "floorFillPen";
		static const char* FloorFillBGPen = "floorFillBGPen";
		static const char* FloorFillName = "floorFillName";
		static const char* Use3DHatching = "use3DHatching";
		static const char* UseFillLocBaseLine = "useFillLocBaseLine";
		static const char* UseSlantedFill = "useSlantedFill";
		static const char* HatchOrientation = "hatchOrientation";
		static const char* HatchOrientationOrigoX = "hatchOrientationOrigoX";
		static const char* HatchOrientationOrigoY = "hatchOrientationOrigoY";
		static const char* HatchOrientationXAxisX = "hatchOrientationXAxisX";
		static const char* HatchOrientationXAxisY = "hatchOrientationXAxisY";
		static const char* HatchOrientationYAxisX = "hatchOrientationYAxisX";
		static const char* HatchOrientationYAxisY = "hatchOrientationYAxisY";
			// Model
		static const char* TopMat = "topMat";
		static const char* SideMat = "sideMat";
		static const char* BotMat = "botMat";
		static const char* MaterialsChained = "materialsChained";
		static const char* TrimmingBodyName = "trimmingBodyName";
	}
	
	
	namespace Skylight
	{
			// Geometry and positioning
		static const char* VertexID = "vertexID";
		static const char* SkylightFixMode = "skylightFixMode";
		static const char* SkylightAnchor = "skylightAnchor";
		static const char* AnchorPosition = "anchorPosition";
		static const char* AnchorLevel = "anchorLevel";
		static const char* AzimuthAngle = "azimuthAngle";
		static const char* ElevationAngle = "elevationAngle";
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
		static const char* PointId1 = "v1";
		static const char* PointId2 = "v2";
		static const char* PolygonId1 = "p1";
		static const char* PolygonId2 = "p2";
		static const char* EdgeStatus = "s";
		static const char* HiddenEdgeValueName = "HiddenEdge";
		static const char* SmoothEdgeValueName = "SmoothEdge";
		static const char* VisibleEdgeValueName = "VisibleEdge";
		static const char* Polygons = "polygons";
		static const char* Materials = "materials";
		static const char* PointIds = "pointIds";
		static const char* MaterialName = "name";
		static const char* Transparency = "transparency";
		static const char* AmbientColor = "ambientColor";
		static const char* EmissionColor = "emissionColor";
		static const char* Material = "material";
		static const char* Model = "model";
		static const char* ModelIds = "modelIds";
		static const char* Ids = "ids";
		static const char* Edges = "edges";
	}
	
	
	namespace Level
	{
		static const char* TypeName		= "level";
		static const char* Index		= "index";
		static const char* Name			= "name";
		static const char* Elevation	= "elevation";
		static const char* Units		= "units";
	}
	
	
	namespace Point
	{
		static const char* X		= "x";
		static const char* Y		= "y";
		static const char* Z		= "z";
		static const char* Units	= "units";
	}
	
	namespace Material
	{
		static const char* Name = "name";
	}
		
}


#endif
