using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    private string _modelUnits;
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

    private double ScaleToNative(double value, string units)
    {
      return UnitUtils.ConvertToInternalUnits(value, new ForgeTypeId(UnitsToNative(units)));
    }

    private double ScaleToSpeckle(double value)
    {
      return UnitUtils.ConvertFromInternalUnits(value, RevitLengthTypeId);
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

    private string UnitsToNative(string units)
    {
      switch (units)
      {
        case Speckle.Core.Kits.Units.Millimeters:
          return UnitTypeId.Millimeters.TypeId;
        case Speckle.Core.Kits.Units.Centimeters:
          return UnitTypeId.Centimeters.TypeId;
        case Speckle.Core.Kits.Units.Meters:
          return UnitTypeId.Meters.TypeId;
        case Speckle.Core.Kits.Units.Inches:
          return UnitTypeId.Inches.TypeId;
        case Speckle.Core.Kits.Units.Feet:
          return UnitTypeId.Feet.TypeId;
        default:
          throw new Exception("The current Unit System is unsupported.");
      }
    }
  }
}

