#include "GetRoofData.hpp"
#include <locale>
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "RealNumber.h"
#include "FieldNames.hpp"

namespace AddOnCommands
{
  static GS::ObjectState SerializeRoofType(const API_RoofType& roof, const API_ElementMemo& memo,
                                           const API_ElementQuantity& quantity)
  {
    UNUSED_PARAMETER (memo);
    UNUSED_PARAMETER (quantity);

    GS::ObjectState os;
    // memo.coords;
    // quantity.roof.volume;
    // The identifier of the room
    os.Add(ApplicationIdFieldName, APIGuidToString(roof.head.guid));
    // GS::UniString roomName = roof.roomName;
    // GS::UniString roomNum = roof.roomNoStr;
    // os.Add(Room::NameFieldName, roomName);
    // os.Add(Room::NumberFieldName, roomNum);

    // The index of the roof's floor
    os.Add(FloorIndexFieldName, roof.head.floorInd);



    return os;
  }

  GS::String GetRoofData::GetName() const
  {
    return GetRoofDataCommandName
  }

  GS::ObjectState GetRoofData::Execute(const GS::ObjectState& parameters,
                                       GS::ProcessControl& /*processControl*/) const
  {
    GS::Array<GS::UniString> ids;
    parameters.Get(ApplicationIdsFieldName, ids);
    GS::Array<API_Guid> elementGuids = ids.Transform<API_Guid>([](const GS::UniString& idStr)
    {
      return APIGuidFromString(idStr.ToCStr());
    });

    GS::ObjectState result;
    const auto& listAdder = result.AddList<GS::ObjectState>(RoofsFieldName);
    for (const API_Guid& guid : elementGuids)
    {
      // element and memo 
      API_Element element{};
      API_ElementMemo elementMemo{};
      element.header.guid = guid;

      GSErrCode err = ACAPI_Element_Get(&element);
      if (err != NoError) continue;

#ifdef ServerMainVers_2600
      if (element.header.type.typeID != API_RoofID)
#else
      if (element.header.typeID != API_RoofID)
#endif
        continue;
      err = ACAPI_Element_GetMemo(guid, &elementMemo, APIMemoMask_All);
      if (err != NoError) continue;

      // quantities
      API_ElementQuantity quantity = {};
      API_Quantities quantities = {};
      API_QuantitiesMask mask;

      ACAPI_ELEMENT_QUANTITY_MASK_CLEAR(mask);
      ACAPI_ELEMENT_QUANTITY_MASK_SET(mask, roof, volume);

      quantities.elements = &quantity;
      err = ACAPI_Element_GetQuantities(guid, nullptr, &quantities, &mask);
      if (err != NoError) continue;

      listAdder(SerializeRoofType(element.roof, elementMemo, quantity));
    }

    return result;
  }
}
