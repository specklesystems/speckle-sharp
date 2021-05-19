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

      Autodesk.Revit.DB.Level level;

      var speckleRevitDuct = speckleDuct as RevitDuct;
      level = LevelToNative(speckleRevitDuct != null ? speckleRevitDuct.level : LevelFromCurve(baseLine));

      var ductType = GetElementType<DB.DuctType>(speckleDuct);
      var systemFamily = ( speckleDuct is RevitDuct rd ) ? rd.systemName : "";

      List<ElementType> types = new FilteredElementCollector(Doc).WhereElementIsElementType()
        .OfClass(typeof(MechanicalSystemType)).ToElements().Cast<ElementType>().ToList();
      var system = types.FirstOrDefault(x => x.Name == systemFamily);
      if ( system == null )
      {
        system = types.FirstOrDefault();
        ConversionErrors.Add(new Exception($"Duct type {systemFamily} not found; replaced with {system.Name}"));
      }

      var docObj = GetExistingElementByApplicationId(( ( Base ) speckleDuct ).applicationId);


      // deleting instead of updating for now!
      if ( docObj != null )
      {
        Doc.Delete(docObj.Id);
      }

      DB.Duct duct = DB.Duct.Create(Doc, system.Id, ductType.Id, level.Id, startPoint, endPoint);

      if ( speckleRevitDuct != null )
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
          {applicationId = speckleRevitDuct.applicationId, ApplicationGeneratedId = duct.UniqueId, NativeObject = duct}
      };

      return placeholders;
    }

    public BuiltElements.Duct DuctToSpeckle(DB.Duct revitDuct)
    {
      var baseGeometry = LocationToSpeckle(revitDuct);
      if ( !( baseGeometry is Line baseLine ) )
      {
        throw new Speckle.Core.Logging.SpeckleException("Only line based Ducts are currently supported.");
      }

      // SPECKLE DUCT
      var speckleDuct = new RevitDuct
      {
        family = revitDuct.DuctType.FamilyName,
        type = revitDuct.DuctType.Name,
        baseLine = baseLine,
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
  }
}