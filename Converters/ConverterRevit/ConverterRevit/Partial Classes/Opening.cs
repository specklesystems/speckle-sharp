using Objects;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Text;
using Opening = Objects.Opening;
using Element = Objects.Element;
using Level = Objects.Level;
using Mesh = Objects.Geometry.Mesh;
using Autodesk.Revit.DB.Structure;
using Objects.Geometry;
using Point = Objects.Geometry.Point;
using Objects.Revit;
using System.Linq;
using Speckle.Core.Models;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public DB.Opening OpeningToNative(Opening speckleOpening)
    {
      var (docObj, stateObj) = GetExistingElementByApplicationId(speckleOpening.applicationId, speckleOpening.type);

      var baseCurves = CurveToNative(speckleOpening.baseGeometry as ICurve);

      if (docObj != null)
        Doc.Delete(docObj.Id);

      DB.Opening revitOpeneing = null;

      //wall opening (could also check if the host is a wall)
      if (speckleOpening is RevitWallOpening && speckleOpening.HasMember<int>("revitHostId"))
      {
        var points = (speckleOpening.baseGeometry as Polyline).points.Select(x => PointToNative(x)).ToList();
        var host = Doc.GetElement(new ElementId((int)speckleOpening["revitHostId"]));
        revitOpeneing = Doc.Create.NewOpening(host as DB.Wall, points[0], points[2]);
      }
      //vertical opening
      else if (speckleOpening.HasMember<int>("revitHostId"))
      {
        var host = Doc.GetElement(new ElementId((int)speckleOpening["revitHostId"]));
        revitOpeneing = Doc.Create.NewOpening(host, baseCurves, true);
      }
      //shaft opening
      else if (speckleOpening.level != null)
      {
        var bottomLevel = LevelToNative(speckleOpening.level);
        var topLevel = speckleOpening.HasMember<Level>("topLevel") ? LevelToNative(speckleOpening["topLevel"] as Level) : null;
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

      Opening speckleOpening = null;




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
        speckleOpening.baseGeometry = poly;

      }
      else
      {
        speckleOpening = new Opening(); //either a VerticalOpening or a ShaftOpening

        var poly = new Polycurve();
        poly.segments = new List<ICurve>();
        foreach (DB.Curve curve in revitOpening.BoundaryCurves)
        {
          if (curve != null)
            poly.segments.Add(CurveToSpeckle(curve));
        }
        speckleOpening.baseGeometry = poly;
      }

      if (baseLevelParam != null)
        speckleOpening.level = (Level)ParameterToSpeckle(baseLevelParam);
      if (topLevelParam != null)
        speckleOpening["topLevel"] = (Level)ParameterToSpeckle(topLevelParam);
      speckleOpening.type = revitOpening.Name;

      AddCommonRevitProps(speckleOpening, revitOpening);

      return speckleOpening;
    }




  }
}
