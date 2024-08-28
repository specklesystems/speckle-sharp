using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Speckle.Core.Kits;

public static class Units
{
  public const string Millimeters = "mm";
  public const string Centimeters = "cm";
  public const string Meters = "m";
  public const string Kilometers = "km";
  public const string Inches = "in";

  /// <summary>International Foot</summary>
  public const string Feet = "ft";
  public const string Yards = "yd";
  public const string Miles = "mi";
  public const string None = "none";

  /// <summary>US Survey foot, now not supported by Speckle, kept privately for backwards compatibility</summary>
  private const string USFeet = "us_ft";

  internal static readonly List<string> SupportedUnits =
    new() { Millimeters, Centimeters, Meters, Kilometers, Inches, Feet, Yards, Miles, None };

  /// <param name="unit"></param>
  /// <returns><see langword="true"/> if <paramref name="unit"/> is a recognised/supported unit string, otherwise <see langword="false"/></returns>
  public static bool IsUnitSupported(string unit)
  {
    return SupportedUnits.Contains(unit);
  }

  /// <summary>
  /// Gets the conversion factor from one unit system to another
  /// </summary>
  /// <param name="from">Semantic unit string for the units to convert from</param>
  /// <param name="to">Semantic unit string for the units to convert to</param>
  /// <exception cref="ArgumentOutOfRangeException">A <inheritdoc cref="GetUnitsFromString"/></exception>
  /// <returns>The scaling factor to convert from the <paramref name="from"/> units to the <paramref name="to"/> units, or 1 if either unit param is null or none</returns>
  [Pure]
  public static double GetConversionFactor(string? from, string? to)
  {
    string? fromUnits = GetUnitsFromString(from);
    string? toUnits = GetUnitsFromString(to);

    switch (fromUnits)
    {
      // METRIC
      case Millimeters:
        switch (toUnits)
        {
          case Centimeters:
            return 0.1;
          case Meters:
            return 0.001;
          case Kilometers:
            return 1e-6;
          case Inches:
            return 0.0393701;
          case Feet:
            return 0.00328084;
          case USFeet:
            return 0.0032808333;
          case Yards:
            return 0.00109361;
          case Miles:
            return 6.21371e-7;
        }
        break;
      case Centimeters:
        switch (toUnits)
        {
          case Millimeters:
            return 10;
          case Meters:
            return 0.01;
          case Kilometers:
            return 1e-5;
          case Inches:
            return 0.393701;
          case Feet:
            return 0.0328084;
          case USFeet:
            return 0.0328083333;
          case Yards:
            return 0.0109361;
          case Miles:
            return 6.21371e-6;
        }
        break;
      case Meters:
        switch (toUnits)
        {
          case Millimeters:
            return 1000;
          case Centimeters:
            return 100;
          case Kilometers:
            return 0.001;
          case Inches:
            return 39.3701;
          case Feet:
            return 3.28084;
          case USFeet:
            return 3.28083333;
          case Yards:
            return 1.09361;
          case Miles:
            return 0.000621371;
        }
        break;
      case Kilometers:
        switch (toUnits)
        {
          case Millimeters:
            return 1000000;
          case Centimeters:
            return 100000;
          case Meters:
            return 1000;
          case Inches:
            return 39370.1;
          case Feet:
            return 3280.84;
          case USFeet:
            return 3280.83333;
          case Yards:
            return 1093.61;
          case Miles:
            return 0.621371;
        }
        break;

      // IMPERIAL
      case Inches:
        switch (toUnits)
        {
          case Millimeters:
            return 25.4;
          case Centimeters:
            return 2.54;
          case Meters:
            return 0.0254;
          case Kilometers:
            return 2.54e-5;
          case Feet:
            return 0.0833333;
          case USFeet:
            return 0.0833331667;
          case Yards:
            return 0.027777694;
          case Miles:
            return 1.57828e-5;
        }
        break;
      case Feet:
        switch (toUnits)
        {
          case Millimeters:
            return 304.8;
          case Centimeters:
            return 30.48;
          case Meters:
            return 0.3048;
          case Kilometers:
            return 0.0003048;
          case Inches:
            return 12;
          case USFeet:
            return 0.999998;
          case Yards:
            return 0.333332328;
          case Miles:
            return 0.000189394;
        }
        break;
      case USFeet:
        switch (toUnits)
        {
          case Millimeters:
            return 120000d / 3937d;
          case Centimeters:
            return 12000d / 3937d;
          case Meters:
            return 1200d / 3937d;
          case Kilometers:
            return 1.2 / 3937d;
          case Inches:
            return 12.000024000000002;
          case Feet:
            return 1.000002;
          case Yards:
            return 1.000002 / 3d;
          case Miles:
            return 1.000002 / 5280d;
        }
        break;
      case Yards:
        switch (toUnits)
        {
          case Millimeters:
            return 914.4;
          case Centimeters:
            return 91.44;
          case Meters:
            return 0.9144;
          case Kilometers:
            return 0.0009144;
          case Inches:
            return 36;
          case Feet:
            return 3;
          case USFeet:
            return 2.999994;
          case Miles:
            return 1d / 1760d;
        }
        break;
      case Miles:
        switch (toUnits)
        {
          case Millimeters:
            return 1.609e+6;
          case Centimeters:
            return 160934;
          case Meters:
            return 1609.34;
          case Kilometers:
            return 1.60934;
          case Inches:
            return 63360;
          case Feet:
            return 5280;
          case USFeet:
            return 5279.98944002112;
          case Yards:
            return 1759.99469184;
        }
        break;
      case None:
        return 1;
    }
    return 1;
  }

