using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;
using Objects.Geometry;
using Speckle.Core.Kits;

namespace Objects.Revit
{
  [SchemaDescription("A Revit beam by line")]
  public class RevitBeam : RevitElement, IBeam
  {
    public ICurve baseLine { get; set; }
  }
}
