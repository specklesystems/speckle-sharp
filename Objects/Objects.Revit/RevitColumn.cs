using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;

namespace Objects.Revit
{
  public class RevitColumn : Column, IRevitElement
  {
    public RevitLevel topLevel { get; set; }
    public RevitLevel level { get; set; }
    public double baseOffset { get; set; }
    public double topOffset { get; set; }
    public string family { get; set; }
    public string type { get; set; }
    public Dictionary<string, object> parameters { get; set; }
    public Dictionary<string, object> typeParameters { get; set; }
    public string elementId { get; set; }

    public bool facingFlipped { get; set; }
    public bool handFlipped { get; set; }
    public bool isSlanted { get; set; }
    public bool structural { get; set; }
    public double rotation { get; set; }
  }
}
