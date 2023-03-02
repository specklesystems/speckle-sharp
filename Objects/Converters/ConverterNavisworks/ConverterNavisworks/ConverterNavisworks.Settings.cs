using System;
using System.Collections.Generic;
using Autodesk.Navisworks.Api;

namespace Objects.Converter.Navisworks
{
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


    public static Dictionary<string, string> Settings { get; } = new Dictionary<string, string>();


    public Vector2D ProjectBasePoint
    {
      get
      {
        if (!Settings.ContainsKey("x-coordinate") || !Settings.ContainsKey("y-coordinate")) return new Vector2D(0, 0);

        var x = Settings["x-coordinate"];
        var y = Settings["y-coordinate"];

        return new Vector2D(Convert.ToDouble(x), Convert.ToDouble(y));
      }
    }

    public Transforms ModelTransform
    {
      get
      {
        if (!Settings.ContainsKey("reference-point")) return Transforms.Default;
        var value = Settings["reference-point"];

        switch (value)
        {
          case ProxyOrigin: return Transforms.ProjectBasePoint;
          case BBoxOrigin: return Transforms.BoundingBox;
          default:
            return Transforms.Default;
        }
      }
    }

    static Units CoordinateUnits
    {
      get
      {
        if (!Settings.ContainsKey("units")) return Units.Meters;
        var value = Settings["units"];

        return (Units)Enum.Parse(typeof(Units), value, true);
      }
    }
  }
}
