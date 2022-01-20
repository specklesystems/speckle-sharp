using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;
using Curve = Objects.Geometry.Curve;
using Line = Objects.Geometry.Line;
using Polyline = Objects.Geometry.Polyline;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public List<ApplicationPlaceholderObject> DuctToNative(BuiltElements.Duct speckleDuct)
    {
      var speckleRevitDuct = speckleDuct as RevitDuct;
      var systemFamily = (speckleRevitDuct != null) ? speckleRevitDuct.systemName : "";
      List<ElementType> types = new FilteredElementCollector(Doc).WhereElementIsElementType()
          .OfClass(typeof(MechanicalSystemType)).ToElements().Cast<ElementType>().ToList();
      var system = types.FirstOrDefault(x => x.Name == systemFamily);
      if (system == null)
      {
        system = types.FirstOrDefault();
        Report.LogConversionError(new Exception($"Duct type {systemFamily} not found; replaced with {system.Name}"));
      }

      Element duct = null;
      if (speckleDuct.baseCurve == null || speckleDuct.baseCurve is Line)
      {
        var ductType = GetElementType<DuctType>(speckleDuct);

        DB.Line baseLine = (speckleDuct.baseCurve != null) ? LineToNative(speckleDuct.baseCurve as Line) : LineToNative(speckleDuct.baseLine);
        XYZ startPoint = baseLine.GetEndPoint(0);
        XYZ endPoint = baseLine.GetEndPoint(1);
        DB.Level lineLevel = LevelToNative(speckleRevitDuct != null ? speckleRevitDuct.level : LevelFromCurve(baseLine));
        DB.Mechanical.Duct lineDuct = DB.Mechanical.Duct.Create(Doc, system.Id, ductType.Id, lineLevel.Id, startPoint, endPoint);
        duct = lineDuct;
      }
      else if (speckleDuct.baseCurve is Polyline polyline)
      {
        var ductType = GetElementType<FlexDuctType>(speckleDuct);

        var speckleRevitFlexDuct = speckleDuct as RevitFlexDuct;
        var points = polyline.GetPoints().Select(o => PointToNative(o)).ToList();
        DB.Level flexLevel = LevelToNative(speckleRevitDuct != null ? speckleRevitDuct.level : LevelFromPoint(points.First()));
        var startTangent = VectorToNative(speckleRevitFlexDuct.startTangent);
        var endTangent = VectorToNative(speckleRevitFlexDuct.endTangent);

        DB.Mechanical.FlexDuct flexDuct = DB.Mechanical.FlexDuct.Create(Doc, system.Id, ductType.Id, flexLevel.Id, startTangent, endTangent, points);
        duct = flexDuct;
      }
      else
      {
        Report.LogConversionError(new Exception($"Duct BaseCurve of type ${speckleDuct.baseCurve.GetType()} cannot be used to create a Revit Duct"));
      }

      var docObj = GetExistingElementByApplicationId(((Base)speckleDuct).applicationId);

      // deleting instead of updating for now!
      if (docObj != null)
      {
        Doc.Delete(docObj.Id);
      }

      if (speckleRevitDuct != null)
      {
        TrySetParam(duct, BuiltInParameter.RBS_CURVE_HEIGHT_PARAM, speckleRevitDuct.height, speckleRevitDuct.units);
        TrySetParam(duct, BuiltInParameter.RBS_CURVE_WIDTH_PARAM, speckleRevitDuct.width, speckleRevitDuct.units);
        TrySetParam(duct, BuiltInParameter.RBS_CURVE_DIAMETER_PARAM, speckleRevitDuct.diameter, speckleRevitDuct.units);
        TrySetParam(duct, BuiltInParameter.CURVE_ELEM_LENGTH, speckleRevitDuct.length, speckleRevitDuct.units);
        TrySetParam(duct, BuiltInParameter.RBS_VELOCITY, speckleRevitDuct.velocity, speckleRevitDuct.units);

        SetInstanceParameters(duct, speckleRevitDuct);
      }

      var placeholders = new List<ApplicationPlaceholderObject>
      {
        new ApplicationPlaceholderObject
          {applicationId = speckleDuct.applicationId, ApplicationGeneratedId = duct.UniqueId, NativeObject = duct}
      };

      return placeholders;
    }

    public BuiltElements.Duct DuctToSpeckle(DB.Mechanical.Duct revitDuct)
    {
      var baseGeometry = LocationToSpeckle(revitDuct);
      if (!(baseGeometry is Line baseLine))
      {
        throw new Speckle.Core.Logging.SpeckleException("Only line based Ducts are currently supported.");
      }

      // SPECKLE DUCT
      var speckleDuct = new RevitDuct
      {
        family = revitDuct.DuctType.FamilyName,
        type = revitDuct.DuctType.Name,
        baseCurve = baseLine,
        diameter = GetParamValue<double>(revitDuct, BuiltInParameter.RBS_CURVE_DIAMETER_PARAM),
        height = GetParamValue<double>(revitDuct, BuiltInParameter.RBS_CURVE_HEIGHT_PARAM),
        width = GetParamValue<double>(revitDuct, BuiltInParameter.RBS_CURVE_WIDTH_PARAM),
        length = GetParamValue<double>(revitDuct, BuiltInParameter.CURVE_ELEM_LENGTH),
        velocity = GetParamValue<double>(revitDuct, BuiltInParameter.RBS_VELOCITY),
        level = ConvertAndCacheLevel(revitDuct, BuiltInParameter.RBS_START_LEVEL_PARAM),
        displayMesh = GetElementMesh(revitDuct)
      };

      var typeElem = Doc.GetElement(revitDuct.MEPSystem.GetTypeId());
      speckleDuct.systemName = typeElem.Name;

      GetAllRevitParamsAndIds(speckleDuct, revitDuct,
        new List<string>
        {
          "RBS_CURVE_HEIGHT_PARAM", "RBS_CURVE_WIDTH_PARAM", "RBS_CURVE_DIAMETER_PARAM", "CURVE_ELEM_LENGTH",
          "RBS_START_LEVEL_PARAM", "RBS_VELOCITY"
        });

      return speckleDuct;
    }

    public BuiltElements.Duct DuctToSpeckle(FlexDuct revitDuct)
    {
      // create polyline from revitduct points
      var polyline = new Polyline();
      polyline.value = PointsToFlatList(revitDuct.Points.Select(o => PointToSpeckle(o)));
      polyline.units = ModelUnits;
      polyline.closed = false;

      // SPECKLE DUCT
      var speckleDuct = new RevitFlexDuct
      {
        family = revitDuct.FlexDuctType.FamilyName,
        type = revitDuct.FlexDuctType.Name,
        baseCurve = polyline,
        diameter = GetParamValue<double>(revitDuct, BuiltInParameter.RBS_CURVE_DIAMETER_PARAM),
        height = GetParamValue<double>(revitDuct, BuiltInParameter.RBS_CURVE_HEIGHT_PARAM),
        width = GetParamValue<double>(revitDuct, BuiltInParameter.RBS_CURVE_WIDTH_PARAM),
        length = GetParamValue<double>(revitDuct, BuiltInParameter.CURVE_ELEM_LENGTH),
        startTangent = VectorToSpeckle(revitDuct.StartTangent),
        endTangent = VectorToSpeckle(revitDuct.EndTangent),
        velocity = GetParamValue<double>(revitDuct, BuiltInParameter.RBS_VELOCITY),
        level = ConvertAndCacheLevel(revitDuct, BuiltInParameter.RBS_START_LEVEL_PARAM),
        displayMesh = GetElementMesh(revitDuct)
      };

      var typeElem = Doc.GetElement(revitDuct.MEPSystem.GetTypeId());
      speckleDuct.systemName = typeElem.Name;

      GetAllRevitParamsAndIds(speckleDuct, revitDuct,
        new List<string>
        {
          "RBS_CURVE_HEIGHT_PARAM", "RBS_CURVE_WIDTH_PARAM", "RBS_CURVE_DIAMETER_PARAM", "CURVE_ELEM_LENGTH",
          "RBS_START_LEVEL_PARAM", "RBS_VELOCITY"
        });

      return speckleDuct;
    }
  }
}