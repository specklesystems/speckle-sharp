using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;
using Objects.Geometry;

namespace Objects.Revit
{
  public class RevitTopography : Topography, IRevitElement
  {
    public string family { get; set; }
    public string type { get; set; }
    public Dictionary<string, object> parameters { get; set; }
    public Dictionary<string, object> typeParameters { get; set; }
    public string elementId { get; set; }

    public RevitLevel level { get; set; }

    public new Mesh baseGeometry { get; set; } = new Mesh();
  }
}
