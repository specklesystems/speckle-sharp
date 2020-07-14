using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Objects.Revit
{
  public class RevitColumn : Column
  {
    public string family { get; set; }
    public Level topLevel { get; set; }
    public double baseOffset { get; set; }
    public double topOffset { get; set; }
    public Dictionary<string,object> parameters { get; set; }
  }
}
