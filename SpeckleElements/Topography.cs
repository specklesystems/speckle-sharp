using Speckle.Elements.Geometry;
using Speckle.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Elements
{
  public class Topography : Base
  {
    public Mesh baseGeometry { get; set; } = new Mesh();
    public Topography()
    {
      
    }

  }
}
