using Autodesk.Revit.DB;

namespace ConnectorRevit
{
  public static class RevitVersionHelper
  {

    public static double ConvertFromInternalUnits(double val, Parameter parameter)
    {
#if REVIT2020
      return UnitUtils.ConvertFromInternalUnits(val, parameter.DisplayUnitType);
#else
      return UnitUtils.ConvertFromInternalUnits(val, parameter.GetUnitTypeId());
#endif
    }
  }
}
