using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;

namespace Objects.Revit
{
  public class RevitColumn : RevitElement, IColumn
  {
    public RevitLevel topLevel { get; set; }
    public double baseOffset { get; set; }
    public double topOffset { get; set; }
    public bool facingFlipped { get; set; }
    public bool handFlipped { get; set; }
    public bool isSlanted { get; set; }
    public bool structural { get; set; }
    public double rotation { get; set; }
    public double height { get; set; }

    public ICurve baseLine { get; set; }
  }
}
