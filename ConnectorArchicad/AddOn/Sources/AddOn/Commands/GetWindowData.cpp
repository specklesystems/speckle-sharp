#include "GetWindowData.hpp"
#include "GetOpeningBaseData.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Point.hpp"
#include "RealNumber.h"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"


namespace AddOnCommands {
  static GS::ObjectState GetWindows(const API_Guid& guid) {
    GSErrCode err = NoError;

    GS::ObjectState currentWindow;
    API_Element element;

    BNZeroMemory(&element, sizeof(API_Element));
    element.header.guid = guid;
    err = ACAPI_Element_Get(&element);

    if (err == NoError) {
		if (err == NoError) {
		  currentWindow.Add(ApplicationIdFieldName, APIGuidToString(guid));
		  currentWindow.Add(ParentElementIdFieldName, APIGuidToString(element.door.owner));

		  AddOnCommands::GetOpeningBaseData<API_WindowType> (element.window, currentWindow);
		}
    }

    return currentWindow;
  }

  GS::String GetWindowData::GetName () const {
    return GetWindowCommandName;
  }

  GS::ObjectState GetWindowData::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const {
    GS::ObjectState result;

    GS::Array<GS::UniString> ids;
    parameters.Get(ApplicationIdsFieldName, ids);
    GS::Array<API_Guid> elementGuids = ids.Transform<API_Guid>([](const GS::UniString& idStr)
      { return APIGuidFromString(idStr.ToCStr()); });
    
    if (elementGuids.IsEmpty())
      return result;

    const auto& listAdderWindows = result.AddList<GS::ObjectState>(WindowsFieldName);

    for (const API_Guid& guid : elementGuids)
    {
      API_Element element{};
      element.header.guid = guid;

      GSErrCode err = ACAPI_Element_Get(&element);

      if (err != NoError /*|| element.header.type.typeID != API_WallID*/) {
        return result;
      }

#ifdef ServerMainVers_2600
        if (element.header.type.typeID == API_WindowID) {
#else
		if (element.header.typeID == API_WindowID) {
#endif
          GS::ObjectState door = GetWindows(element.header.guid);
          listAdderWindows(door);
        }
      }

    return result;
  }

}
