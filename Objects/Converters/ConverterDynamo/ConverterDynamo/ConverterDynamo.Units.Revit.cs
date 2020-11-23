#if REVIT
using System;
using System.Linq;
using Autodesk.Revit.DB;

namespace Objects.Converter.Dynamo
{
  public partial class ConverterDynamo
  {

    public string GetRevitDocUnits()
    {
        if (Doc != null)
        {
          _modelUnits = UnitsToSpeckle(RevitLengthTypeId.TypeId);
          return _modelUnits;
        }
      return Speckle.Core.Kits.Units.Meters;
    }

    private ForgeTypeId _revitUnitsTypeId;
    private ForgeTypeId RevitLengthTypeId
    {
      get
      {
        if (_revitUnitsTypeId == null)
        {
          var fo = Doc.GetUnits().GetFormatOptions(SpecTypeId.Length);
          _revitUnitsTypeId = fo.GetUnitTypeId();
        }
        return _revitUnitsTypeId;
      }
    }

    //new units api introduced in 2021, bleah
    private string UnitsToSpeckle(string typeId)
    {
      if (typeId == UnitTypeId.Millimeters.TypeId)
        return Speckle.Core.Kits.Units.Millimeters;
      else if (typeId == UnitTypeId.Centimeters.TypeId)
        return Speckle.Core.Kits.Units.Centimeters;
      else if (typeId == UnitTypeId.Meters.TypeId)
        return Speckle.Core.Kits.Units.Meters;
      else if (typeId == UnitTypeId.Inches.TypeId)
        return Speckle.Core.Kits.Units.Inches;
      else if (typeId == UnitTypeId.Feet.TypeId)
        return Speckle.Core.Kits.Units.Feet;

      throw new Exception("The current Unit System is unsupported.");
    }
  }
}
#endif