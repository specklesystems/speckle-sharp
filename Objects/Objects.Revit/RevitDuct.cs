using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;
using Objects.Geometry;

namespace Objects.Revit
{
  public class RevitDuct : RevitElement, IDuct
  {
    public double width { get; set; }
    public double height { get; set; }
    public double diameter { get; set; }
    public double length { get; set; }
    public double velocity { get; set; }
    public string systemName { get; set; }
    public string systemType { get; set; }
    public Line baseLine { get; set; }
  }
}
