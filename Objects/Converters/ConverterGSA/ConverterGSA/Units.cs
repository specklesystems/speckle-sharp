using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Speckle.Core.Logging;
using Speckle.Core.Kits;

namespace ConverterGSA
{
  #region Primary dimensions
  public static class MassUnits
  {
    public const string Milligram = "mg";
    public const string Gram = "g";
    public const string Kilogram = "kg";
    public const string Tonne = "t";
    public const string Slug = "sl";
    public const string Pound = "lb";
    public const string None = "none";

    public static double GetConversionFactor(string from, string to)
    {
      from = GetUnitsFromString(from);
      to = GetUnitsFromString(to);

      return GetConversionFactorToSI(from) * GetConversionFactorFromSI(to);
    }

    private static double GetConversionFactorToSI(string from)
    {
      switch (from)
      {
        case MassUnits.Milligram: return 1e-6;
        case MassUnits.Gram: return 1e-3;
        case MassUnits.Tonne: return 1e3;
        case MassUnits.Slug: return 14.59390;
        case MassUnits.Pound: return 0.45359237;
        case MassUnits.None: return 1;
      }
      return 1;
    }

    private static double GetConversionFactorFromSI(string to)
    {
      switch (to)
      {
        case MassUnits.Milligram: return 1e6;
        case MassUnits.Gram: return 1e3;
        case MassUnits.Tonne: return 1e-3;
        case MassUnits.Slug: return 1/14.59390;
        case MassUnits.Pound: return 1/0.45359237;
        case MassUnits.None: return 1;
      }
      return 1;
    }

    public static string GetUnitsFromString(string unit)
    {
      if (unit == null) return null;
      switch (unit.ToLower())
      {
        case "mg":
        case "mil":
        case "milligram":
        case "milligrams":
          return MassUnits.Milligram;
        case "g":
        case "gram":
        case "grams":
          return MassUnits.Gram;
        case "kg":
        case "kilogram":
        case "kilograms":
          return MassUnits.Kilogram;
        case "t":
        case "tonne":
        case "tonnes":
          return MassUnits.Tonne;
        case "sl":
        case "slug":
        case "slugs":
          return MassUnits.Slug;
        case "lb":
        case "lbs":
        case "pound":
        case "pounds":
          return MassUnits.Pound;
        case "none":
          return MassUnits.None;
      }

      throw new Exception($"Cannot understand what unit {unit} is.");
    }

    public static int GetEncodingFromUnit(string unit)
    {
      switch (unit)
      {
        case Milligram: return 1;
        case Gram: return 2;
        case Kilogram: return 3;
        case Tonne: return 4;
        case Slug: return 5;
        case Pound: return 6;
      }

      return 0;
    }

    public static string GetUnitFromEncoding(double unit)
    {
      switch (unit)
      {
        case 1: return Milligram;
        case 2: return Gram;
        case 3: return Kilogram;
        case 4: return Tonne;
        case 5: return Slug;
        case 6: return Pound;
      }

      return None;
    }
  }

  public static class TimeUnits
  {
    public const string Millisecond = "ms";
    public const string Second = "s";
    public const string Minute = "min";
    public const string Hour = "h";
    public const string Day = "d";
    public const string None = "none";

    public static double GetConversionFactor(string from, string to)
    {
      from = GetUnitsFromString(from);
      to = GetUnitsFromString(to);

      return GetConversionFactorToSI(from) * GetConversionFactorFromSI(to);
    }

    private static double GetConversionFactorToSI(string from)
    {
      switch (from)
      {
        case TimeUnits.Millisecond: return 1e-3;
        case TimeUnits.Minute: return 60;
        case TimeUnits.Hour: return 60 * 60;
        case TimeUnits.Day: return 24 * 60 * 60;
        case TimeUnits.None: return 1;
      }
      return 1;
    }

