using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;
using Point = Objects.Geometry.Point;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationObject AdaptiveComponentToNative(AdaptiveComponent speckleAc)
    {
      var docObj = GetExistingElementByApplicationId(speckleAc.applicationId);
      var appObj = new ApplicationObject(speckleAc.id, speckleAc.speckle_type) { applicationId = speckleAc.applicationId };

      // skip if element already exists in doc & receive mode is set to ignore
      if (IsIgnore(docObj, appObj, out appObj))
        return appObj;

      string familyName = speckleAc["family"] as string != null ? speckleAc["family"] as string : "";
      if (!GetElementType<FamilySymbol>(speckleAc, appObj, out FamilySymbol familySymbol))
      {
        appObj.Update(status: ApplicationObject.State.Failed);
        return appObj;
      }
      if (familySymbol.FamilyName != familyName)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Could not find adaptive component {familyName}");
        return appObj;
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
            Doc.Delete(docObj.Id);

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
        revitAc = AdaptiveComponentInstanceUtils.CreateAdaptiveComponentInstance(Doc, familySymbol);

      SetAdaptivePoints(revitAc, speckleAc.basePoints, out List<string> notes);
      AdaptiveComponentInstanceUtils.SetInstanceFlipped(revitAc, speckleAc.flipped);

      SetInstanceParameters(revitAc, speckleAc);
      var state = isUpdate ? ApplicationObject.State.Updated : ApplicationObject.State.Created;
      appObj.Update(status: state, createdId: revitAc.UniqueId, convertedItem: revitAc, log: notes);
      return appObj;
    }

    private AdaptiveComponent AdaptiveComponentToSpeckle(DB.FamilyInstance revitAc)
    {
      var speckleAc = new AdaptiveComponent();

      var symbol = revitAc.Document.GetElement(revitAc.GetTypeId()) as FamilySymbol;

      speckleAc.family = symbol.FamilyName;
      speckleAc.type = revitAc.Document.GetElement(revitAc.GetTypeId()).Name;

      speckleAc.basePoints = GetAdaptivePoints(revitAc);
      speckleAc.flipped = AdaptiveComponentInstanceUtils.IsInstanceFlipped(revitAc);
      speckleAc.displayValue = GetElementMesh(revitAc);

      GetAllRevitParamsAndIds(speckleAc, revitAc);
      Report.Log($"Converted AdaptiveComponent {revitAc.Id}");
      return speckleAc;
    }

    private void SetAdaptivePoints(DB.FamilyInstance revitAc, List<Point> points, out List<string> notes)
    {
      notes = new List<string>();
      var pointIds = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(revitAc).ToList();

      if (pointIds.Count != points.Count)
      {
        notes.Add("Adaptive family error: wrong number of points supplied");
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
        points.Add(PointToSpeckle(point.Position, revitAc.Document));
      }
      return points;
    }
  }
}
