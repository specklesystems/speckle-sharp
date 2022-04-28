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
    public ApplicationPlaceholderObject OpeningToNative(BuiltElements.Opening speckleOpening)
    {
      var baseCurves = CurveToNative(speckleOpening.outline);

      var docObj = GetExistingElementByApplicationId(speckleOpening.applicationId);
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
            if (rwo.host == null) throw new SpeckleException($"Hosted wall openings require a host wall");
            Element existingElement;
            try
            {
              existingElement = GetExistingElementByApplicationId(rwo.host.applicationId);
            }
            catch (Exception e)
            {
              throw new SpeckleException($"Could not find the provided host wall by it's element id.", e);
            }

            if (!(existingElement is Wall wall))
              throw new SpeckleException($"The provided host element is not a wall.");
            
            hostElement = wall;
          }

          var poly = rwo.outline as Polyline;
          if (poly == null || !(poly.GetPoints().Count == 4 && poly.closed))
            throw new SpeckleException($"Curve outline for wall opening must be a rectangle-shaped polyline.");

          var points = poly.GetPoints().Select(PointToNative).ToList();
          revitOpening = Doc.Create.NewOpening((Wall)hostElement, points[0], points[2]);
          break;
        }

        case RevitVerticalOpening rvo:
        {
          if (CurrentHostElement == null)
            throw new SpeckleException($"Hosted vertical openings require a host family");
          revitOpening = Doc.Create.NewOpening(CurrentHostElement, baseCurves, true);
          break;
        }

        case RevitShaft rs:
        {
          var bottomLevel = ConvertLevelToRevit(rs.bottomLevel);
          var topLevel = ConvertLevelToRevit(rs.topLevel);
          revitOpening = Doc.Create.NewOpening(bottomLevel, topLevel, baseCurves);
          TrySetParam(revitOpening, BuiltInParameter.WALL_USER_HEIGHT_PARAM, rs.height, rs.units);

          break;
        }

        default:
          if (CurrentHostElement as Wall != null)
          {
            var speckleOpeningOutline = speckleOpening.outline as Polyline;
            if (speckleOpeningOutline == null)
              throw new SpeckleException("Cannot create opening, outline must be a rectangle-shaped polyline.");
            
            var points = speckleOpeningOutline.GetPoints().Select(PointToNative).ToList();
            revitOpening = Doc.Create.NewOpening(CurrentHostElement as Wall, points[0], points[2]);
          }
          else
          {
            Report.LogConversionError(new Exception("Cannot create Opening, opening type not supported"));
            throw new SpeckleException("Opening type not supported");
          }

          break;
      }

      if (speckleOpening is RevitOpening ro)
      {
        SetInstanceParameters(revitOpening, ro);
      }

      Report.Log($"Created Opening {revitOpening.Id}");
      return new ApplicationPlaceholderObject
      {
        NativeObject = revitOpening, applicationId = speckleOpening.applicationId,
        ApplicationGeneratedId = revitOpening.UniqueId
      };
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
        var btmLeft = PointToSpeckle(revitOpening.BoundaryRect[0]);
        var topRight = PointToSpeckle(revitOpening.BoundaryRect[1]);
        poly.value.AddRange(btmLeft.ToList());
        poly.value.AddRange(new Point(btmLeft.x, btmLeft.y, topRight.z, ModelUnits).ToList());
        poly.value.AddRange(topRight.ToList());
        poly.value.AddRange(new Point(topRight.x, topRight.y, btmLeft.z, ModelUnits).ToList());
        poly.value.AddRange(btmLeft.ToList());
        poly.units = ModelUnits;
        speckleOpening.outline = poly;
      }
      else
      {
        if (revitOpening.Host != null)
        {
          //we can ignore vertical openings because they will be created when we try re-create voids in the roof / ceiling / floor outline
          return null;
          //speckleOpening = new RevitVerticalOpening();
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
        {
          if (curve != null)
          {
            poly.segments.Add(CurveToSpeckle(curve));
          }
        }

        speckleOpening.outline = poly;
      }

      speckleOpening["type"] = revitOpening.Name;

      GetAllRevitParamsAndIds(speckleOpening, revitOpening,
        new List<string> { "WALL_BASE_CONSTRAINT", "WALL_HEIGHT_TYPE", "WALL_USER_HEIGHT_PARAM" });
      Report.Log($"Converted Opening {revitOpening.Id}");
      return speckleOpening;
    }
  }
}