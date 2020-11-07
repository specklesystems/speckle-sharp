using Objects.Geometry;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{
  public class Room : Element
  {
    // public ICurve baseGeometry { get; set; }
    //public string name { get; set; }
    public string number { get; set; }
    public double area { get; set; }
    public double volume { get; set; }

    public List<ICurve> holes { get; set; } = new List<ICurve>();
    // public Mesh displayMesh { get; set; } = new Mesh();
    public Room()
    {

    }

  }
}
