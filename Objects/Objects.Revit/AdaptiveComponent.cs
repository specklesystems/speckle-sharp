using Objects.BuiltElements;
using Objects.Geometry;
using Speckle.Core.Kits;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Revit
{
  public class AdaptiveComponent : RevitElement, IRevit
  {
    public bool flipped { get; set; }
    public List<Point> basePoints { get; set; }

  }
}
