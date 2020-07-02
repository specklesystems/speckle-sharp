using Speckle.Elements.Geometry;
using Speckle.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Elements
{
  public class Room : Base
  {
    public ICurve baseGeometry { get; set; }
    public string name { get; set; }
    public string number { get; set; }
    public double area { get; set; }
    public double volume { get; set; }
    public Room()
    {
      
    }

  }
}
