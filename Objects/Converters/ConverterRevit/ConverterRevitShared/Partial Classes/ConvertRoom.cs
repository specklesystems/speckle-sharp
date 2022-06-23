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
    public List<ApplicationPlaceholderObject> RoomToNative(Room speckleRoom)
    {
      var revitRoom = GetExistingElementByApplicationId(speckleRoom.applicationId) as DB.Room;
      if (revitRoom != null && ReceiveMode == Speckle.Core.Kits.ReceiveMode.Ignore)
        return new List<ApplicationPlaceholderObject> { new ApplicationPlaceholderObject { applicationId = speckleRoom.applicationId, ApplicationGeneratedId = revitRoom.UniqueId, NativeObject = revitRoom } }; ;
      var level = ConvertLevelToRevit(speckleRoom.level);

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

      var placeholders = new List<ApplicationPlaceholderObject>()
      {
        new ApplicationPlaceholderObject
        {
        applicationId = speckleRoom.applicationId,
        ApplicationGeneratedId = revitRoom.UniqueId,
        NativeObject = revitRoom
        }
      };
      Report.Log($"{(isUpdate ? "Updated" : "Created")} Room {revitRoom.Id}");
      return placeholders;

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
      {
        speckleRoom.voids = profiles.Skip(1).ToList();
      }

      GetAllRevitParamsAndIds(speckleRoom, revitRoom);

      speckleRoom.displayValue = GetElementDisplayMesh(revitRoom);
      Report.Log($"Converted Room {revitRoom.Id}");

      return speckleRoom;
    }




  }
}