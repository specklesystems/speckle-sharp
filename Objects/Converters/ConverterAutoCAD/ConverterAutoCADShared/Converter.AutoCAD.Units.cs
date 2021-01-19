using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Converter.AutoCAD
{
  public enum DistanceUnitFormat
  {
    Current = -1,
    Scientific = 1,
    Decimal = 2,
    Engineering = 3,
    Architectural = 4,
    Fractional = 5
  }

  public partial class ConverterAutoCAD
  {
    private string _modelUnits;
    public string ModelUnits
    {
      get
      {
        if (string.IsNullOrEmpty(_modelUnits))
        {
          _modelUnits = UnitToSpeckle(Doc.Database.Lunits);
        }
        return _modelUnits;
      }
    }
    private void SetUnits(Base geom)
    {
      geom.units = ModelUnits;
    }

    private double ScaleToNative(double value, string units)
    {
      var f = Units.GetConversionFactor(units, ModelUnits);
      return value * f;
    }

    private string UnitToSpeckle(int units)
    {
      switch (units)
      {
        case (int)DistanceUnitFormat.Architectural:
          return Units.Meters;
        case (int)DistanceUnitFormat.Engineering:
          return Units.Millimeters;
        default:
          throw new System.Exception("The current Unit System is unsupported.");
      }
    }
  }
}
