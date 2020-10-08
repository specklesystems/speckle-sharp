using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using Objects.Geometry;
using System;
using System.Collections.Generic;
using System.Text;
using Line = Objects.Geometry.Line;
using Element = Objects.Element;
using System.Linq;
using Objects;
using Point = Objects.Geometry.Point;
using Curve = Objects.Geometry.Curve;
using Autodesk.Revit.DB.Structure;
using Wall = Objects.Wall;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public IGeometry LocationToSpeckle(DB.Element revitElement)
    {
      if (revitElement.Location is LocationCurve)
      {
        var curve = (revitElement.Location as LocationCurve).Curve;

        //apply revit offset as transfrom
        if (revitElement is DB.Wall)
        {
          var offset = revitElement.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).AsDouble();
          XYZ vector = new XYZ(0, 0, offset);
          Transform tf = Transform.CreateTranslation(vector);
          curve = curve.CreateTransformed(tf);
        }

        return CurveToSpeckle(curve);
      }

      if (revitElement is DB.FamilyInstance)
      {
        var familyInstance = revitElement as FamilyInstance;
        //vertical columns are point based, and the point does not reflect the actual vertical location 
        if (Categories.columnCategories.Contains(familyInstance.Category)
          || familyInstance.StructuralType == StructuralType.Column)
        {
          return TryGetLocationAsCurve(familyInstance);
        }
      }

      var point = (revitElement.Location as LocationPoint).Point;
      return PointToSpeckle(point);

    }

    /// <summary>
    /// Tries to to get the location as a Curve
    /// </summary>
    /// <param name="loc"></param>
    /// <returns></returns>
    private IGeometry TryGetLocationAsCurve(FamilyInstance familyInstance)
    {
      if (familyInstance.CanHaveAnalyticalModel())
      {
        //no need to apply offset transform
        var analiticalModel = familyInstance.GetAnalyticalModel();
        if (analiticalModel != null)
          return CurveToSpeckle(analiticalModel.GetCurve());
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


    private object LocationToNative(Element elem)
    {
      if (elem.baseGeometry == null)
        throw new Exception("Location is null.");

      //no transforms are appliend on points

      if (elem.baseGeometry is Point)
      {
        return PointToNative(elem.baseGeometry as Point);
      }

      //must be a curve!?
      var converted = GeometryToNative(elem.baseGeometry);
      var curve = (converted as CurveArray).get_Item(0);
      //reapply revit's offset
      var offset = elem.GetMemberSafe<double>("baseOffset");
      
      
      if (elem is Column)
      {
        //revit verical columns can only be POINT based
        if (!elem.GetMemberSafe<bool>("isSlanted") || IsVertical(curve))
        {
          var baseLine = elem.baseGeometry as Line;
          var point = new Point(baseLine.value[0], baseLine.value[1], baseLine.value[3] - offset);

          return PointToNative(point);
        }
      }
      //undo offset transform
      else if (elem is Wall)
      {
        var revitOffset = offset * Scale;
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
