using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{
  public class Topography : Base
  {
    public Mesh baseGeometry { get; set; } = new Mesh();
    public Topography() { }
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitTopography : Topography
  {
    [SchemaIgnore]
    public string elementId { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> parameters { get; set; }
  }

}
