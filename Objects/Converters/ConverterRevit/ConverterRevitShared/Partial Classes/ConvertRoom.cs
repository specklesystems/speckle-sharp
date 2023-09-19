using Autodesk.Revit.DB;
using Objects.BuiltElements;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB.Architecture;
using Point = Objects.Geometry.Point;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationObject RoomToNative(Room speckleRoom)
    {
      var revitRoom = GetExistingElementByApplicationId(speckleRoom.applicationId) as DB.Room;
      var appObj = new ApplicationObject(speckleRoom.id, speckleRoom.speckle_type) { applicationId = speckleRoom.applicationId };

      // skip if element already exists in doc & receive mode is set to ignore
      if (IsIgnore(revitRoom, appObj))
        return appObj;

      var level = ConvertLevelToRevit(speckleRoom.level, out ApplicationObject.State levelState);

      var isUpdate = true;
      if (revitRoom == null)
      {
        var basePoint = PointToNative(speckleRoom.basePoint);

        // set computation level of Level based on the bottom elevation of the Room (Rooms can have offset elevation from Levels)
        // it is not guaranteed that the final computation level will fit for all the Rooms, however not generating extra levels is preferred
        if (level.get_Parameter(BuiltInParameter.LEVEL_ROOM_COMPUTATION_HEIGHT).AsDouble() < basePoint.Z) {
          TrySetParam(level, BuiltInParameter.LEVEL_ROOM_COMPUTATION_HEIGHT, basePoint.Z);
        }

        revitRoom = Doc.Create.NewRoom(level, new UV(basePoint.X, basePoint.Y));
        isUpdate = false;
      }

      revitRoom.Name = speckleRoom.name;
      revitRoom.Number = speckleRoom.number;

      SetInstanceParameters(revitRoom, speckleRoom);

      var state = isUpdate ? ApplicationObject.State.Updated : ApplicationObject.State.Created;
      appObj.Update(status: state, createdId: revitRoom.UniqueId, convertedItem: revitRoom);
      return appObj;
    }

    public BuiltElements.Room RoomToSpeckle(DB.Room revitRoom)
    {
      var profiles = GetProfiles(revitRoom);

      var speckleRoom = new Room();

      speckleRoom.name = revitRoom.get_Parameter(BuiltInParameter.ROOM_NAME).AsString();
      speckleRoom.number = revitRoom.Number;
      speckleRoom.basePoint = (Point)LocationToSpeckle(revitRoom);
      speckleRoom.level = ConvertAndCacheLevel(revitRoom, BuiltInParameter.ROOM_LEVEL_ID);
      if (profiles.Any())
        speckleRoom.outline = profiles[0];
      speckleRoom.area = GetParamValue<double>(revitRoom, BuiltInParameter.ROOM_AREA);
      if (profiles.Count > 1)
        speckleRoom.voids = profiles.Skip(1).ToList();

      GetAllRevitParamsAndIds(speckleRoom, revitRoom);

      speckleRoom.displayValue = GetElementDisplayValue(revitRoom);
      var phase = Doc.GetElement(revitRoom.get_Parameter(BuiltInParameter.ROOM_PHASE).AsElementId());
      if (phase != null)
      {
        speckleRoom["phaseCreated"] = phase.Name;
      }

      return speckleRoom;
    }
  }
}
