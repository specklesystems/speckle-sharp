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

    public List<ApplicationObject> ProfileWallToNative(RevitProfileWall speckleRevitWall)
    {
      var appObj = new ApplicationObject(speckleRevitWall.id, speckleRevitWall.speckle_type) { applicationId = speckleRevitWall.applicationId };

      if (speckleRevitWall.profile == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Does not have a profile.");
        return new List<ApplicationObject> { appObj };
      }

      var revitWall = GetExistingElementByApplicationId(speckleRevitWall.applicationId) as DB.Wall;
      if (revitWall != null && ReceiveMode == Speckle.Core.Kits.ReceiveMode.Ignore)
      {
        appObj.Update(status: ApplicationObject.State.Skipped, createdId: revitWall.UniqueId, existingObject: revitWall);
        return new List<ApplicationObject> { appObj };
      }

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
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Revit wall was null");
        return new List<ApplicationObject> { appObj };
      }

      var level = ConvertLevelToRevit(speckleRevitWall.level, out ApplicationObject.State levelState);
      TrySetParam(revitWall, BuiltInParameter.WALL_BASE_CONSTRAINT, level);

      var offset = minZ - level.Elevation;
      TrySetParam(revitWall, BuiltInParameter.WALL_BASE_OFFSET, offset);

      if (revitWall.WallType.Name != wallType.Name)
        revitWall.ChangeTypeId(wallType.Id);

      SetInstanceParameters(revitWall, speckleRevitWall);

      appObj.Update(status: ApplicationObject.State.Created, createdId: revitWall.UniqueId, existingObject: revitWall);
      var placeholders = new List<ApplicationObject>() { appObj };

      var hostedElements = SetHostedElements(speckleRevitWall, revitWall);
      placeholders.AddRange(hostedElements);

      return placeholders;
    }
  }
}
