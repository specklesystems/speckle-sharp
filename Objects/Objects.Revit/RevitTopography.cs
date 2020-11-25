using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.Revit
{
  public class RevitTopography : Base, IBaseRevitElement, IRevitHasParameters, ITopography
  {
    public Mesh baseGeometry { get; set; } = new Mesh();

    public string elementId { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> parameters { get; set; }
  }
}
