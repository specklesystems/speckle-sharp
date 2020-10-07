using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using Element = Objects.Element;
using Objects.Revit;
using System.Linq;
using Objects.Geometry;
using System;
using Objects;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public DB.Element AdaptiveComponentToNative(AdaptiveComponent speckleAc)
    {
      var (docObj, stateObj) = GetExistingElementByApplicationId(speckleAc.applicationId, speckleAc.type);

      string familyName = speckleAc.GetMemberSafe("family", "");
      DB.FamilySymbol familySymbol = GetFamilySymbol(speckleAc);
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

      SetAdaptivePoints(revitAc, speckleAc.baseGeometry as Polyline);
      AdaptiveComponentInstanceUtils.SetInstanceFlipped(revitAc, speckleAc.flipped);

      SetElementParams(revitAc, speckleAc);
      return revitAc;
    }

    private Element AdaptiveComponentToSpeckle(DB.FamilyInstance revitAc)
    {
      var speckleAc = new AdaptiveComponent();
      speckleAc.type = Doc.GetElement(revitAc.GetTypeId()).Name;
      speckleAc.baseGeometry = GetAdaptivePoints(revitAc);
      speckleAc.flipped = AdaptiveComponentInstanceUtils.IsInstanceFlipped(revitAc);
      speckleAc.displayMesh = MeshUtils.GetElementMesh(revitAc, Scale);

      AddCommonRevitProps(speckleAc, revitAc);

      return speckleAc;
    }

    private void SetAdaptivePoints(DB.FamilyInstance revitAc, Polyline poly)
    {
      var pointIds = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(revitAc).ToList();
      var specklePoints = poly.points;

      if (pointIds.Count != specklePoints.Count)
      {
        ConversionErrors.Add(new Error ("Adaptive family error", $"Wrong number of points supplied to adapive family" ));
        return;
      }

      //set adaptive points
      for (int i = 0; i < pointIds.Count; i++)
      {
        var point = Doc.GetElement(pointIds[i]) as ReferencePoint;
        point.Position = PointToNative(specklePoints[i]);
      }
    }


    private Polyline GetAdaptivePoints(DB.FamilyInstance revitAc)
    {
      var pointIds = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(revitAc).ToList();
      var poly = new Polyline();
      for (int i = 0; i < pointIds.Count; i++)
      {
        var point = Doc.GetElement(pointIds[i]) as ReferencePoint;
        poly.value.AddRange(PointToSpeckle(point.Position).value);
      }
      return poly;
    }
  }
}
