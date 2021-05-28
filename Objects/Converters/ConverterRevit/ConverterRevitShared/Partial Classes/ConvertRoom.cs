using Autodesk.Revit.DB;
using Objects.BuiltElements;
using Objects.Geometry;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB.Architecture;
using Point = Objects.Geometry.Point;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public BuiltElements.Room RoomToSpeckle(DB.Room revitRoom)
    {
      var profiles = GetProfiles(revitRoom);

      var speckleRoom = new Room();

      speckleRoom.name = revitRoom.get_Parameter(BuiltInParameter.ROOM_NAME).AsString();
      speckleRoom.number = revitRoom.Number;
      speckleRoom.center = (Point)LocationToSpeckle(revitRoom);
      speckleRoom.level = ConvertAndCacheLevel(revitRoom, BuiltInParameter.ROOM_LEVEL_ID);
      speckleRoom.outline = profiles[0];
      if (profiles.Count > 1)
      {
        speckleRoom.voids = profiles.Skip(1).ToList();
      }

      GetAllRevitParamsAndIds(speckleRoom, revitRoom);
      speckleRoom.displayMesh = GetElementDisplayMesh(revitRoom);

      return speckleRoom;
    }

    private List<ICurve> GetProfiles(DB.Room room)
    {
      var profiles = new List<ICurve>();
      var boundaries = room.GetBoundarySegments(new SpatialElementBoundaryOptions());
      foreach (var loop in boundaries)
      {
        var poly = new Polycurve(ModelUnits);
        foreach (var segment in loop)
        {
          var c = segment.GetCurve();

          if (c == null)
          {
            continue;
          }

          poly.segments.Add(CurveToSpeckle(c));
        }
        profiles.Add(poly);
      }
      return profiles;
    }
  }
}