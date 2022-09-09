using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Objects.BuiltElements;
using Speckle.Core.Models;
using System;
using Objects.BuiltElements.Revit;
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
      if (revitElement is DB.FamilyInstance familyInstance)
      {
        //vertical columns are point based, and the point does not reflect the actual vertical location
        if (Categories.columnCategories.Contains(familyInstance.Category) ||
          familyInstance.StructuralType == StructuralType.Column)
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
    private Base TryGetLocationAsCurve(DB.FamilyInstance familyInstance)
    {
#if !REVIT2023
      if (familyInstance.CanHaveAnalyticalModel())
      {
        //no need to apply offset transform
        var analyticalModel = familyInstance.GetAnalyticalModel();
        if (analyticalModel != null && analyticalModel.GetCurve() != null)
          return CurveToSpeckle(analyticalModel.GetCurve()) as Base;
      }

#else
      var manager = AnalyticalToPhysicalAssociationManager.GetAnalyticalToPhysicalAssociationManager(Doc);

      if (manager.HasAssociation(familyInstance.Id))
      {
        var analyticalModel = Doc.GetElement(AnalyticalToPhysicalAssociationManager.GetAnalyticalToPhysicalAssociationManager(Doc).GetAssociatedElementId(familyInstance.Id)) as AnalyticalMember;
        //no need to apply offset transform
        if (analyticalModel != null && analyticalModel.GetCurve() != null)
          return CurveToSpeckle(analyticalModel.GetCurve()) as Base;
      }
#endif
      Point point = familyInstance.Location switch
      {
        LocationPoint p => PointToSpeckle(p.Point),
        LocationCurve c => PointToSpeckle(c.Curve.GetEndPoint(0)),
        _ => null,
      };

      try
      {
        //apply offset transform and create line
        var baseOffset = GetParamValue<double>(familyInstance, BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
        var baseLevel = ConvertAndCacheLevel(familyInstance, BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
        var topOffset = GetParamValue<double>(familyInstance, BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);
        var topLevel = ConvertAndCacheLevel(familyInstance, BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);

        var baseLine = new Line(new[] { point.x, point.y, baseLevel.elevation + baseOffset, point.x, point.y, topLevel.elevation + topOffset }, ModelUnits);
        baseLine.length = Math.Abs(baseLine.start.z - baseLine.end.z);

        return baseLine;
      }
      catch { }
      //everything else failed, just return the base point without moving it
      return point;
    }

    //TODO: revise and improve
    private object LocationToNative(Base elem)
    {

      //no transforms are applied on points

      if (elem["basePoint"] as Point != null)
        return PointToNative(elem["basePoint"] as Point);

      if (elem["baseLine"] == null)
        throw new Speckle.Core.Logging.SpeckleException("Location is null.");

      //must be a curve!?
      var converted = GeometryToNative(elem["baseLine"] as Base);
      var curve = (converted as CurveArray).get_Item(0);
      //reapply revit's offset
      var offset = elem["baseOffset"] as double?;

      if (elem is Column)
      {
        //revit vertical columns can only be POINT based
        if (!(bool)elem["isSlanted"] || IsVertical(curve))
        {
          var baseLine = elem["baseLine"] as Line;
          var point = new Point(baseLine.start.x, baseLine.start.y, baseLine.start.z - (double)offset, ModelUnits);

          return PointToNative(point);
        }
      }
      //undo offset transform
      else if (elem is Wall w)
      {
        var revitOffset = ScaleToNative((double)offset, ((Base)w.baseLine)["units"] as string);
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
        return true;

      return false;
    }
  }
}
