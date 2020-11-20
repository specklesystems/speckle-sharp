using Objects.Primitive;
using Rhino;
using Speckle.Core.Kits;

namespace Objects.Converter.RhinoGh
{
  public static partial class Conversion
  {

    public static string ToSpeckle(this UnitSystem us)
    {
      switch (us)
      {
        //case UnitSystem.None:
        //  break;
        //case UnitSystem.Angstroms:
        //  break;
        //case UnitSystem.Nanometers:
        //  break;
        //case UnitSystem.Microns:
        //  break;
        case UnitSystem.Millimeters:
          return Units.Millimeters;
        case UnitSystem.Centimeters:
          return Units.Centimeters;
        //case UnitSystem.Decimeters:
        //  break;
        case UnitSystem.Meters:
          return Units.Meters;
        //case UnitSystem.Dekameters:
        //  break;
        //case UnitSystem.Hectometers:
        //  break;
        case UnitSystem.Kilometers:
          return Units.Kilometers;
        //case UnitSystem.Megameters:
        //  break;
        //case UnitSystem.Gigameters:
        //  break;
        //case UnitSystem.Microinches:
        //  break;
        //case UnitSystem.Mils:
        //  break;
        case UnitSystem.Inches:
          return Units.Inches;
        case UnitSystem.Feet:
          return Units.Feet;
        case UnitSystem.Yards:
          return Units.Yards;
        case UnitSystem.Miles:
          return Units.Miles;
        //case UnitSystem.PrinterPoints:
        //  break;
        //case UnitSystem.PrinterPicas:
        //  break;
        //case UnitSystem.NauticalMiles:
        //  break;
        //case UnitSystem.AstronomicalUnits:
        //  break;
        //case UnitSystem.LightYears:
        //  break;
        //case UnitSystem.Parsecs:
        //  break;
        //case UnitSystem.CustomUnits:
        //  break;
        //case UnitSystem.Unset:
        //  break;
        default:
          throw new System.Exception("The current Unit System is unsupported.");
      }
    }
  }
}
