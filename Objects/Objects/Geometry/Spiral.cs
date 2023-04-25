using Objects.Primitive;
using Speckle.Core.Models;

namespace Objects.Geometry;

public enum SpiralType
{
  Biquadratic,
  BiquadraticParabola,
  Bloss,
  Clothoid,
  Cosine,
  Cubic,
  CubicParabola,
  Radioid,
  Sinusoid,
  Unknown
}

public class Spiral : Base, ICurve, IHasBoundingBox, IDisplayValue<Polyline>
{
  public Point startPoint { get; set; }
  public Point endPoint { get; set; }
  public Plane plane { get; set; } // plane with origin at spiral center
  public double turns { get; set; } // total angle of spiral. positive is counterclockwise, negative is clockwise
  public Vector pitchAxis { get; set; } = new();
  public double pitch { get; set; } = 0;
  public SpiralType spiralType { get; set; }

  public string units { get; set; }

  public double length { get; set; }

  public Interval domain { get; set; }

  [DetachProperty]
  public Polyline displayValue { get; set; }

  public Box bbox { get; set; }
}
