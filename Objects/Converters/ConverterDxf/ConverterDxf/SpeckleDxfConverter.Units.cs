using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.netDxf.Units;

namespace Objects.Converters.DxfConverter
{
  public partial class SpeckleDxfConverter
  {
    public double ScaleToNative(double value, string units)
    {
      return value * Units.GetConversionFactor(units, DocUnitsToUnits(Doc.DrawingVariables.InsUnits));
    }

    public DrawingUnits UnitsToDocUnits(string units)
    {
      return units switch
      {
        Units.Centimeters => DrawingUnits.Centimeters,
        Units.Meters => DrawingUnits.Meters,
        Units.Kilometers => DrawingUnits.Kilometers,
        Units.Inches => DrawingUnits.Inches,
        Units.Feet => DrawingUnits.Feet,
        Units.Yards => DrawingUnits.Yards,
        Units.Miles => DrawingUnits.Miles,
        _ => DrawingUnits.Meters
      };
    }

    public string DocUnitsToUnits(DrawingUnits units)
    {
      return units switch
      {
        DrawingUnits.Centimeters => Units.Centimeters,
        DrawingUnits.Meters => Units.Meters,
        DrawingUnits.Kilometers => Units.Kilometers,
        DrawingUnits.Inches => Units.Inches,
        DrawingUnits.Feet => Units.Feet,
        DrawingUnits.Yards => Units.Yards,
        DrawingUnits.Miles => Units.Miles,
        _ => throw new SpeckleException("Unknown document units!")
      };
    }
  }
}