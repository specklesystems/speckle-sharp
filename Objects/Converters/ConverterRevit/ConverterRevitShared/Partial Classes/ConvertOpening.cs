using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;
using Point = Objects.Geometry.Point;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationObject OpeningToNative(BuiltElements.Opening speckleOpening)
    {
      var baseCurves = CurveToNative(speckleOpening.outline);

      var docObj = GetExistingElementByApplicationId(speckleOpening.applicationId);
      var appObj = new ApplicationObject(speckleOpening.id, speckleOpening.speckle_type) { applicationId = speckleOpening.applicationId };

      // skip if element already exists in doc & receive mode is set to ignore
      if (IsIgnore(docObj, appObj, out appObj))
        return appObj;

      if (docObj != null)
        Doc.Delete(docObj.Id);

      Opening revitOpening = null;

      switch (speckleOpening)
      {
        case RevitWallOpening rwo:
          {
            // Prevent host element overriding as this will propagate upwards to other hosted elements in a wall :)
            string elementId = null;
            var hostElement = CurrentHostElement;
            if (!(hostElement is Wall))
            {
              // Try with the opening wall if it exists
              if (rwo.host == null)
              {
                appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Host wall was null");
                return appObj;
              }
              Element existingElement;
              try
              {
                existingElement = GetExistingElementByApplicationId(rwo.host.applicationId);
              }
              catch (Exception e)
              {
                appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Could not find the host wall: {e.Message}");
                return appObj;
              }

              if (!(existingElement is Wall wall))
              {
                appObj.Update(status: ApplicationObject.State.Failed, logItem: $"The host is not a wall");
                return appObj;
              }

              hostElement = wall;
            }

            var poly = rwo.outline as Polyline;
            if (poly == null || !((poly.GetPoints().Count == 5 && poly.closed) || (poly.GetPoints().Count == 4 && !poly.closed)))
            {
              appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Curve outline for wall opening must be a rectangle-shaped polyline");
              return appObj;
            }

            var points = poly.GetPoints().Select(PointToNative).ToList();
            revitOpening = Doc.Create.NewOpening((Wall)hostElement, points[0], points[2]);
            break;
          }

        case RevitVerticalOpening rvo:
          {
            if (CurrentHostElement == null)
            {
              appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Hosted vertical openings require a host family");
              return appObj;
            }
            revitOpening = Doc.Create.NewOpening(CurrentHostElement, baseCurves, true);
            break;
          }

        case RevitShaft rs:
          {
            var bottomLevel = ConvertLevelToRevit(rs.bottomLevel, out ApplicationObject.State bottomState);
            var topLevel = ConvertLevelToRevit(rs.topLevel, out ApplicationObject.State topState);
            revitOpening = Doc.Create.NewOpening(bottomLevel, topLevel, baseCurves);
            TrySetParam(revitOpening, BuiltInParameter.WALL_USER_HEIGHT_PARAM, rs.height, rs.units);

            break;
          }

        default:
          if (CurrentHostElement as Wall != null)
          {
            var speckleOpeningOutline = speckleOpening.outline as Polyline;
            if (speckleOpeningOutline == null)
            {
              appObj.Update(status: ApplicationObject.State.Failed, logItem: "Outline must be a rectangle-shaped polyline");
              return appObj;
            }
            var points = speckleOpeningOutline.GetPoints().Select(PointToNative).ToList();
            revitOpening = Doc.Create.NewOpening(CurrentHostElement as Wall, points[0], points[2]);
          }
          else
          {
            appObj.Update(status: ApplicationObject.State.Failed, logItem: "Opening type not supported");
            return appObj;
          }
          break;
      }

      if (speckleOpening is RevitOpening ro)
        SetInstanceParameters(revitOpening, ro);

      appObj.Update(status: ApplicationObject.State.Created, createdId: revitOpening.UniqueId, convertedItem: revitOpening);
      return appObj;
    }

    public BuiltElements.Opening OpeningToSpeckle(DB.Opening revitOpening)
    {
      if (!ShouldConvertHostedElement(revitOpening, revitOpening.Host))
        return null;

      RevitOpening speckleOpening;
      if (revitOpening.IsRectBoundary)
      {
        speckleOpening = new RevitWallOpening();

        var poly = new Polyline();
        poly.value = new List<double>();

        //2 points: bottom left and top right
        var btmLeft = PointToSpeckle(revitOpening.BoundaryRect[0], revitOpening.Document);
        var topRight = PointToSpeckle(revitOpening.BoundaryRect[1], revitOpening.Document);
        poly.value.AddRange(btmLeft.ToList());
        poly.value.AddRange(new Point(btmLeft.x, btmLeft.y, topRight.z, ModelUnits).ToList());
        poly.value.AddRange(topRight.ToList());
        poly.value.AddRange(new Point(topRight.x, topRight.y, btmLeft.z, ModelUnits).ToList());

        poly.value.AddRange(btmLeft.ToList());
        // setting closed to true because we added the first point again.
        poly.closed = true;
        poly.units = ModelUnits;
        speckleOpening.outline = poly;
      }
      else
      {
        if (revitOpening.Host != null)
        {
          //we can ignore vertical openings because they will be created when we try re-create voids in the roof / ceiling / floor outline
          return null;
        }
        else
        {
          speckleOpening = new RevitShaft();
          if (revitOpening.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE) != null)
          {
            ((RevitShaft)speckleOpening).topLevel =
              ConvertAndCacheLevel(revitOpening, BuiltInParameter.WALL_HEIGHT_TYPE);
            ((RevitShaft)speckleOpening).bottomLevel =
              ConvertAndCacheLevel(revitOpening, BuiltInParameter.WALL_BASE_CONSTRAINT);
            ((RevitShaft)speckleOpening).height =
              GetParamValue<double>(revitOpening, BuiltInParameter.WALL_USER_HEIGHT_PARAM);
          }
        }

        var poly = new Polycurve(ModelUnits);
        poly.segments = new List<ICurve>();
        foreach (DB.Curve curve in revitOpening.BoundaryCurves)
          if (curve != null)
            poly.segments.Add(CurveToSpeckle(curve, revitOpening.Document));

        speckleOpening.outline = poly;
      }

      speckleOpening["type"] = revitOpening.Name;

      GetAllRevitParamsAndIds(speckleOpening, revitOpening,
        new List<string> { "WALL_BASE_CONSTRAINT", "WALL_HEIGHT_TYPE", "WALL_USER_HEIGHT_PARAM" });
      return speckleOpening;
    }
  }
}
