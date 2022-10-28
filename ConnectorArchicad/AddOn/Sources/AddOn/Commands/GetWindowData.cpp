#include "GetWindowData.hpp"
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
      GS::ObjectState openingBase;
      openingBase.Add(Window::width, element.window.openingBase.width);
      openingBase.Add(Window::height, element.window.openingBase.height);
      openingBase.Add(Window::subFloorThickness, element.window.openingBase.subFloorThickness);
      openingBase.Add(Window::reflected, element.window.openingBase.reflected);
      openingBase.Add(Window::oSide, element.window.openingBase.oSide);
      openingBase.Add(Window::refSide, element.window.openingBase.refSide);
      currentWindow.Add(Window::openingBase, openingBase);

      currentWindow.Add(ApplicationIdFieldName, APIGuidToString(guid));
      currentWindow.Add(Window::revealDepthFromSide, element.window.revealDepthFromSide);
      currentWindow.Add(Window::jambDepthHead, element.window.jambDepthHead);
      currentWindow.Add(Window::jambDepth, element.window.jambDepth);
      currentWindow.Add(Window::jambDepth2, element.window.jambDepth2);
      currentWindow.Add(Window::objLoc, element.window.objLoc);
      currentWindow.Add(Window::lower, element.window.lower);
      currentWindow.Add(Window::directionType, windowDoorDirectionTypeNames.Get(element.window.directionType));

      currentWindow.Add(Window::startPoint, Objects::Point3D(element.window.startPoint.x, element.window.startPoint.y, 0));
      currentWindow.Add(Window::dirVector, Objects::Point3D(element.window.dirVector.x, element.window.dirVector.y, 0));

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
