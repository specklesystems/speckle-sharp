using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{
  public class Level : Element, ILevel
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
