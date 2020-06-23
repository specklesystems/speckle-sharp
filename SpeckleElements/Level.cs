using Speckle.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Elements
{
  public class Level : Element
  {
    public string name { get; set; }
    public double elevation { get; set; }
    public Level()
    {
      
  }
    public Level(string name, double elevation)
    {
      this.name = name;
      this.elevation = elevation;
    }
  }
}
