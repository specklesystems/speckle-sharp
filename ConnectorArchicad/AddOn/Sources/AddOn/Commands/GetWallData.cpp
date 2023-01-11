#include "GetWallData.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Point.hpp"
#include "RealNumber.h"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"

namespace AddOnCommands {

  static GS::ObjectState SerializeWallType (const API_WallType& wall) {
    GS::ObjectState os;

    // The identifier of the wall
    os.Add (ApplicationIdFieldName, APIGuidToString (wall.head.guid));

    // The index of the wall's floor
    os.Add (FloorIndexFieldName, wall.head.floorInd);

    // The start and end points of the wall
    double z = Utility::GetStoryLevel (wall.head.floorInd) + wall.bottomOffset;
    os.Add (Wall::StartPointFieldName, Objects::Point3D (wall.begC.x, wall.begC.y, z));
    os.Add (Wall::EndPointFieldName, Objects::Point3D (wall.endC.x, wall.endC.y, z));

    // The arc angle of the wall
    if (abs (wall.angle) > EPS)
      os.Add (Wall::ArcAngleFieldName, wall.angle);

    // The parameters of the wall
    os.Add (Wall::HeightFieldName, wall.height);
    os.Add (Wall::BaseOffsetFieldName, wall.bottomOffset);
    os.Add (Wall::TopOffsetFieldName, wall.topOffset);
    os.Add (Wall::FlippedFieldName, wall.flipped);

    // The structure type of the wall (basic, composite or profiled)
    os.Add (Wall::StructureFieldName, structureTypeNames.Get (wall.modelElemStructureType));

    // The building material index, composite index or the profile index of the wall
    switch (wall.modelElemStructureType) {
    case API_BasicStructure:
      os.Add (Wall::BuildingMaterialIndexFieldName, wall.buildingMaterial);
      break;
    case API_CompositeStructure:
      os.Add (Wall::CompositeIndexFieldName, wall.composite);
      break;
    case API_ProfileStructure:
      os.Add (Wall::ProfileIndexFieldName, wall.profileAttr);
      break;
    default:
      break;
    }

    // The geometry method of the wall (straight, trapezoid or polygonal)
    os.Add (Wall::GeometryMethodFieldName, wallTypeNames.Get (wall.type));

    // The profile type of the wall (straight, slanted, trapezoid or polygonal)
    os.Add (Wall::WallComplexityFieldName, profileTypeNames.Get (wall.profileType));

    // The thickness of the wall (first and second thickness for trapezoid walls)
    if (wall.type == APIWtyp_Trapez) {
      os.Add (Wall::FirstThicknessFieldName, wall.thickness);
      os.Add (Wall::SecondThicknessFieldName, wall.thickness1);
    }
    else {
      os.Add (Wall::ThicknessFieldName, wall.thickness);
    }

    // The outside slant angle of the wall
    os.Add (Wall::OutsideSlantAngleFieldName, wall.slantAlpha);

    // The inside slant angle of the wall
    if (wall.profileType == APISect_Trapez)
      os.Add (Wall::InsideSlantAngleFieldName, wall.slantBeta);

    // Does it have any embedded object?
    os.Add(Wall::HasDoorFieldName, wall.hasDoor);
    os.Add(Wall::HasWindowFieldName, wall.hasWindow);

    // End
    return os;
  }

  GS::String GetWallData::GetName () const {
    return GetWallDataCommandName;
  }

  GS::ObjectState GetWallData::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const {
    GS::Array<GS::UniString> ids;
    parameters.Get (ApplicationIdsFieldName, ids);
    GS::Array<API_Guid> elementGuids = ids.Transform<API_Guid> ([] (const GS::UniString& idStr) { return APIGuidFromString (idStr.ToCStr ()); });

    GS::ObjectState result;
    const auto& listAdder = result.AddList<GS::ObjectState> (WallsFieldName);
    for (const API_Guid& guid : elementGuids) {
      API_Element element {};
      element.header.guid = guid;

      GSErrCode err = ACAPI_Element_Get (&element);
      if (err != NoError) {
        continue;
      }

#ifdef ServerMainVers_2600
      if (element.header.type.typeID != API_WallID)
#else
      if (element.header.typeID != API_WallID)
#endif
      {
        continue;
      }

      listAdder(SerializeWallType(element.wall));
    }

    return result;
  }

}