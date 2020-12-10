using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
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
    public ApplicationPlaceholderObject OpeningToNative(BuiltElements.Opening speckleOpening)
    {


      var baseCurves = CurveToNative(speckleOpening.outline);

      var docObj = GetExistingElementByApplicationId(((Base)speckleOpening).applicationId);
      if (docObj != null)
      {
        Doc.Delete(docObj.Id);
      }

      DB.Opening revitOpening = null;

      switch (speckleOpening)
      {
        case RevitWallOpening rwo:
          {
            var points = (rwo.outline as Polyline).points.Select(x => PointToNative(x)).ToList();
            revitOpening = Doc.Create.NewOpening(CurrentHostElement as Wall, points[0], points[2]);
            break;
          }

        case RevitVerticalOpening rvo:
          {
            revitOpening = Doc.Create.NewOpening(CurrentHostElement, baseCurves, true);
            break;
          }

        case RevitShaft rs:
          {
            var bottomLevel = LevelToNative(rs.bottomLevel);
            var topLevel = LevelToNative(rs.topLevel);
            revitOpening = Doc.Create.NewOpening(bottomLevel, topLevel, baseCurves);
            break;
          }

        default:
          ConversionErrors.Add(new Error("Cannot create Opening", "Opening type not supported"));
          throw new Exception("Opening type not supported");
      }


      if (speckleOpening is RevitOpening ro)
      {
        SetInstanceParameters(revitOpening, ro);
      }

      return new ApplicationPlaceholderObject { NativeObject = revitOpening, applicationId = speckleOpening.applicationId, ApplicationGeneratedId = revitOpening.UniqueId };
    }

    public BuiltElements.Opening OpeningToSpeckle(DB.Opening revitOpening)
    {
      #region host handling

      // Check if it's been converted previously - from a parent host.
      if (ConvertedObjectsList.IndexOf(revitOpening.UniqueId) != -1)
      {
        return null;
      }

      // If the parent is in our selection list, back off, as this element will be converted by the host element.
      if (revitOpening.Host != null && ContextObjects.FindIndex(obj => obj.applicationId == revitOpening.Host.UniqueId) != -1)
      {
        return null;
      }

      #endregion

      //REVIT PARAMS > SPECKLE PROPS
      var baseLevelParam = revitOpening.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT);
      var topLevelParam = revitOpening.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);
      var heightParam = revitOpening.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);

      RevitOpening speckleOpening;
      if (revitOpening.IsRectBoundary)
      {
        speckleOpening = new RevitWallOpening();

        var poly = new Polyline();
        poly.value = new List<double>();

        //2 points: bottom left and top right
        var btmLeft = PointToSpeckle(revitOpening.BoundaryRect[0]);
        var topRight = PointToSpeckle(revitOpening.BoundaryRect[1]);
        poly.value.AddRange(btmLeft.value);
        poly.value.AddRange(new Point(btmLeft.value[0], btmLeft.value[1], topRight.value[2], ModelUnits).value);
        poly.value.AddRange(topRight.value);
        poly.value.AddRange(new Point(topRight.value[0], topRight.value[1], btmLeft.value[2], ModelUnits).value);
        poly.value.AddRange(btmLeft.value);
        poly.units = ModelUnits;
        speckleOpening.outline = poly;
      }
      else
      {
        if (revitOpening.Host != null)
        {
          speckleOpening = new RevitVerticalOpening();
        }
        else
        {
          speckleOpening = new RevitShaft();
          if (topLevelParam != null)
          {
            ((RevitShaft)speckleOpening).topLevel = ConvertAndCacheLevel(topLevelParam);
            ((RevitShaft)speckleOpening).bottomLevel = ConvertAndCacheLevel(baseLevelParam);
            ((RevitShaft)speckleOpening).height = (double)ParameterToSpeckle(heightParam);
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

      //speckleOpening.type = revitOpening.Name;

      AddCommonRevitProps(speckleOpening, revitOpening);

      return speckleOpening;
    }
  }
}