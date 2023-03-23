using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.Other
{
  public class Hatch : Base
  {
    [Obsolete("Use Loops instead")]
    public List<ICurve> curves { get; set; }
    public List<HatchLoop> loops { get; set; }
    public string pattern { get; set; }
    public double scale { get; set; } = 1;
    public double rotation { get; set; } = 0; // relative angle

    public Hatch() { }
  }

  /// <summary>
  /// Represents a Hatch Loop from a <see cref="Hatch"/>'s curve.
  /// </summary>
  public class HatchLoop : Base
  {
    public ICurve Curve { get; set; }
    public HatchLoopType Type { get; set; }

    public HatchLoop()
    {
    }

    public HatchLoop(ICurve curve, HatchLoopType type)
    {
      Curve = curve;
      Type = type;
    }
  }

  /// <summary>
  /// Represents the type of a loop in a <see cref="Hatch"/>'s curves.
  /// </summary>
  public enum HatchLoopType
  {
    Unknown,
    Outer,
    Inner
  }
}
