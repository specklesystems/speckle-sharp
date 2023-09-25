using Autodesk.Revit.DB;
using ConverterRevitShared.Extensions;
using RevitSharedResources.Interfaces;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {

    private string _modelUnits;

#if REVIT2020
    public string ModelUnits
    {
      get
      {
        if (string.IsNullOrEmpty(_modelUnits))
        {
          _modelUnits = UnitsToSpeckle(RevitLengthTypeId);
        }
        return _modelUnits;
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

    /// <summary>
    /// Converts Speckle length values to internal ones
    /// NOTE: use only to convert double values, not point or vector coordinates. For those use Point/VectorToNative
    /// as that takes into account the Project Base Location
    /// </summary>
    /// <param name="value"></param>
    /// <param name="units"></param>
    /// <returns></returns>
    public double ScaleToNative(double value, string units)
    {
      return UnitUtils.ConvertToInternalUnits(value, UnitsToNative(units));
    }

    /// <summary>
    /// Converts Speckle length values to internal ones
    /// NOTE: use only to convert double values, not point or vector coordinates. For those use Point/VectorToNative
    /// as that takes into account the Project Base Location
    /// </summary>
    /// <param name="value"></param>
    /// <param name="units"></param>
    /// <returns></returns>
    public double ScaleToNative(double value, DisplayUnitType units)
    {
      return UnitUtils.ConvertToInternalUnits(value, units);
    }

    /// <summary>
    /// Converts internal length values to Speckle ones
    /// NOTE: use only to convert double values, not point or vector coordinates. For those use Point/VectorToSpeckle
    /// as that takes into account the Project Base Location
    /// </summary>
    /// <param name="value"></param>
    /// <param name="units"></param>
    /// <returns></returns>
    public double ScaleToSpeckle(double value)
    {
      return ScaleToSpeckle(value, RevitLengthTypeId);
    }
    
    public static double ScaleToSpeckle(double value, DisplayUnitType unitType)
    {
      return UnitUtils.ConvertFromInternalUnits(value, unitType);
    }

    public static double ScaleToSpeckle(double value, DisplayUnitType unitType, IRevitDocumentAggregateCache cache)
    {
      return ScaleToSpeckle(value, unitType);
    }

    public static double ScaleToSpeckle(double value, string units)
    {
      return ScaleToSpeckle(value, UnitsToNative(units));
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

    public static DisplayUnitType UnitsToNative(string units)
    {
      switch (units)
      {
        case Speckle.Core.Kits.Units.Millimeters:
          return DisplayUnitType.DUT_MILLIMETERS;
        case Speckle.Core.Kits.Units.Centimeters:
          return DisplayUnitType.DUT_CENTIMETERS;
        case Speckle.Core.Kits.Units.Meters:
          return DisplayUnitType.DUT_METERS;
        case Speckle.Core.Kits.Units.Inches:
          return DisplayUnitType.DUT_DECIMAL_INCHES;
        case Speckle.Core.Kits.Units.Feet:
          return DisplayUnitType.DUT_DECIMAL_FEET;
        default:
          throw new Speckle.Core.Logging.SpeckleException($"The Unit System \"{units}\" is unsupported.");
      }
    }
    private static string UnitsToNativeString(DisplayUnitType unitType)
    {
      return unitType.ToString();
    }
#else
    public string ModelUnits
    {
      get
      {
        if (string.IsNullOrEmpty(_modelUnits))
        {
          _modelUnits = UnitsToSpeckle(RevitLengthTypeId.TypeId);
        }
        return _modelUnits;
      }
    }

    private ForgeTypeId _revitUnitsTypeId;
    public ForgeTypeId RevitLengthTypeId
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

    public static double ScaleToNative(double value, string units)
    {
      if (string.IsNullOrEmpty(units))
        return value;
      return UnitUtils.ConvertToInternalUnits(value, UnitsToNative(units));
    }

    private double? defaultConversionFactor;
    public double ScaleToSpeckle(double value)
    {
      defaultConversionFactor ??= ScaleToSpeckle(1, RevitLengthTypeId);
      return value * defaultConversionFactor.Value;
    }
    
    public static double ScaleToSpeckle(double value, string units)
    {
      return ScaleToSpeckle(value, UnitsToNative(units));
    }
    
    public static double ScaleToSpeckle(double value, ForgeTypeId forgeTypeId)
    {
      // our current profiling shows that the method "ConvertFromInternalUnits" is a huge bottleneck
      // in the ScaleToSpeckle(double) method, but not here. This is because the former method is called
      // roughly 12 times more often than this function
      return UnitUtils.ConvertFromInternalUnits(value, forgeTypeId);
    }

    //new units api introduced in 2021, bleah
    public string UnitsToSpeckle(string typeId)
    {
      if (typeId == UnitTypeId.Millimeters.TypeId)
        return Speckle.Core.Kits.Units.Millimeters;
      else if (typeId == UnitTypeId.Centimeters.TypeId)
        return Speckle.Core.Kits.Units.Centimeters;
      else if (typeId == UnitTypeId.Meters.TypeId || typeId == UnitTypeId.MetersCentimeters.TypeId)
        return Speckle.Core.Kits.Units.Meters;
      else if (typeId == UnitTypeId.Inches.TypeId || typeId == UnitTypeId.FractionalInches.TypeId)
        return Speckle.Core.Kits.Units.Inches;
      else if (typeId == UnitTypeId.Feet.TypeId || typeId == UnitTypeId.FeetFractionalInches.TypeId)
        return Speckle.Core.Kits.Units.Feet;

      throw new Speckle.Core.Logging.SpeckleException($"The Unit System \"{typeId}\" is unsupported.");
    }

    public static string UnitsToNativeString(string units)
    {
      return UnitsToNativeString(UnitsToNative(units));
    }
    public static string UnitsToNativeString(ForgeTypeId forgeTypeId)
    {
      return forgeTypeId.TypeId;
    }
    public static ForgeTypeId UnitsToNative(string units)
    {
      switch (units)
      {
        case Speckle.Core.Kits.Units.Millimeters:
          return UnitTypeId.Millimeters;
        case Speckle.Core.Kits.Units.Centimeters:
          return UnitTypeId.Centimeters;
        case Speckle.Core.Kits.Units.Meters:
          return UnitTypeId.Meters;
        case Speckle.Core.Kits.Units.Inches:
          return UnitTypeId.Inches;
        case Speckle.Core.Kits.Units.Feet:
          return UnitTypeId.Feet;
        default:
          throw new Speckle.Core.Logging.SpeckleException($"The Unit System \"{units}\" is unsupported.");
      }
    }
#endif
  }
}
