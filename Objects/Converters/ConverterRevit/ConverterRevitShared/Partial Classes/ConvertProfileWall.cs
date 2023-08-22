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
    public ApplicationObject ProfileWallToNative(RevitProfileWall speckleRevitWall)
    {
      var revitWall = GetExistingElementByApplicationId(speckleRevitWall.applicationId) as DB.Wall;
      var appObj = new ApplicationObject(speckleRevitWall.id, speckleRevitWall.speckle_type) { applicationId = speckleRevitWall.applicationId };

      // skip if element already exists in doc & receive mode is set to ignore
      if (IsIgnore(revitWall, appObj))
        return appObj;

      if (speckleRevitWall.profile == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Does not have a profile.");
        return appObj;
      }

      var wallType = GetElementType<WallType>(speckleRevitWall, appObj, out bool _);
      if (wallType == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed);
        return appObj;
      }

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
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Wall creation returned null");
        return appObj;
      }

      var level = ConvertLevelToRevit(speckleRevitWall.level, out ApplicationObject.State levelState);
      TrySetParam(revitWall, BuiltInParameter.WALL_BASE_CONSTRAINT, level);

      var offset = minZ - level.Elevation;
      TrySetParam(revitWall, BuiltInParameter.WALL_BASE_OFFSET, offset);

      if (revitWall.WallType.Name != wallType.Name)
        revitWall.ChangeTypeId(wallType.Id);

      SetInstanceParameters(revitWall, speckleRevitWall);

      appObj.Update(status: ApplicationObject.State.Created, createdId: revitWall.UniqueId, convertedItem: revitWall);
      appObj = SetHostedElements(speckleRevitWall, revitWall, appObj);
      return appObj;
    }
  }
}
