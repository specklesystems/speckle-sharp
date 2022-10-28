#include "GetDoorData.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Point.hpp"
#include "RealNumber.h"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"


namespace AddOnCommands {
  static GS::ObjectState GetDoors(const API_Guid& guid) {
    GSErrCode err = NoError;

    GS::ObjectState currentDoor;
    API_Element element;

    BNZeroMemory(&element, sizeof(API_Element));
    element.header.guid = guid;
    err = ACAPI_Element_Get(&element);

    if (err == NoError) {
      GS::ObjectState openingBase;
      openingBase.Add(Door::width, element.door.openingBase.width);
      openingBase.Add(Door::height, element.door.openingBase.height);
      openingBase.Add(Door::subFloorThickness, element.door.openingBase.subFloorThickness);
      openingBase.Add(Door::reflected, element.door.openingBase.reflected);
      openingBase.Add(Door::oSide, element.door.openingBase.oSide);
      openingBase.Add(Door::refSide, element.door.openingBase.refSide);
      currentDoor.Add(Door::openingBase, openingBase);

      currentDoor.Add(ApplicationIdFieldName, APIGuidToString(guid));
      currentDoor.Add(Door::revealDepthFromSide, element.door.revealDepthFromSide);
      currentDoor.Add(Door::jambDepthHead, element.door.jambDepthHead);
      currentDoor.Add(Door::jambDepth, element.door.jambDepth);
      currentDoor.Add(Door::jambDepth2, element.door.jambDepth2);
      currentDoor.Add(Door::objLoc, element.door.objLoc);
      currentDoor.Add(Door::lower, element.door.lower);
      currentDoor.Add(Door::directionType, windowDoorDirectionTypeNames.Get(element.door.directionType));

      currentDoor.Add(Door::startPoint, Objects::Point3D(element.door.startPoint.x, element.door.startPoint.y, 0));
      currentDoor.Add(Door::dirVector, Objects::Point3D(element.door.dirVector.x, element.door.dirVector.y, 0));
    }

    return currentDoor;
  }

  GS::String GetDoorData::GetName () const {
    return GetDoorCommandName;
  }

  GS::ObjectState GetDoorData::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const {
    GS::ObjectState result;

    GS::Array<GS::UniString> ids;
    parameters.Get(ApplicationIdsFieldName, ids);
    GS::Array<API_Guid> elementGuids = ids.Transform<API_Guid>([](const GS::UniString& idStr)
      { return APIGuidFromString(idStr.ToCStr()); });
    
    if (elementGuids.IsEmpty())
      return result;

    const auto& listAdderDoors = result.AddList<GS::ObjectState>(DoorsFieldName);

    for (const API_Guid& guid : elementGuids)
    {
      API_Element element{};
      element.header.guid = guid;

      GSErrCode err = ACAPI_Element_Get(&element);

      if (err != NoError /*|| element.header.type.typeID != API_WallID*/) {
        return result;
      }

#ifdef ServerMainVers_2600
        if (element.header.type.typeID == API_DoorID) {
#else
		if (element.header.typeID == API_DoorID) {
#endif
          GS::ObjectState door = GetDoors(element.header.guid);
          listAdderDoors(door);
        }
      }

    return result;
  }

}
