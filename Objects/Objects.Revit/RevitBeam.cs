using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;
using Objects.Geometry;

namespace Objects.Revit
{
  public class RevitBeam : RevitFamilyElement, IBeam
  {
    public ICurve baseLine { get; set; }
  }
}
