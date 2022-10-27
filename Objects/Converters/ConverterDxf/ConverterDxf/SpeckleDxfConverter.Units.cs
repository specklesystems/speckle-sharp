using System;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.netDxf.Units;

namespace Objects.Converters.DxfConverter
{
  public partial class SpeckleDxfConverter
  {
    public double ScaleToNative(double value, string units)
    {
      Console.WriteLine(Doc);
      Console.WriteLine(units);
      if (units == null)
      {
        throw new Exception("null units");
      }

      if (Doc == null)
        throw new Exception("null doc");
      
      return value * Units.GetConversionFactor(units, DocUnitsToUnits(Doc.DrawingVariables.InsUnits));
    }

    public DrawingUnits UnitsToDocUnits(string units)
    {
      switch (units)
      {
        case Units.Centimeters:
          return DrawingUnits.Centimeters;
        case Units.Meters:
          return DrawingUnits.Meters;
        case Units.Kilometers:
          return DrawingUnits.Kilometers;
        case Units.Inches:
          return DrawingUnits.Inches;
        case Units.Feet:
          return DrawingUnits.Feet;
        case Units.Yards:
          return DrawingUnits.Yards;
        case Units.Miles:
          return DrawingUnits.Miles;
        default:
          return DrawingUnits.Meters;
      }
    }

    public string DocUnitsToUnits(DrawingUnits units)
    {
      switch (units)
      {
        case DrawingUnits.Centimeters:
          return Units.Centimeters;
        case DrawingUnits.Meters:
          return Units.Meters;
        case DrawingUnits.Kilometers:
          return Units.Kilometers;
        case DrawingUnits.Inches:
          return Units.Inches;
        case DrawingUnits.Feet:
          return Units.Feet;
        case DrawingUnits.Yards:
          return Units.Yards;
        case DrawingUnits.Miles:
          return Units.Miles;
        case DrawingUnits.Unitless:
          return Units.None;
        default:
          throw new SpeckleException($"Unknown document units: {units}");
      }
    }
  }
}