using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB;
using Point = Objects.Geometry.Point;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationPlaceholderObject AdaptiveComponentToNative(AdaptiveComponent speckleAc)
    {
      var docObj = GetExistingElementByApplicationId(speckleAc.applicationId);

      string familyName = speckleAc["family"] as string != null ? speckleAc["family"] as string : "";
      DB.FamilySymbol familySymbol = GetElementType<DB.FamilySymbol>(speckleAc);
      if (familySymbol.FamilyName != familyName)
      {
        Report.LogConversionError(new Exception($"Could not find adaptive component {familyName}"));
        return null;
      }

      DB.FamilyInstance revitAc = null;
      var isUpdate = false;
      //try update existing 
      if (docObj != null)
      {
        try
        {
          var revitType = Doc.GetElement(docObj.GetTypeId()) as ElementType;

          // if family changed, tough luck. delete and let us create a new one.
          if (familyName != revitType.FamilyName)
          {
            Doc.Delete(docObj.Id);
          }
          else
          {
            revitAc = (DB.FamilyInstance)docObj;

            // check for a type change
            if (speckleAc.type != null && speckleAc.type != revitType.Name)
              revitAc.ChangeTypeId(familySymbol.Id);
          }

          isUpdate = true;
        }
        catch
        {
          //something went wrong, re-create it
        }
      }

      //create family instance
      if (revitAc == null)
      {
        revitAc = AdaptiveComponentInstanceUtils.CreateAdaptiveComponentInstance(Doc, familySymbol);
      }

      SetAdaptivePoints(revitAc, speckleAc.basePoints);
      AdaptiveComponentInstanceUtils.SetInstanceFlipped(revitAc, speckleAc.flipped);

      SetInstanceParameters(revitAc, speckleAc);

      Report.Log($"Successfully {(isUpdate ? "updated" : "created")} AdaptiveComponent {revitAc.Id}");

      return new ApplicationPlaceholderObject { applicationId = speckleAc.applicationId, ApplicationGeneratedId = revitAc.UniqueId, NativeObject = revitAc };
    }

    private AdaptiveComponent AdaptiveComponentToSpeckle(DB.FamilyInstance revitAc)
    {
      var speckleAc = new AdaptiveComponent();
      speckleAc.family = revitAc.Document.GetElement(revitAc.GetTypeId()).Name;
      speckleAc.basePoints = GetAdaptivePoints(revitAc);
      speckleAc.flipped = AdaptiveComponentInstanceUtils.IsInstanceFlipped(revitAc);
      speckleAc.displayValue = GetElementMesh(revitAc);

      GetAllRevitParamsAndIds(speckleAc, revitAc);
      Report.Log($"Converted AdaptiveComponent {revitAc.Id}");
      return speckleAc;
    }

    private void SetAdaptivePoints(DB.FamilyInstance revitAc, List<Point> points)
    {
      var pointIds = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(revitAc).ToList();

      if (pointIds.Count != points.Count)
      {
        Report.LogConversionError(new Exception("Adaptive family error\nWrong number of points supplied to adaptive family"));
        return;
      }

      //set adaptive points
      for (int i = 0; i < pointIds.Count; i++)
      {
        var point = Doc.GetElement(pointIds[i]) as ReferencePoint;
        point.Position = PointToNative(points[i]);
      }
    }


    private List<Point> GetAdaptivePoints(DB.FamilyInstance revitAc)
    {
      var pointIds = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(revitAc).ToList();
      var points = new List<Point>();
      for (int i = 0; i < pointIds.Count; i++)
      {
        var point = revitAc.Document.GetElement(pointIds[i]) as ReferencePoint;
        points.Add(PointToSpeckle(point.Position));
      }
      return points;
    }
  }
}
