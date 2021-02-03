using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Objects.BuiltElements;
using Speckle.Core.Models;
using System;
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
      if (revitElement is FamilyInstance familyInstance)
      {
        //vertical columns are point based, and the point does not reflect the actual vertical location
        if (Categories.columnCategories.Contains(familyInstance.Category)
             || familyInstance.StructuralType == StructuralType.Column)
        {
          return TryGetLocationAsCurve(familyInstance);
        }
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

            return CurveToSpeckle(curve) as Base;
          }
        case LocationPoint locationPoint:
          {
            return PointToSpeckle(locationPoint.Point);
          }
        // TODO what is the correct way to handle this?
        case null:
          return null;

        default:
          return null;
      }
    }

    /// <summary>
    /// Tries to to get the location as a Curve
    /// </summary>
    /// <param name="loc"></param>
    /// <returns></returns>
    private Base TryGetLocationAsCurve(FamilyInstance familyInstance)
    {
      if (familyInstance.CanHaveAnalyticalModel())
      {
        //no need to apply offset transform
        var analiticalModel = familyInstance.GetAnalyticalModel();
        if (analiticalModel != null)
        {
          return CurveToSpeckle(analiticalModel.GetCurve()) as Base;
        }
      }
      var point = (familyInstance.Location as LocationPoint).Point;
      try
      {
        //apply offset tranform and create line
        var baseOffset = familyInstance.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM).AsDouble(); //keep internal units
        var topOffset = familyInstance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM).AsDouble(); //keep internal units
        var topLevel = (DB.Level)Doc.GetElement(familyInstance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM).AsElementId()); //keep internal units

        var baseLine = DB.Line.CreateBound(new XYZ(point.X, point.Y, point.Z + baseOffset), new XYZ(point.X, point.Y, topLevel.Elevation));

        return LineToSpeckle(baseLine);
      }
      catch { }
      //everything else failed, just retun the base point without moving it
      return PointToSpeckle(point);
    }

    //TODO: revise and improve
    private object LocationToNative(Base elem)
    {

      //no transforms are appliend on points

      if (elem["basePoint"] as Point != null)
      {
        return PointToNative(elem["basePoint"] as Point);
      }

      if (elem["baseLine"] == null)
      {
        throw new Exception("Location is null.");
      }

      //must be a curve!?
      var converted = GeometryToNative(elem["baseLine"] as Base);
      var curve = (converted as CurveArray).get_Item(0);
      //reapply revit's offset
      var offset = elem["baseOffset"] as double?;

      if (elem is Column)
      {
        //revit verical columns can only be POINT based
        if (!(bool)elem["isSlanted"] || IsVertical(curve))
        {
          var baseLine = elem["baseLine"] as Line;
          var point = new Point(baseLine.start.x, baseLine.start.y, baseLine.start.z - (double) offset, ModelUnits);

          return PointToNative(point);
        }
      }
      //undo offset transform
      else if (elem is Wall w)
      {
        var revitOffset = ScaleToNative((double)offset, ((Base)w.baseLine).units);
        XYZ vector = new XYZ(0, 0, -revitOffset);
        Transform tf = Transform.CreateTranslation(vector);
        curve = curve.CreateTransformed(tf);
      }

      return curve;
    }

    /// <summary>
    /// Checks whether the curve is vertical or not.
    /// </summary>
    /// <param name="curve"></param>
    /// <returns></returns>
    private bool IsVertical(DB.Curve curve)
    {
      var diffX = Math.Abs(curve.GetEndPoint(0).X - curve.GetEndPoint(1).X);
      var diffY = Math.Abs(curve.GetEndPoint(0).Y - curve.GetEndPoint(1).Y);

      if (diffX < 0.1 && diffY < 0.1)
      {
        return true;
      }

      return false;
    }
  }
}