using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using static Objects.Revit.RevitUtils;

namespace Objects.Revit
{
  [SchemaDescription("A DirectShape element by mesh")]
  public class DirectShape : Base, IBaseRevitElement
  {
    public string type { get; set; }
    
    // pruned from: https://docs.google.com/spreadsheets/d/1uNa77XYLjeN-1c63gsX6C5D5Pvn_3ZB4B0QMgPeloTw/edit#gid=1549586957
    public RevitCategory category { get; set; }
    
    public Mesh baseGeometry { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> parameters { get; set; }

    [SchemaIgnore]
    public string elementId { get; set; }
  }

}