    private static double GetConversionFactorFromSI(string to)
    {
      switch (to)
      {
        case TimeUnits.Millisecond: return 1e3;
        case TimeUnits.Minute: return 1 / 60;
        case TimeUnits.Hour: return 1 / (60 * 60);
        case TimeUnits.Day: return 1 / (24 * 60 * 60);
        case TimeUnits.None: return 1;
      }
      return 1;
    }

    public static string GetUnitsFromString(string unit)
    {
      if (unit == null) return null;
      switch (unit.ToLower())
      {
        case "ms":
        case "mil":
        case "millisecond":
        case "milliseconds":
          return TimeUnits.Millisecond;
        case "s":
        case "second":
        case "seconds":
          return TimeUnits.Second;
        case "min":
        case "minute":
        case "minutes":
          return TimeUnits.Minute;
        case "h":
        case "hr":
        case "hour":
        case "hours":
          return TimeUnits.Hour;
        case "d":
        case "day":
        case "days":
          return TimeUnits.Day;
        case "none":
          return TimeUnits.None;
      }

      throw new Exception($"Cannot understand what unit {unit} is.");
    }

    public static int GetEncodingFromUnit(string unit)
    {
      switch (unit)
      {
        case Millisecond: return 1;
        case Second: return 2;
        case Minute: return 3;
        case Hour: return 4;
        case Day: return 5;
      }

      return 0;
    }

    public static string GetUnitFromEncoding(double unit)
    {
      switch (unit)
      {
        case 1: return Millisecond;
        case 2: return Second;
        case 3: return Minute;
        case 4: return Hour;
        case 5: return Day;
      }

      return None;
    }
  }

  public static class TemperatureUnits
  {
    public const string Celcius = "C";
    public const string Fahrenheit = "F";
    public const string Kelvin = "K";
    public const string None = "none";

    public static double Convert(double v, string from, string to)
    {
      from = GetUnitsFromString(from);
      to = GetUnitsFromString(to);

      return ConvertFromSI(ConvertToSI(v, from), to);
    }

    private static double ConvertToSI(double v, string from)
    {
      switch (from)
      {
        case TemperatureUnits.Celcius: return v+273;
        case TemperatureUnits.Fahrenheit: return (5 / 9) * (v - 32) + 273;
        case TemperatureUnits.None: return v;
      }
      return v;
    }

    private static double ConvertFromSI(double v, string to)
    {
      switch (to)
      {
        case TemperatureUnits.Celcius: return v - 273;
        case TemperatureUnits.Fahrenheit: return 9 / 5 * (v - 273) + 32;
        case TemperatureUnits.None: return v;
      }
      return v;
    }

    public static string GetUnitsFromString(string unit)
    {
      if (unit == null) return null;
      switch (unit.ToLower())
      {
        case "c":
        case "oc":
        case "celcius":
          return TemperatureUnits.Celcius;
        case "f":
        case "of":
        case "fahrenheit":
          return TemperatureUnits.Fahrenheit;
        case "k":
        case "kelvin":
          return TemperatureUnits.Kelvin;
        case "none":
          return TemperatureUnits.None;
      }

      throw new Exception($"Cannot understand what unit {unit} is.");
    }

    public static int GetEncodingFromUnit(string unit)
    {
      switch (unit)
      {
        case Celcius: return 1;
        case Fahrenheit: return 2;
        case Kelvin: return 3;
      }

      return 0;
    }

    public static string GetUnitFromEncoding(double unit)
    {
      switch (unit)
      {
        case 1: return Celcius;
        case 2: return Fahrenheit;
        case 3: return Kelvin;
      }

      return None;
    }
  }

  //Electric Current
  //Luminous Intensity
  //Amount of Matter
  #endregion

  #region Other Dimensions
  public static class ForceUnits
  {
    public const string Newton = "N";
    public const string Kilonewton = "kN";
    public const string Meganewtown = "MN";
    public const string PoundForce = "lbf";
    public const string KiloPoundForce = "kip";
    public const string None = "none";

