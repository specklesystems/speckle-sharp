using System;
using System.Collections.Generic;
using System.Text;
using static Objects.Revit.RevitUtils;
using Objects.BuiltElements;
using Objects.Geometry;
using Speckle.Core.Kits;

namespace Objects.Revit
{
  public class DirectShape : Element, IRevitElement
  {
    public RevitCategory category { get; set; }
    public string family { get; set; }
    public string type { get; set; }
    public Dictionary<string, object> parameters { get; set; }
    public Dictionary<string, object> typeParameters { get; set; }
    public Mesh baseGeometry { get; set; }

    public RevitLevel level { get; set; }
    [SchemaBuilderIgnore]
    public string elementId { get; set; }
  }

  // pruned from: https://docs.google.com/spreadsheets/d/1uNa77XYLjeN-1c63gsX6C5D5Pvn_3ZB4B0QMgPeloTw/edit#gid=1549586957



}
