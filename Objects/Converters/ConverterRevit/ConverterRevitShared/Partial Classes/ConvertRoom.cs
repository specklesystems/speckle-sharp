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
      if (IsIgnore(revitRoom, appObj, out appObj))
        return appObj;

      var level = ConvertLevelToRevit(speckleRoom.level, out ApplicationObject.State levelState);

      var isUpdate = true;
      if (revitRoom == null)
      {
        var basePoint = PointToNative(speckleRoom.basePoint);
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

      speckleRoom.displayValue = GetElementDisplayMesh(revitRoom);

      return speckleRoom;
    }
  }
}