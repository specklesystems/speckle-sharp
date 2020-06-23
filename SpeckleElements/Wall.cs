using Speckle.Elements.Geometry;
using Speckle.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Elements
{
  public class Wall : Element
  {
    public Level topLevel { get; set; }
    public double height { get; set; }
    public Wall()
    {
      
    }

  }
}
