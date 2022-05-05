using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using DB = Autodesk.Revit.DB;
using Line = Objects.Geometry.Line;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public List<ApplicationPlaceholderObject> CableTrayToNative(BuiltElements.CableTray speckleCableTray)
    {
      var speckleRevitCableTray = speckleCableTray as RevitCableTray;
      var cableTrayType = GetElementType<CableTrayType>(speckleCableTray);

      Element cableTray = null;
      if (speckleCableTray.baseCurve is Line)
      {
        DB.Line baseLine = LineToNative(speckleCableTray.baseCurve as Line);
        XYZ startPoint = baseLine.GetEndPoint(0);
        XYZ endPoint = baseLine.GetEndPoint(1);
        DB.Level lineLevel = ConvertLevelToRevit(speckleRevitCableTray != null ? speckleRevitCableTray.level : LevelFromCurve(baseLine));
        CableTray lineCableTray = CableTray.Create(Doc, cableTrayType.Id, startPoint, endPoint, lineLevel.Id);
        cableTray = lineCableTray;
      }
      else
      {
        Report.LogConversionError(new Exception($"CableTray BaseCurve of type ${speckleCableTray.baseCurve.GetType()} cannot be used to create a Revit CableTray"));
      }

      var docObj = GetExistingElementByApplicationId(((Base)speckleCableTray).applicationId);

      if (docObj != null && ReceiveMode == Speckle.Core.Kits.ReceiveMode.Ignore)
        return new List<ApplicationPlaceholderObject>
      {
        new ApplicationPlaceholderObject
          {applicationId = speckleCableTray.applicationId, ApplicationGeneratedId = docObj.UniqueId, NativeObject = docObj}
      };

      // deleting instead of updating for now!
      if (docObj != null)
      {
        Doc.Delete(docObj.Id);
      }

      if (speckleRevitCableTray != null)
      {
        TrySetParam(cableTray, BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM, speckleRevitCableTray.height, speckleRevitCableTray.units);
        TrySetParam(cableTray, BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM, speckleRevitCableTray.width, speckleRevitCableTray.units);
        TrySetParam(cableTray, BuiltInParameter.CURVE_ELEM_LENGTH, speckleRevitCableTray.length, speckleRevitCableTray.units);
        SetInstanceParameters(cableTray, speckleRevitCableTray);
      }

      var placeholders = new List<ApplicationPlaceholderObject>
      {
        new ApplicationPlaceholderObject
          {applicationId = speckleCableTray.applicationId, ApplicationGeneratedId = cableTray.UniqueId, NativeObject = cableTray}
      };
      Report.Log($"Created CableTray {cableTray.Id}");
      return placeholders;
    }

    public BuiltElements.CableTray CableTrayToSpeckle(DB.Electrical.CableTray revitCableTray)
    {
      var baseGeometry = LocationToSpeckle(revitCableTray);
      if (!(baseGeometry is Line baseLine))
      {
        throw new Speckle.Core.Logging.SpeckleException("Only line based CableTrays are currently supported.");
      }
      var cableTrayType = revitCableTray.Document.GetElement(revitCableTray.GetTypeId()) as CableTrayType;

      // SPECKLE CABLETRAY
      var speckleCableTray = new RevitCableTray
      {
        family = cableTrayType.FamilyName,
        type = cableTrayType.Name,
        baseCurve = baseLine,
        height = GetParamValue<double>(revitCableTray, BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM),
        width = GetParamValue<double>(revitCableTray, BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM),
        length = GetParamValue<double>(revitCableTray, BuiltInParameter.CURVE_ELEM_LENGTH),
        level = ConvertAndCacheLevel(revitCableTray, BuiltInParameter.RBS_START_LEVEL_PARAM),
        displayValue = GetElementMesh(revitCableTray)
      };

      GetAllRevitParamsAndIds(speckleCableTray, revitCableTray,
        new List<string>
        {
          "RBS_CABLETRAY_HEIGHT_PARAM", "RBS_CABLETRAY_WIDTH_PARAM", "CURVE_ELEM_LENGTH", "RBS_START_LEVEL_PARAM"
        });

      return speckleCableTray;
    }
  }
}