  /// <summary>
  /// Given <paramref name="unit"/>, maps several friendly unit aliases to a a semantic unit string
  /// </summary>
  /// <param name="unit"></param>
  /// <returns>The semantic unit string, <see langword="null"/> if <paramref name="unit"/> is <see langword="null"/></returns>
  /// <exception cref="ArgumentOutOfRangeException">Unit string is not a supported unit (see <see cref="IsUnitSupported"/>)</exception>
  [Pure]
  public static string? GetUnitsFromString(string? unit)
  {
    if (string.IsNullOrWhiteSpace(unit))
    {
      return null;
    }

    return unit.ToLower() switch
    {
      "mm" or "mil" or "millimeter" or "millimeters" or "millimetres" => Millimeters,
      "cm" or "centimetre" or "centimeter" or "centimetres" or "centimeters" => Centimeters,
      "m" or "meter" or "metre" or "meters" or "metres" => Meters,
      "inches" or "inch" or "in" => Inches,
      "feet" or "foot" or "ft" => Feet,
      "ussurveyfeet" => USFeet, //BUG: why don't we match on "us_ft"?
      "yard" or "yards" or "yd" => Yards,
      "miles" or "mile" or "mi" => Miles,
      "kilometers" or "kilometer" or "km" => Kilometers,
      "none" => None,
      _ => throw new ArgumentOutOfRangeException(nameof(unit), $"Unrecognised unit string {unit}"),
    };
  }

  /// <summary>
  /// Maps semantic unit strings to a numeric encoding
  /// </summary>
  /// <param name="unit"></param>
  /// <remarks>non-recognised unit encodings will be silently mapped to <c>0</c></remarks>
  /// <returns></returns>
  [Pure]
  public static int GetEncodingFromUnit(string unit)
  {
    return unit switch
    {
      Millimeters => 1,
      Centimeters => 2,
      Meters => 3,
      Kilometers => 4,
      Inches => 5,
      Feet => 6,
      Yards => 7,
      Miles => 8,
      _ => 0,
    };
  }

  /// <summary>
  /// Maps a numeric encoding to the semantic unit string
  /// </summary>
  /// <param name="unit">numeric encoded unit</param>
  /// <remarks>non-recognised unit encodings will be silently mapped to <see cref="None"/></remarks>
  /// <returns>Semantic unit string</returns>
  [Pure]
  public static string GetUnitFromEncoding(double unit)
  {
    return unit switch
    {
      1 => Millimeters,
      2 => Centimeters,
      3 => Meters,
      4 => Kilometers,
      5 => Inches,
      6 => Feet,
      7 => Yards,
      8 => Miles,
      _ => None,
    };
  }
}
