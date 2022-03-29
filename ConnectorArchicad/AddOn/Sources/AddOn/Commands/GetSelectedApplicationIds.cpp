#include "GetSelectedApplicationIds.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "FieldNames.hpp"


namespace AddOnCommands {


  static GS::Array<API_Guid> GetSelectedElementGuids()
  {
    GS::Array<API_Guid>		elementGuids;
    API_SelectionInfo		selectionInfo;
    GS::Array<API_Neig>		selNeigs;

    GSErrCode err = ACAPI_Selection_Get(&selectionInfo, &selNeigs, true);
    if (err == NoError) {
      if (selectionInfo.typeID != API_SelEmpty) {
        for (const API_Neig& neig : selNeigs) {
          if (Utility::IsElement3D(neig.guid)) {
            elementGuids.Push(neig.guid);
          }
        }
      }
    }

    BMKillHandle((GSHandle*)&selectionInfo.marquee.coords);

    return elementGuids;
  }


  GS::String GetSelectedApplicationIds::GetName() const
  {
    return GetSelectedApplicationIdsCommandName;
  }


  GS::ObjectState GetSelectedApplicationIds::Execute(const GS::ObjectState& /*parameters*/, GS::ProcessControl& /*processControl*/) const
  {

    GS::Array<API_Guid>	elementGuids = GetSelectedElementGuids();

    GS::ObjectState retVal;

    const auto& listAdder = retVal.AddList<GS::UniString>(ApplicationIdsFieldName);
    for (const API_Guid& guid : elementGuids) {
      listAdder(APIGuidToString(guid));
    }

    return retVal;
  }


}