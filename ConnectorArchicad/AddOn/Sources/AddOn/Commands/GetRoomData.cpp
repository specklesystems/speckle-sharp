#include "GetRoomData.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Point.hpp"
#include "RealNumber.h"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"

namespace AddOnCommands
{
    static GS::ObjectState SerializeRoomType(const API_ZoneType& zone)
    {
        GS::ObjectState os;

        // The identifier of the room
        os.Add(ApplicationIdFieldName, APIGuidToString(zone.head.guid));

        // The index of the room's floor
        os.Add(FloorIndexFieldName, zone.head.floorInd);
        
        // The base point of the room
        double z = Utility::GetStoryLevel(zone.head.floorInd) + zone.roomBaseLev;
        os.Add(Room::BasePointFieldName, Objects::Point3D(0, 0, z));

        // The height of the room
        os.Add(Room::HeightFieldName, zone.roomHeight);
        

        return os;
    }

    GS::String GetRoomData::GetName() const
    {
        return GetRoomDataCommandName
    }

    GS::ObjectState GetRoomData::Execute(const GS::ObjectState& parameters,
                                         GS::ProcessControl& /*processControl*/) const
    {
        GS::Array<GS::UniString> ids;
        parameters.Get(ApplicationIdsFieldName, ids);
        GS::Array<API_Guid> elementGuids = ids.Transform<API_Guid>([](const GS::UniString& idStr)
        {
            return APIGuidFromString(idStr.ToCStr());
        });

        GS::ObjectState result;
        const auto& listAdder = result.AddList<GS::ObjectState>(RoomsFieldName);
        for (const API_Guid& guid : elementGuids)
        {
            API_Element element{};
            element.header.guid = guid;

            GSErrCode err = ACAPI_Element_Get(&element);
            if (err != NoError)
            {
                continue;
            }

            if (element.header.typeID != API_ZoneID)
            {
                continue;
            }

            listAdder(SerializeRoomType(element.zone));
        }

        return result;
    }
}
