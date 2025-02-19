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
        _modelUnits = UnitsToSpeckle(RevitLengthTypeId);
        return _modelUnits;
      }
      return Speckle.Core.Kits.Units.Meters;
    }

    private string UnitsToSpeckle(DisplayUnitType type)
    {
      switch (type)
      {
        case DisplayUnitType.DUT_MILLIMETERS:
          return Speckle.Core.Kits.Units.Millimeters;
        case DisplayUnitType.DUT_CENTIMETERS:
          return Speckle.Core.Kits.Units.Centimeters;
        case DisplayUnitType.DUT_METERS:
          return Speckle.Core.Kits.Units.Meters;
        case DisplayUnitType.DUT_DECIMAL_INCHES:
          return Speckle.Core.Kits.Units.Inches;
        case DisplayUnitType.DUT_DECIMAL_FEET:
          return Speckle.Core.Kits.Units.Feet;
        default:
          throw new Speckle.Core.Logging.SpeckleException($"The Unit System \"{type}\" is unsupported.");
      }
    }

    private DisplayUnitType _revitUnitsTypeId = DisplayUnitType.DUT_UNDEFINED;
    public DisplayUnitType RevitLengthTypeId
    {
      get
      {
        if (_revitUnitsTypeId == DisplayUnitType.DUT_UNDEFINED)
        {
          var fo = Doc.GetUnits().GetFormatOptions(UnitType.UT_Length);
          _revitUnitsTypeId = fo.DisplayUnits;
        }
        return _revitUnitsTypeId;
      }
    }

    //private ForgeTypeId _revitUnitsTypeId;
    //private ForgeTypeId RevitLengthTypeId
    //{
    //  get
    //  {
    //    if (_revitUnitsTypeId == null)
    //    {
    //      var fo = Doc.GetUnits().GetFormatOptions(SpecTypeId.Length);
    //      _revitUnitsTypeId = fo.GetUnitTypeId();
    //    }
    //    return _revitUnitsTypeId;
    //  }
    //}
    //private string UnitsToSpeckle(string typeId)
    //{
    //  if (typeId == UnitTypeId.Millimeters.TypeId)
    //    return Speckle.Core.Kits.Units.Millimeters;
    //  else if (typeId == UnitTypeId.Centimeters.TypeId)
    //    return Speckle.Core.Kits.Units.Centimeters;
    //  else if (typeId == UnitTypeId.Meters.TypeId)
    //    return Speckle.Core.Kits.Units.Meters;
    //  else if (typeId == UnitTypeId.Inches.TypeId)
    //    return Speckle.Core.Kits.Units.Inches;
    //  else if (typeId == UnitTypeId.Feet.TypeId)
    //    return Speckle.Core.Kits.Units.Feet;

    //  throw new Speckle.Core.Logging.SpeckleException($"The Unit System \"{typeId}\" is unsupported.");
    //}
  }
}
#endif
