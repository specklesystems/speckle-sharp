#if REVIT
using Autodesk.Revit.DB;
using Objects.Converter.Revit;
#endif

namespace Objects.Converter.Dynamo
{
  public partial class ConverterDynamo
  {
    private string _modelUnits;
    public string ModelUnits
    {
      get
      {
        if (string.IsNullOrEmpty(_modelUnits))
        {
#if REVIT
          _modelUnits = GetRevitDocUnits();
#else
          _modelUnits = Speckle.Core.Kits.Units.Meters;
#endif
        }
        return _modelUnits;
      }
    }

#if REVIT

    public string GetRevitDocUnits()
    {
      if (Doc != null)
      {
        _modelUnits = UnitsToSpeckle(RevitLengthTypeId);
        return _modelUnits;
      }
      return Speckle.Core.Kits.Units.Meters;
    }

#if !(REVIT2022 || REVIT2023)

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
        case DisplayUnitType.DUT_METERS_CENTIMETERS:
          return Speckle.Core.Kits.Units.Meters;
        case DisplayUnitType.DUT_DECIMAL_INCHES:
          return Speckle.Core.Kits.Units.Inches;
        case DisplayUnitType.DUT_DECIMAL_FEET:
          return Speckle.Core.Kits.Units.Feet;
        case DisplayUnitType.DUT_FEET_FRACTIONAL_INCHES:
          return Speckle.Core.Kits.Units.Feet;
        case DisplayUnitType.DUT_FRACTIONAL_INCHES:
          return Speckle.Core.Kits.Units.Inches;
        default:
          throw new Speckle.Core.Logging.SpeckleException($"The Unit System \"{type}\" is unsupported.");
      }

    }
#else

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

    private string UnitsToSpeckle(ForgeTypeId typeId)
    {
      if (typeId == UnitTypeId.Millimeters)
        return Speckle.Core.Kits.Units.Millimeters;
      else if (typeId == UnitTypeId.Centimeters)
        return Speckle.Core.Kits.Units.Centimeters;
      else if (typeId == UnitTypeId.Meters)
        return Speckle.Core.Kits.Units.Meters;
      else if (typeId == UnitTypeId.MetersCentimeters)
        return Speckle.Core.Kits.Units.Meters;
      else if (typeId == UnitTypeId.Inches)
        return Speckle.Core.Kits.Units.Inches;
      else if (typeId == UnitTypeId.Feet)
        return Speckle.Core.Kits.Units.Feet;
      else if (typeId == UnitTypeId.FeetFractionalInches)
        return Speckle.Core.Kits.Units.Feet;
      else if (typeId == UnitTypeId.FractionalInches)
        return Speckle.Core.Kits.Units.Inches;

      throw new Speckle.Core.Logging.SpeckleException($"The Unit System \"{typeId}\" is unsupported.");
    }

#endif

#endif

    private double ScaleToNative(double value, string units)
    {
      var f = Speckle.Core.Kits.Units.GetConversionFactor(units, ModelUnits);
      return value * f;
    }

  }
}
