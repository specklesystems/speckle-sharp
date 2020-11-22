using Autodesk.Revit.DB;
using Objects.Geometry;
using Objects.Revit;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB.Architecture;
using Level = Objects.BuiltElements.Level;
using Point = Objects.Geometry.Point;
using Room = Objects.BuiltElements.Room;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public RevitRoom RoomToSpeckle(DB.Room revitRoom)
    {
      var baseLevelParam = revitRoom.get_Parameter(BuiltInParameter.ROOM_LEVEL_ID);
      var profiles = GetProfiles(revitRoom);

      var speckleRoom = new RevitRoom();

      speckleRoom.name = revitRoom.get_Parameter(BuiltInParameter.ROOM_NAME).AsString();
      speckleRoom.number = revitRoom.Number;
      speckleRoom.basePoint = (Point)LocationToSpeckle(revitRoom);
      speckleRoom.level = ConvertAndCacheLevel(baseLevelParam);
      speckleRoom.outline = profiles[0];
      if (profiles.Count > 1)
        speckleRoom.voids = profiles.Skip(1).ToList();

      AddCommonRevitProps(speckleRoom, revitRoom);

      (speckleRoom.displayMesh.faces, speckleRoom.displayMesh.vertices) = MeshUtils.GetFaceVertexArrayFromElement(revitRoom, Scale);

      return speckleRoom;
    }

    private List<ICurve> GetProfiles(DB.Room room)
    {
      var profiles = new List<ICurve>();
      var boundaries = room.GetBoundarySegments(new SpatialElementBoundaryOptions());
      foreach (var loop in boundaries)
      {
        var poly = new Polycurve();
        foreach (var segment in loop)
        {
          var c = segment.GetCurve();

          if (c == null) continue;
          poly.segments.Add(CurveToSpeckle(c));
        }
        profiles.Add(poly);
      }
      return profiles;
    }
  }
}