    public static double GetConversionFactor(string from, string to)
    {
      from = GetUnitsFromString(from);
      to = GetUnitsFromString(to);

      return GetConversionFactorToSI(from) * GetConversionFactorFromSI(to);
    }

    private static double GetConversionFactorToSI(string from)
    {
      switch (from)
      {
        case ForceUnits.Kilonewton: return 1e3;
        case ForceUnits.Meganewtown: return 1e6;
        case ForceUnits.PoundForce: return 4.4482216;
        case ForceUnits.KiloPoundForce: return 4448.2216;
        case ForceUnits.None: return 1;
      }
      return 1;
    }

    private static double GetConversionFactorFromSI(string to)
    {
      switch (to)
      {
        case ForceUnits.Kilonewton: return 1e-3;
        case ForceUnits.Meganewtown: return 1e-6;
        case ForceUnits.PoundForce: return 1 / 4.4482216;
        case ForceUnits.KiloPoundForce: return 1 / 4448.2216;
        case ForceUnits.None: return 1;
      }
      return 1;
    }

    public static string GetUnitsFromString(string unit)
    {
      if (unit == null) return null;
      switch (unit.ToLower())
      {
        case "n":
          return ForceUnits.Newton;
        case "kn":
          return ForceUnits.Kilonewton;
        case "mn":
          return ForceUnits.Meganewtown;
        case "lbf":
          return ForceUnits.PoundForce;
        case "kip":
          return ForceUnits.KiloPoundForce;
        case "none":
          return ForceUnits.None;
      }

      throw new Exception($"Cannot understand what unit {unit} is.");
    }

    public static int GetEncodingFromUnit(string unit)
    {
      switch (unit)
      {
        case Newton: return 1;
        case Kilonewton: return 2;
        case Meganewtown: return 3;
        case PoundForce: return 4;
        case KiloPoundForce: return 5;
      }

      return 0;
    }

    public static string GetUnitFromEncoding(double unit)
    {
      switch (unit)
      {
        case 1: return Newton;
        case 2: return Kilonewton;
        case 3: return Meganewtown;
        case 4: return PoundForce;
        case 5: return KiloPoundForce;
      }

      return None;
    }
  }

  public static class AccelerationUnits
  {
    public const string MetersPerSecondSquare = "m/s2";
    public const string FeetPerSecondSquare = "ft/s2";
    public const string None = "none";

    public static double GetConversionFactor(string from, string to)
    {
      from = GetUnitsFromString(from);
      to = GetUnitsFromString(to);

      return GetConversionFactorToSI(from) * GetConversionFactorFromSI(to);
    }

    private static double GetConversionFactorToSI(string from)
    {
      switch (from)
      {
        case AccelerationUnits.FeetPerSecondSquare: return Units.GetConversionFactor(Units.Feet, Units.Meters);
        case AccelerationUnits.None: return 1;
      }
      return 1;
    }

    private static double GetConversionFactorFromSI(string to)
    {
      switch (to)
      {
        case AccelerationUnits.FeetPerSecondSquare: return Units.GetConversionFactor(Units.Meters, Units.Feet);
        case AccelerationUnits.None: return 1;
      }
      return 1;
    }

    public static string GetUnitsFromString(string unit)
    {
      if (unit == null) return null;
      switch (unit.ToLower())
      {
        case "m/s2":
          return AccelerationUnits.MetersPerSecondSquare;
        case "ft/s2":
          return AccelerationUnits.FeetPerSecondSquare;
        case "none":
          return AccelerationUnits.None;
      }

      throw new Exception($"Cannot understand what unit {unit} is.");
    }

    public static int GetEncodingFromUnit(string unit)
    {
      switch (unit)
      {
        case MetersPerSecondSquare: return 1;
        case FeetPerSecondSquare: return 2;
      }

      return 0;
    }

    public static string GetUnitFromEncoding(double unit)
    {
      switch (unit)
      {
        case 1: return MetersPerSecondSquare;
        case 2: return FeetPerSecondSquare;
      }

      return None;
    }
  }

