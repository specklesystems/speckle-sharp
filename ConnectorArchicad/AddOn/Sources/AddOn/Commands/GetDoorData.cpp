#include "GetDoorData.hpp"
#include "GetOpeningBaseData.hpp"
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
      currentDoor.Add(ApplicationIdFieldName, APIGuidToString(guid));
      currentDoor.Add(ParentElementIdFieldName, APIGuidToString(element.door.owner));
      
	  AddOnCommands::GetOpeningBaseData<API_DoorType> (element.door, currentDoor);
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
