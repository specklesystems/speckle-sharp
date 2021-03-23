using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB.Mechanical;
using Line = Objects.Geometry.Line;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public List<ApplicationPlaceholderObject> DuctToNative(BuiltElements.Duct speckleDuct)
    {
      var baseLine = LineToNative(speckleDuct.baseLine);
      XYZ startPoint = baseLine.GetEndPoint(0);
      XYZ endPoint = baseLine.GetEndPoint(1);

      Autodesk.Revit.DB.Level level = null;

      var speckleRevitDuct = speckleDuct as RevitDuct;
      if (speckleRevitDuct != null)
      {
        level = LevelToNative(speckleRevitDuct.level);
      }
      else
      {
        level = LevelToNative(LevelFromCurve(baseLine));
      }

      var ductType = GetElementType<DB.DuctType>(speckleDuct);
      var systemFamily = (speckleDuct is RevitDuct rd) ? rd.systemName : "";

      List<ElementType> types = new FilteredElementCollector(Doc).WhereElementIsElementType().OfClass(typeof(MechanicalSystemType)).ToElements().Cast<ElementType>().ToList();
      var system = types.FirstOrDefault(x => x.Name == systemFamily);
      if (system == null)
      {
        system = types.FirstOrDefault();
        ConversionErrors.Add(new Exception($"Duct type {systemFamily} not found; replaced with {system.Name}"));
      }

      var docObj = GetExistingElementByApplicationId(((Base)speckleDuct).applicationId);

      // deleting instead of updating for now!
      if (docObj != null)
      {
        Doc.Delete(docObj.Id);
      }

      DB.Duct duct = DB.Duct.Create(Doc, system.Id, ductType.Id, level.Id, startPoint, endPoint);

      if (speckleRevitDuct != null)
      {
        TrySetParam(duct, BuiltInParameter.RBS_CURVE_HEIGHT_PARAM, speckleRevitDuct.height, speckleRevitDuct.units);
        TrySetParam(duct, BuiltInParameter.RBS_CURVE_WIDTH_PARAM, speckleRevitDuct.width, speckleRevitDuct.units);
        TrySetParam(duct, BuiltInParameter.RBS_CURVE_DIAMETER_PARAM, speckleRevitDuct.diameter, speckleRevitDuct.units);
        TrySetParam(duct, BuiltInParameter.CURVE_ELEM_LENGTH, speckleRevitDuct.length, speckleRevitDuct.units);
        TrySetParam(duct, BuiltInParameter.RBS_VELOCITY, speckleRevitDuct.velocity, speckleRevitDuct.units);

        SetInstanceParameters(duct, speckleRevitDuct);
      }

      var placeholders = new List<ApplicationPlaceholderObject>() { new ApplicationPlaceholderObject { applicationId = speckleRevitDuct.applicationId, ApplicationGeneratedId = duct.UniqueId, NativeObject = duct } };

      return placeholders;
    }

    public BuiltElements.Duct DuctToSpeckle(DB.Duct revitDuct)
    {
      var baseGeometry = LocationToSpeckle(revitDuct);
      var baseLine = baseGeometry as Line;
      if (baseLine == null)
      {
        throw new Speckle.Core.Logging.SpeckleException("Only line base Ducts are currently supported.");
      }

      // SPECKLE DUCT
      var speckleDuct = new RevitDuct();
      speckleDuct.family = revitDuct.DuctType.FamilyName;
      speckleDuct.type = revitDuct.DuctType.Name;
      speckleDuct.baseLine = baseLine;

      speckleDuct.diameter = GetParamValue<double>(revitDuct, BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);
      speckleDuct.height = GetParamValue<double>(revitDuct, BuiltInParameter.RBS_CURVE_HEIGHT_PARAM);
      speckleDuct.width = GetParamValue<double>(revitDuct, BuiltInParameter.RBS_CURVE_WIDTH_PARAM);
      speckleDuct.length = GetParamValue<double>(revitDuct, BuiltInParameter.CURVE_ELEM_LENGTH);
      speckleDuct.velocity = GetParamValue<double>(revitDuct, BuiltInParameter.RBS_VELOCITY);
      speckleDuct.level = ConvertAndCacheLevel(revitDuct, BuiltInParameter.RBS_START_LEVEL_PARAM);

      var typeElem = Doc.GetElement(revitDuct.MEPSystem.GetTypeId());
      speckleDuct.systemName = typeElem.Name;

      GetAllRevitParamsAndIds(speckleDuct, revitDuct,
        new List<string> { "RBS_CURVE_HEIGHT_PARAM", "RBS_CURVE_WIDTH_PARAM", "RBS_CURVE_DIAMETER_PARAM", "CURVE_ELEM_LENGTH", "RBS_START_LEVEL_PARAM", "RBS_VELOCITY" });

      return speckleDuct;
    }
  }
}
