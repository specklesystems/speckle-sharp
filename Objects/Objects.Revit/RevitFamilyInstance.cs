using Objects.BuiltElements;
using Objects.Geometry;
using System.Collections.Generic;

namespace Objects.Revit
{
  public class RevitFamilyInstance : Element, IRevitElement
  {
    public Element host { get; set; }
    public string family { get; set; }
    public string type { get; set; }
    public Dictionary<string, object> parameters { get; set; }
    public Dictionary<string, object> typeParameters { get; set; }
    public string elementId { get; set; }
    public Point basePoint { get; set; }
    public RevitLevel level { get; set; }
    public int revitHostId { get; set; }
    public bool facingFlipped { get; set; }
    public bool handFlipped { get; set; }
    public double rotation { get; set; }
  }
}