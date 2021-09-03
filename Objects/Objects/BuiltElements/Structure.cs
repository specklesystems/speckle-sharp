using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Objects.BuiltElements
{
  public class Structure : Base, IDisplayMesh
  {
    public Point location { get; set; }
    public List<string> pipeIds { get; set; }

    [DetachProperty]
    public Mesh displayMesh { get; set; }

    public string units { get; set; }

    public Structure() { }
  }
}
