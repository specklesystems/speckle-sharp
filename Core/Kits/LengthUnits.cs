using System;
namespace Speckle.Core.Kits
{
  public static class LengthUnits
  {
    public const string Millimiters = "mm";
    public const string Centimeters = "cm";
    public const string Meters = "m";
    public const string Kilometers = "km";
    public const string Inches = "in";
    public const string Feet = "ft"; // smelly ones
    public const string Miles = "mi";

    public static double GetConversionFactor(string from, string to)
    {
      from = GetUnitsFromString(from);
      to = GetUnitsFromString(to);

      switch (from)
      {
        // METRIC
        case LengthUnits.Millimiters:
          switch (to)
          {
            case LengthUnits.Centimeters: return 0.1;
            case LengthUnits.Meters: return 0.001;
            case LengthUnits.Kilometers: return 1e-6;
            case LengthUnits.Inches: return 0.0393701;
            case LengthUnits.Feet: return 0.00328084;
            case LengthUnits.Miles: return 6.21371e-7;
          }
          break;
        case LengthUnits.Centimeters:
          switch (to)
          {
            case LengthUnits.Millimiters: return 10;
            case LengthUnits.Meters: return 0.01;
            case LengthUnits.Kilometers: return 1e-5;
            case LengthUnits.Inches: return 0.393701;
            case LengthUnits.Feet: return 0.0328084;
            case LengthUnits.Miles: return 6.21371e-6;
          }
          break;
        case LengthUnits.Meters:
          switch (to)
          {
            case LengthUnits.Millimiters: return 1000;
            case LengthUnits.Centimeters: return 100;
            case LengthUnits.Kilometers: return 1000;
            case LengthUnits.Inches: return 39.3701;
            case LengthUnits.Feet: return 3.28084;
            case LengthUnits.Miles: return 0.000621371;
          }
          break;
        case LengthUnits.Kilometers:
          switch (to)
          {
            case LengthUnits.Millimiters: return 1000000;
            case LengthUnits.Centimeters: return 100000;
            case LengthUnits.Meters: return 1000;
            case LengthUnits.Inches: return 39370.1;
            case LengthUnits.Feet: return 3280.84;
            case LengthUnits.Miles: return 0.621371;
          }
          break;

        // IMPERIAL
        case LengthUnits.Inches:
          switch (to)
          {
            case LengthUnits.Millimiters: return 25.4;
            case LengthUnits.Centimeters: return 2.54;
            case LengthUnits.Meters: return 0.0254;
            case LengthUnits.Kilometers: return 2.54e-5;
            case LengthUnits.Feet: return 0.0833333;
            case LengthUnits.Miles: return 1.57828e-5;
          }
          break;
        case LengthUnits.Feet:
          switch (to)
          {
            case LengthUnits.Millimiters: return 304.8;
            case LengthUnits.Centimeters: return 30.48;
            case LengthUnits.Meters: return 0.3048;
            case LengthUnits.Kilometers: return 0.0003048;
            case LengthUnits.Inches: return 12;
            case LengthUnits.Miles: return 0.000189394;
          }
          break;
        case LengthUnits.Miles:
          switch (to)
          {
            case LengthUnits.Millimiters: return 1.609e+6;
            case LengthUnits.Centimeters: return 160934;
            case LengthUnits.Meters: return 1609.34;
            case LengthUnits.Kilometers: return 1.60934;
            case LengthUnits.Inches: return 63360;
            case LengthUnits.Feet: return 5280;
          }
          break;
      }
      return 1;
    }

    public static string GetUnitsFromString(string unit)
    {
      switch (unit.ToLower())
      {
        case "mm":
        case "mil":
        case "millimiters":
        case "milimiters":
          return LengthUnits.Millimiters;
        case "cm":
        case "centimetre":
        case "centimeters":
          return LengthUnits.Centimeters;
        case "m":
        case "meter":
        case "meters":
          return LengthUnits.Meters;
      }
      throw new Exception($"Cannot understand what unit {unit} is.");
    }
  }
}