  public static class PressureUnits
  {
    public const string Pascal = "Pa";
    public const string Kilopascal = "kPa";
    public const string Megapascal = "MPa";
    public const string Gigapascal = "GPa";
    public const string PoundPerSquareFoot = "psf";
    public const string PoundPerSquareInch = "psi";
    public const string None = "none";

    public static double GetConversionFactor(string from, string to)
    {
      from = GetUnitsFromString(from);
      to = GetUnitsFromString(to);

      return GetConversionFactorToSI(from) * GetConversionFactorFromSI(to);
    }

    private static double GetConversionFactorToSI(string from)
    {
      switch (from)
      {
        case PressureUnits.Kilopascal: return 1e3;
        case PressureUnits.Megapascal: return 1e6;
        case PressureUnits.Gigapascal: return 1e9;
        case PressureUnits.PoundPerSquareFoot: return ForceUnits.GetConversionFactor(ForceUnits.PoundForce,ForceUnits.Newton) / Math.Pow(Units.GetConversionFactor(Units.Feet, Units.Meters), 2);
        case PressureUnits.PoundPerSquareInch: return ForceUnits.GetConversionFactor(ForceUnits.PoundForce, ForceUnits.Newton) / Math.Pow(Units.GetConversionFactor(Units.Inches, Units.Meters), 2);
        case PressureUnits.None: return 1;
      }
      return 1;
    }

    private static double GetConversionFactorFromSI(string to)
    {
      switch (to)
      {
        case PressureUnits.Kilopascal: return 1e-3;
        case PressureUnits.Megapascal: return 1e-6;
        case PressureUnits.Gigapascal: return 1e-9;
        case PressureUnits.PoundPerSquareFoot: return ForceUnits.GetConversionFactor(ForceUnits.Newton, ForceUnits.PoundForce) / Math.Pow(Units.GetConversionFactor(Units.Meters, Units.Feet), 2);
        case PressureUnits.PoundPerSquareInch: return ForceUnits.GetConversionFactor(ForceUnits.Newton, ForceUnits.PoundForce) / Math.Pow(Units.GetConversionFactor(Units.Meters, Units.Inches), 2);
        case PressureUnits.None: return 1;
      }
      return 1;
    }

    public static string GetUnitsFromString(string unit)
    {
      if (unit == null) return null;
      switch (unit.ToLower())
      {
        case "pa":
        case "kg/m/s2":
        case "n/m2":
        case "pascal":
        case "pascals":
          return PressureUnits.Pascal;
        case "kpa":
        case "kn/m2":
        case "kilopascal":
        case "kilopascals":
          return PressureUnits.Kilopascal;
        case "mpa":
        case "mn/m2":
        case "n/mm2":
        case "megapascal":
        case "megapascals":
          return PressureUnits.Megapascal;
        case "gpa":
        case "gn/m2":
        case "kn/mm2":
        case "gigapascal":
        case "gigapascals":
          return PressureUnits.Gigapascal;
        case "psf":
          return PressureUnits.PoundPerSquareFoot;
        case "psi":
          return PressureUnits.PoundPerSquareInch;
        case "none":
          return PressureUnits.None;
      }

      throw new Exception($"Cannot understand what unit {unit} is.");
    }

    public static int GetEncodingFromUnit(string unit)
    {
      switch (unit)
      {
        case Pascal: return 1;
        case Kilopascal: return 2;
        case Megapascal: return 3;
        case Gigapascal: return 4;
        case PoundPerSquareFoot: return 5;
        case PoundPerSquareInch: return 6;
      }

      return 0;
    }

    public static string GetUnitFromEncoding(double unit)
    {
      switch (unit)
      {
        case 1: return Pascal;
        case 2: return Kilopascal;
        case 3: return Megapascal;
        case 4: return Gigapascal;
        case 5: return PoundPerSquareFoot;
        case 6: return PoundPerSquareInch;
      }

      return None;
    }
  }

  //Velocity
  //Density
  //Moment

  #endregion
}
