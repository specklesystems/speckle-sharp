using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using Objects.Revit;
using System.Linq;
using Speckle.Core.Models;
using System.Collections.Generic;
using Point = Objects.Geometry.Point;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public DB.Element AdaptiveComponentToNative(AdaptiveComponent speckleAc)
    {
      var docObj = GetExistingElementByApplicationId(speckleAc.applicationId);

      string familyName = speckleAc["family"] as string != null ? speckleAc["family"] as string : "";
      
      DB.FamilySymbol familySymbol = GetElementType<DB.FamilySymbol>(speckleAc);
      DB.FamilyInstance revitAc = null;

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

      SetElementParamsFromSpeckle(revitAc, speckleAc);
      return revitAc;
    }

    private AdaptiveComponent AdaptiveComponentToSpeckle(DB.FamilyInstance revitAc)
    {
      var speckleAc = new AdaptiveComponent();
      speckleAc.type = Doc.GetElement(revitAc.GetTypeId()).Name;
      speckleAc.basePoints = GetAdaptivePoints(revitAc);
      speckleAc.flipped = AdaptiveComponentInstanceUtils.IsInstanceFlipped(revitAc);
      speckleAc["@displayMesh"] = GetElementMesh(revitAc);

      AddCommonRevitProps(speckleAc, revitAc);

      return speckleAc;
    }

    private void SetAdaptivePoints(DB.FamilyInstance revitAc, List<Point> points)
    {
      var pointIds = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(revitAc).ToList();

      if (pointIds.Count != points.Count)
      {
        ConversionErrors.Add(new Error("Adaptive family error", $"Wrong number of points supplied to adapive family"));
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
        var point = Doc.GetElement(pointIds[i]) as ReferencePoint;
        points.Add(PointToSpeckle(point.Position));
      }
      return points;
    }
  }
}
