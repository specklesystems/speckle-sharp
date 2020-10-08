using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using DB = Autodesk.Revit.DB.Architecture;
using Objects.Geometry;
using System;
using System.Collections.Generic;
using System.Text;
using Room = Objects.Room;
using Level= Objects.Level;
using Objects;
using System.Linq;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public Room RoomToSpeckle(DB.Room revitRoom)
    {
      var baseLevelParam = revitRoom.get_Parameter(BuiltInParameter.ROOM_LEVEL_ID);
      var profiles = GetProfiles(revitRoom);

      var speckleRoom = new Room();

      speckleRoom.type = revitRoom.get_Parameter(BuiltInParameter.ROOM_NAME).AsString();
      speckleRoom.number = revitRoom.Number;
      speckleRoom["basePoint"] = LocationToSpeckle(revitRoom);
      speckleRoom.level = (Level)ParameterToSpeckle(baseLevelParam);
      speckleRoom.baseGeometry = profiles[0];
      if (profiles.Count > 1)
        speckleRoom.holes = profiles.Skip(1).ToList();

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
