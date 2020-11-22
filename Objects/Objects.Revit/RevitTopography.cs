using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;
using Objects.Geometry;

namespace Objects.Revit
{
  public class RevitTopography : RevitElement, ITopography
  {
    public Mesh baseGeometry { get; set; } = new Mesh();
  }
}
