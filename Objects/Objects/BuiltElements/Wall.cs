using Objects.Geometry;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{

  public class Wall : Base, IWall
  {
    public double height { get; set; }

    public ICurve baseLine { get; set; }

    public Wall() { }

  }
}
