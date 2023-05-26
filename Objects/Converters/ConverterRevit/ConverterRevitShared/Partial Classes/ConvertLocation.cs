using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;
using Line = Objects.Geometry.Line;
using Point = Objects.Geometry.Point;
using Wall = Objects.BuiltElements.Wall;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public Base LocationToSpeckle(DB.Element revitElement)
    {
      if (revitElement is DB.FamilyInstance familyInstance 
        && familyInstance.Location is LocationPoint lp
        && (Categories.columnCategories.Contains(familyInstance.Category)
          || familyInstance.StructuralType == StructuralType.Column))
      {
        //vertical columns are point based, and the point does not reflect the actual vertical location
        return TryGetColumnLocationAsCurve(familyInstance, lp);
      }

      var revitLocation = revitElement.Location;
      switch (revitLocation)
      {
        case LocationCurve locationCurve:
          {
            var curve = locationCurve.Curve;

            //apply revit offset as transfrom
            if (revitElement is DB.Wall)
            {
              var offset = revitElement.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).AsDouble();
              XYZ vector = new XYZ(0, 0, offset);
              Transform tf = Transform.CreateTranslation(vector);
              curve = curve.CreateTransformed(tf);
            }

            return CurveToSpeckle(curve, revitElement.Document) as Base;
          }
        case LocationPoint locationPoint:
          {
            return PointToSpeckle(locationPoint.Point, revitElement.Document);
          }
        // TODO what is the correct way to handle this?
        case null:
          return null;

        default:
          return null;
      }
    }

    /// <summary>
    /// Tries to to get the location of a column as a Curve
    /// </summary>
    /// <param name="familyInstance"></param>
    /// <param name="locationPoint"></param>
    /// <returns></returns>
    private Base TryGetColumnLocationAsCurve(DB.FamilyInstance familyInstance, LocationPoint locationPoint)
    {
      var point = PointToSpeckle(locationPoint.Point, familyInstance.Document);

      var baseOffset = GetParamValue<double>(familyInstance, BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
      var baseLevel = ConvertAndCacheLevel(familyInstance, BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
      var topOffset = GetParamValue<double>(familyInstance, BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);
      var topLevel = ConvertAndCacheLevel(familyInstance, BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);

      if (baseLevel == null || topLevel == null)
      {
        SpeckleLog.Logger.Error("Failed to get baseCurve from vertical column because the baseLevel or topLevel (or both) parameters were null");
        return point;
      }

      var baseLine = new Line(new[] { point.x, point.y, baseLevel.elevation + baseOffset, point.x, point.y, topLevel.elevation + topOffset }, ModelUnits);
      baseLine.length = Math.Abs(baseLine.start.z - baseLine.end.z);

      return baseLine;
    }

    /// <summary>
    /// Checks whether the curve is vertical or not.
    /// </summary>
    /// <param name="curve"></param>
    /// <returns></returns>
    private static bool IsVertical(DB.Curve curve)
    {
      var diffX = Math.Abs(curve.GetEndPoint(0).X - curve.GetEndPoint(1).X);
      var diffY = Math.Abs(curve.GetEndPoint(0).Y - curve.GetEndPoint(1).Y);

      if (diffX < 0.1 && diffY < 0.1)
        return true;

      return false;
    }
  }
}
