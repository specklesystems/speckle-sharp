using Speckle.Elements.Geometry;
using Speckle.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Elements
{
  public class Element : Base
  {
    public IGeometry baseGeometry { get; set; }
    public Mesh displayMesh { get; set; } = new Mesh();
    public string type { get; set; }
    public Level level { get; set; }
  }
}
