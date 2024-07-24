using System;
using System.Collections.Generic;
using System.Globalization;
using Autodesk.Navisworks.Api;

namespace Objects.Converter.Navisworks;

// ReSharper disable once UnusedType.Global
public partial class ConverterNavisworks
{
  public enum Transforms
  {
    Default,
    ProjectBasePoint,
    BoundingBox
  }

  // CAUTION: these strings need to have the same values as in the converter
  private const string INTERNAL_ORIGIN = "Model Origin (default)";
  private const string PROXY_ORIGIN = "Project Base Origin";
  private const string BBOX_ORIGIN = "Boundingbox Origin";

  private static Dictionary<string, string> Settings { get; } = new();

  private static Vector2D ProjectBasePoint
  {
    get
    {
      if (!Settings.TryGetValue("x-coordinate", out string x) || !Settings.TryGetValue("y-coordinate", out string y))
      {
        return new Vector2D(0, 0);
      }

      return new Vector2D(
        Convert.ToDouble(x, CultureInfo.InvariantCulture),
        Convert.ToDouble(y, CultureInfo.InvariantCulture)
      );
    }
  }

  private static Transforms ModelTransform
  {
    get
    {
      if (!Settings.TryGetValue("reference-point", out string referencePoint))
      {
        return Transforms.Default;
      }

      return referencePoint switch
      {
        PROXY_ORIGIN => Transforms.ProjectBasePoint,
        BBOX_ORIGIN => Transforms.BoundingBox,
        INTERNAL_ORIGIN => Transforms.Default,
        _ => Transforms.Default
      };
    }
  }

  private static Units CoordinateUnits
  {
    get
    {
      if (!Settings.TryGetValue("units", out string units))
      {
        return Units.Meters;
      }

      return (Units)Enum.Parse(typeof(Units), units, true);
    }
  }

  private static bool ExcludeProperties
  {
    get
    {
      if (!Settings.TryGetValue("exclude-properties", out string shouldExcludeProperties))
      {
        return false;
      }

      return shouldExcludeProperties == "True";
    }
  }

  private static bool IncludeInternalProperties
  {
    get
    {
      if (!Settings.TryGetValue("internal-properties", out string shouldIncludeInternalProperties))
      {
        return false;
      }

      return shouldIncludeInternalProperties == "True";
    }
  }

  private static bool UseInternalPropertyNames
  {
    get
    {
      if (!Settings.TryGetValue("internal-property-names", out string useInternalPropertyNames))
      {
        return false;
      }

      return useInternalPropertyNames == "True";
    }
  }
}
