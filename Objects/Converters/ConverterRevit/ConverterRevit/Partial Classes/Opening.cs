using Autodesk.Revit.DB;
using Objects.Geometry;
using Objects.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

using DB = Autodesk.Revit.DB;

using Level = Objects.BuiltElements.Level;
using Opening = Objects.BuiltElements.Opening;
using Point = Objects.Geometry.Point;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public DB.Opening OpeningToNative(RevitOpening speckleOpening)
    {
      var (docObj, stateObj) = GetExistingElementByApplicationId(speckleOpening.applicationId, speckleOpening.type);

      var baseCurves = CurveToNative(speckleOpening.outline);

      if (docObj != null)
        Doc.Delete(docObj.Id);

      DB.Opening revitOpeneing = null;

      //wall opening (could also check if the host is a wall)
      if (speckleOpening is RevitWallOpening rwo)
      {
        var points = rwo.outline.points.Select(x => PointToNative(x)).ToList();
        var host = Doc.GetElement(new ElementId(rwo.revitHostId));
        revitOpeneing = Doc.Create.NewOpening(host as DB.Wall, points[0], points[2]);
      }
      //vertical opening
      else if (speckleOpening is RevitVerticalOpening rvo)
      {
        var host = Doc.GetElement(new ElementId(rvo.revitHostId));
        revitOpeneing = Doc.Create.NewOpening(host, baseCurves, true);
      }
      //shaft opening
      else if (speckleOpening is RevitShaft rs)
      {
        var bottomLevel = LevelToNative(rs.level);
        var topLevel = rs.level != null ? LevelToNative(rs.topLevel) : null;
        revitOpeneing = Doc.Create.NewOpening(bottomLevel, topLevel, baseCurves);
      }
      else
      {
        ConversionErrors.Add(new Error("Cannot create Opening", "Does not satisfy requrements to create a Revit Opening"));
        throw new Exception("Cannot create Opening");
      }

      SetElementParams(revitOpeneing, speckleOpening);

      return revitOpeneing;
    }

    public Opening OpeningToSpeckle(DB.Opening revitOpening)
    {
      //REVIT PARAMS > SPECKLE PROPS
      var baseLevelParam = revitOpening.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT);
      var topLevelParam = revitOpening.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);

      RevitOpening speckleOpening = null;

      if (revitOpening.IsRectBoundary)
      {
        speckleOpening = new RevitWallOpening();

        var poly = new Polyline();
        poly.value = new List<double>();

        //2 points: bottom left and top right
        var btmLeft = PointToSpeckle(revitOpening.BoundaryRect[0]);
        var topRight = PointToSpeckle(revitOpening.BoundaryRect[1]);
        poly.value.AddRange(btmLeft.value);
        poly.value.AddRange(new Point(btmLeft.value[0], btmLeft.value[1], topRight.value[2]).value);
        poly.value.AddRange(topRight.value);
        poly.value.AddRange(new Point(topRight.value[0], topRight.value[1], btmLeft.value[2]).value);
        poly.value.AddRange(btmLeft.value);
        speckleOpening.outline = poly;
      }
      else
      {//TODO: check it works!!!
        if (revitOpening.Host != null)
          speckleOpening = new RevitVerticalOpening();
        else
          speckleOpening = new RevitShaft();

        var poly = new Polycurve();
        poly.segments = new List<ICurve>();
        foreach (DB.Curve curve in revitOpening.BoundaryCurves)
        {
          if (curve != null)
            poly.segments.Add(CurveToSpeckle(curve));
        }
        speckleOpening.outline = poly;
      }

      if (baseLevelParam != null)
        speckleOpening.level = (RevitLevel)ParameterToSpeckle(baseLevelParam);
      if (topLevelParam != null)
        speckleOpening["topLevel"] = (RevitLevel)ParameterToSpeckle(topLevelParam);
      speckleOpening.type = revitOpening.Name;

      AddCommonRevitProps(speckleOpening, revitOpening);

      return speckleOpening;
    }
  }
}