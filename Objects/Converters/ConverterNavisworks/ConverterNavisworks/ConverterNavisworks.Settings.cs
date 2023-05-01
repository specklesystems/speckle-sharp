using System;
using System.Collections.Generic;
using System.Globalization;
using Autodesk.Navisworks.Api;

namespace Objects.Converter.Navisworks;

public partial class ConverterNavisworks
{
  public enum Transforms
  {
    Default,
    ProjectBasePoint,
    BoundingBox
  }

  // CAUTION: these strings need to have the same values as in the converter
  private const string InternalOrigin = "Model Origin (default)";
  private const string ProxyOrigin = "Project Base Origin";
  private const string BBoxOrigin = "Boundingbox Origin";


  private static Dictionary<string, string> Settings { get; } = new();


  private static Vector2D ProjectBasePoint
  {
    get
    {
      if (!Settings.ContainsKey("x-coordinate") || !Settings.ContainsKey("y-coordinate"))
        return new Vector2D(0, 0);

      var x = Settings["x-coordinate"];
      var y = Settings["y-coordinate"];

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
      if (!Settings.ContainsKey("reference-point"))
        return Transforms.Default;
      var value = Settings["reference-point"];

      return value switch
      {
        ProxyOrigin => Transforms.ProjectBasePoint,
        BBoxOrigin => Transforms.BoundingBox,
        InternalOrigin => Transforms.Default,
        _ => Transforms.Default
      };
    }
  }

  private static Units CoordinateUnits
  {
    get
    {
      if (!Settings.ContainsKey("units"))
        return Units.Meters;
      var value = Settings["units"];

      return (Units)Enum.Parse(typeof(Units), value, true);
    }
  }
}
