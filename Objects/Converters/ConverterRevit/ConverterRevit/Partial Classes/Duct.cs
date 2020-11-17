using Objects;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB.Mechanical;
using Duct = Objects.BuiltElements.Duct;
using Level = Objects.BuiltElements.Level;
using Line = Objects.Geometry.Line;
using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.DB.Mechanical;
using System.Linq;
using Objects.Revit;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public DB.Duct DuctToNative(Duct speckleDuct)
    {
      DB.Duct duct = null;
      var baseLine = LineToNative(speckleDuct.baseLine);
      XYZ startPoint = baseLine.GetEndPoint(0);
      XYZ endPoint = baseLine.GetEndPoint(1);

      Autodesk.Revit.DB.Level level = null;
      var type = "";

      var speckleRevitDuct = speckleDuct as RevitDuct;
      if (speckleRevitDuct != null)
      {
        type = speckleRevitDuct.type;
        level = LevelToNative(speckleRevitDuct.level);
      }
      else
      {
        level = LevelToNative(LevelFromCurve(baseLine));
      }

      var ductType = GetElementByTypeAndName<DB.DuctType>(type);
      var system = GetElementByTypeAndName<MechanicalSystemType>(speckleDuct.system);

      var (docObj, stateObj) = GetExistingElementByApplicationId(speckleDuct.applicationId, speckleDuct.speckle_type);

      // deleting instead of updating for now!
      if (docObj != null)
        Doc.Delete(docObj.Id);

      duct = DB.Duct.Create(Doc, system.Id, ductType.Id, level.Id, startPoint, endPoint);

      if (speckleRevitDuct != null)
        SetElementParams(duct, speckleRevitDuct);

      return duct;
    }
    public Duct DuctToSpeckle(DB.Duct revitDuct)
    {
      var baseGeometry = LocationToSpeckle(revitDuct);
      var baseLine = baseGeometry as Line;
      if (baseLine == null)
      {
        throw new Exception("Only line base Ducts are currently supported.");
      }

      // REVIT PARAMS > SPECKLE PROPS
      var heightParam = revitDuct.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM);
      var widthParam = revitDuct.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM);
      var diameterParam = revitDuct.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);
      var lengthParam = revitDuct.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
      var levelParam = revitDuct.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM);
      var velocityParam = revitDuct.get_Parameter(BuiltInParameter.RBS_VELOCITY);
      var system = revitDuct.get_Parameter(BuiltInParameter.RBS_SYSTEM_CLASSIFICATION_PARAM);

      // SPECKLE DUCT
      var speckleDuct = new RevitDuct();
      speckleDuct.type = revitDuct.DuctType.FamilyName;
      speckleDuct.baseLine = baseLine;
      if (diameterParam != null)
      {
        speckleDuct.diameter = (double)ParameterToSpeckle(diameterParam);
      }
      else
      {
        speckleDuct.height = (double)ParameterToSpeckle(heightParam);
        speckleDuct.width = (double)ParameterToSpeckle(widthParam);
      }
      speckleDuct.length = (double)ParameterToSpeckle(lengthParam);
      speckleDuct.velocity = (double)ParameterToSpeckle(velocityParam);
      speckleDuct.level = (RevitLevel)ParameterToSpeckle(levelParam);
      speckleDuct.system = (String)ParameterToSpeckle(system);

      AddCommonRevitProps(speckleDuct, revitDuct);

      return speckleDuct;
    }


  }
}
