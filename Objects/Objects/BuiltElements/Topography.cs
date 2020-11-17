using Objects.Geometry;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{
  public class Topography : Base
  {
    public Mesh baseGeometry { get; set; } = new Mesh();
    public Topography()
    {

    }

  }
}
