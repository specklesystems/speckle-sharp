using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {

    public List<ApplicationPlaceholderObject> ProfileWallToNative(RevitProfileWall speckleRevitWall)
    {
      if (speckleRevitWall.profile == null)
      {
        throw new Speckle.Core.Logging.SpeckleException($"Failed to create wall ${speckleRevitWall.applicationId}. Profile Wall does not have a profile.");
      }

      var revitWall = GetExistingElementByApplicationId(speckleRevitWall.applicationId) as DB.Wall;

      var wallType = GetElementType<WallType>(speckleRevitWall);
      // Level level = null;
      var structural = speckleRevitWall.structural;
      var profile = new List<DB.Curve>();
      var minZ = double.MaxValue;
      for (var i = 0; i < CurveToNative(speckleRevitWall.profile).Size; i++)
      {
        var curve = CurveToNative(speckleRevitWall.profile).get_Item(i);
        profile.Add(curve);
        if (curve.GetEndPoint(0).Z < minZ)
          minZ = curve.GetEndPoint(0).Z;
        if (curve.GetEndPoint(1).Z < minZ)
          minZ = curve.GetEndPoint(1).Z;
      }

      //cannot update
      if (revitWall != null)
        Doc.Delete(revitWall.Id);

      revitWall = DB.Wall.Create(Doc, profile, structural);


      if (revitWall == null)
      {
        throw (new Exception($"Failed to create wall ${speckleRevitWall.applicationId}."));
      }

      var level = ConvertLevelToRevit(speckleRevitWall.level);
      TrySetParam(revitWall, BuiltInParameter.WALL_BASE_CONSTRAINT, level);

      var offset = minZ - level.Elevation;
      TrySetParam(revitWall, BuiltInParameter.WALL_BASE_OFFSET, offset);


      if (revitWall.WallType.Name != wallType.Name)
      {
        revitWall.ChangeTypeId(wallType.Id);
      }


      SetInstanceParameters(revitWall, speckleRevitWall);

      var placeholders = new List<ApplicationPlaceholderObject>()
      {
        new ApplicationPlaceholderObject
        {
        applicationId = speckleRevitWall.applicationId,
        ApplicationGeneratedId = revitWall.UniqueId,
        NativeObject = revitWall
        }
      };

      var hostedElements = SetHostedElements(speckleRevitWall, revitWall);
      placeholders.AddRange(hostedElements);
      Report.Log($"Created ProfileWall {revitWall.Id}");
      return placeholders;
    }



  }
}
