﻿using System;
using System.Collections.Generic;
using Speckle.Core.Logging;

namespace Speckle.Core.Kits
{
  public static class Units
  {
    public const string Millimeters = "mm";
    public const string Centimeters = "cm";
    public const string Meters = "m";
    public const string Kilometers = "km";
    public const string Inches = "in";
    public const string Feet = "ft"; // smelly ones
    public const string Yards = "yd";
    public const string Miles = "mi";
    public const string None = "none";

    private static List<string> SupportedUnits = new List<string>() { Millimeters, Centimeters, Meters, Kilometers, Inches, Feet, USFeet, Yards, Miles, None };

    public static bool IsUnitSupported(string unit) => SupportedUnits.Contains(unit);

    // public const string USInches = "us_in"; the smelliest ones, can add later if people scream "USA #1"
    public const string USFeet = "us_ft"; // it happened, absolutely gross
    // public const string USYards = "us_yd"; the smelliest ones, can add later if people scream "USA #1"
    // public const string USMiles = "us_mi"; the smelliest ones, can add later if people scream "USA #1"

    public static double GetConversionFactor(string from, string to)
    {
      from = GetUnitsFromString(from);
      to = GetUnitsFromString(to);

      switch (from)
      {
        // METRIC
        case Units.Millimeters:
          switch (to)
          {
            case Units.Centimeters:
              return 0.1;
            case Units.Meters:
              return 0.001;
            case Units.Kilometers:
              return 1e-6;
            case Units.Inches:
              return 0.0393701;
            case Units.Feet:
              return 0.00328084;
            case Units.USFeet:
              return 0.0032808333;
            case Units.Yards:
              return 0.00109361;
            case Units.Miles:
              return 6.21371e-7;
          }
          break;
        case Units.Centimeters:
          switch (to)
          {
            case Units.Millimeters:
              return 10;
            case Units.Meters:
              return 0.01;
            case Units.Kilometers:
              return 1e-5;
            case Units.Inches:
              return 0.393701;
            case Units.Feet:
              return 0.0328084;
            case Units.USFeet:
              return 0.0328083333;
            case Units.Yards:
              return 0.0109361;
            case Units.Miles:
              return 6.21371e-6;
          }
          break;
        case Units.Meters:
          switch (to)
          {
            case Units.Millimeters:
              return 1000;
            case Units.Centimeters:
              return 100;
            case Units.Kilometers:
              return 1000;
            case Units.Inches:
              return 39.3701;
            case Units.Feet:
              return 3.28084;
            case Units.USFeet:
              return 3.28083333;
            case Units.Yards:
              return 1.09361;
            case Units.Miles:
              return 0.000621371;
          }
          break;
        case Units.Kilometers:
          switch (to)
          {
            case Units.Millimeters:
              return 1000000;
            case Units.Centimeters:
              return 100000;
            case Units.Meters:
              return 1000;
            case Units.Inches:
              return 39370.1;
            case Units.Feet:
              return 3280.84;
            case Units.USFeet:
              return 3280.83333;
            case Units.Yards:
              return 1093.61;
            case Units.Miles:
              return 0.621371;
          }
          break;

        // IMPERIAL
        case Units.Inches:
          switch (to)
          {
            case Units.Millimeters:
              return 25.4;
            case Units.Centimeters:
              return 2.54;
            case Units.Meters:
              return 0.0254;
            case Units.Kilometers:
              return 2.54e-5;
            case Units.Feet:
              return 0.0833333;
            case Units.USFeet:
              return 0.0833331667;
            case Units.Yards:
              return 0.027777694;
            case Units.Miles:
              return 1.57828e-5;
          }
          break;
        case Units.Feet:
          switch (to)
          {
            case Units.Millimeters:
              return 304.8;
            case Units.Centimeters:
              return 30.48;
            case Units.Meters:
              return 0.3048;
            case Units.Kilometers:
              return 0.0003048;
            case Units.Inches:
              return 12;
            case Units.USFeet:
              return 0.999998;
            case Units.Yards:
              return 0.333332328;
            case Units.Miles:
              return 0.000189394;
          }
          break;
        case Units.USFeet:
          switch (to)
          {
            case Units.Millimeters:
              return 120000d / 3937d;
            case Units.Centimeters:
              return 12000d / 3937d;
            case Units.Meters:
              return 1200d / 3937d;
            case Units.Kilometers:
              return 1.2 / 3937d;
            case Units.Inches:
              return 12.000024000000002;
            case Units.Feet:
              return 1.000002;
            case Units.Yards:
              return 1.000002 / 3d;
            case Units.Miles:
              return 1.000002 / 5280d;
          }
          break;
        case Units.Yards:
          switch (to)
          {
            case Units.Millimeters:
              return 914.4;
            case Units.Centimeters:
              return 91.44;
            case Units.Meters:
              return 0.9144;
            case Units.Kilometers:
              return 0.0009144;
            case Units.Inches:
              return 36;
            case Units.Feet:
              return 3;
            case Units.USFeet:
              return 2.999994;
            case Units.Miles:
              return 1d / 1760d;
          }
          break;
        case Units.Miles:
          switch (to)
          {
            case Units.Millimeters:
              return 1.609e+6;
            case Units.Centimeters:
              return 160934;
            case Units.Meters:
              return 1609.34;
            case Units.Kilometers:
              return 1.60934;
            case Units.Inches:
              return 63360;
            case Units.Feet:
              return 5280;
            case Units.USFeet:
              return 5279.98944002112;
            case Units.Yards:
              return 1759.99469184;
          }
          break;
        case Units.None:
          return 1;
      }
      return 1;
    }

    public static string GetUnitsFromString(string unit)
    {
      if (unit == null) return null;
      switch (unit.ToLower())
      {
        case "mm":
        case "mil":
        case "millimeter":
        case "millimeters":
        case "millimetres":
          return Units.Millimeters;
        case "cm":
        case "centimetre":
        case "centimeter":
        case "centimetres":
        case "centimeters":
          return Units.Centimeters;
        case "m":
        case "meter":
        case "metre":
        case "meters":
        case "metres":
          return Units.Meters;
        case "inches":
        case "inch":
        case "in":
          return Units.Inches;
        case "feet":
        case "foot":
        case "ft":
          return Units.Feet;
        case "ussurveyfeet":
          return Units.USFeet;
        case "yard":
        case "yards":
        case "yd":
          return Units.Yards;
        case "miles":
        case "mile":
        case "mi":
          return Units.Miles;
        case "kilometers":
        case "kilometer":
        case "km":
          return Units.Kilometers;
        case "none":
          return Units.None;
      }

      throw new SpeckleException($"Cannot understand what unit {unit} is.");
    }

    public static int GetEncodingFromUnit(string unit)
    {
      switch (unit)
      {
        case Millimeters: return 1;
        case Centimeters: return 2;
        case Meters: return 3;
        case Kilometers: return 4;
        case Inches: return 5;
        case Feet: return 6;
        case Yards: return 7;
        case Miles: return 8;
      }

      return 0;
    }

    public static string GetUnitFromEncoding(double unit)
    {
      switch (unit)
      {
        case 1: return Millimeters;
        case 2: return Centimeters;
        case 3: return Meters;
        case 4: return Kilometers;
        case 5: return Inches;
        case 6: return Feet;
        case 7: return Yards;
        case 8: return Miles;
      }

      return None;
    }
  }
}
