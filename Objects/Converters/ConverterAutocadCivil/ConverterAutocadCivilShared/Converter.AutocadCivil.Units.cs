using Speckle.Core.Kits;
using Speckle.Core.Models;

using Autodesk.AutoCAD.DatabaseServices;

namespace Objects.Converter.AutocadCivil
{
  public partial class ConverterAutocadCivil
  {
    private string _modelUnits;
    public string ModelUnits
    {
      get
      {
        if (string.IsNullOrEmpty(_modelUnits))
          _modelUnits = UnitToSpeckle(Doc.Database.Insunits);
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

    private string UnitToSpeckle(UnitsValue units)
    {
      switch (units)
      {
        case UnitsValue.Millimeters:
          return Units.Millimeters;
        case UnitsValue.Centimeters:
          return Units.Centimeters;
        case UnitsValue.Meters:
          return Units.Meters;
        case UnitsValue.Kilometers:
          return Units.Kilometers;
        case UnitsValue.Inches:
          return Units.Inches;
        case UnitsValue.Feet:
          return Units.Feet;
        case UnitsValue.Yards:
          return Units.Yards;
        case UnitsValue.Miles:
          return Units.Miles;
        default:
          throw new System.Exception("The current Unit System is unsupported.");
      }
    }
  }
}
