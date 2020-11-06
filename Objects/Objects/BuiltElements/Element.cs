using Objects.Geometry;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects
{
  public class Element : Base
  {
    public Mesh displayMesh { get; set; } = new Mesh();

    public string linearUnits { get; set; } 
  }

  public interface IRevitElement
  {
    string family { get; set; }
    string type { get; set; }
  }

}
