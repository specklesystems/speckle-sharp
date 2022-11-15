using System;
using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements
{
  /// <summary>
  /// A fitting for MEP elements
  /// </summary>
  public class Fitting : Base, IDisplayValue<List<Mesh>>
  {
    public FittingType type { get; set; }

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    public string units { get; set; }

    public Fitting() { }
  }

  public enum FittingType
  {
    Cross,
    Elbow,
    Tap,
    Tee,
    Transition,
    Union,
    Unknown
  }
}
