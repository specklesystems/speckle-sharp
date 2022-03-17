#ifndef FIELD_NAMES_HPP
#define FIELD_NAMES_HPP

static const char* ApplicationIdFieldName = "applicationId";
static const char* ApplicationIdsFieldName = "applicationIds";
static const char* ElementTypeFieldName = "elementType";
static const char* ElementTypesFieldName = "elementTypes";

static const char* FloorIndexFieldName = "floorIndex";
static const char* ShapeFieldName = "shape";

static const char* WallsFieldName = "walls";
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
  static const char* TransparencyieldName = "transparency";
  static const char* AmbientColorFieldName = "ambientColor";
  static const char* EmissionColorFieldName = "emissionColor";
  static const char* MaterialFieldName = "material";
  static const char* ModelFieldName = "model";
}


#endif
