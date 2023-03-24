using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Objects.Primitive;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.Geometry
{
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
    public Vector pitchAxis { get; set; } = new Vector();
    public double pitch { get; set; } = 0;
    public SpiralType spiralType { get; set; }

    [DetachProperty]
    public Polyline displayValue { get; set; }

    public Box bbox { get; set; }

    public double length { get; set; }

    public Interval domain { get; set; }

    public string units { get; set; }

    public Spiral() { }
  }
}
