using Objects.Geometry;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects
{
  public class Roof : Element
  {
    public List<ICurve> holes { get; set; } = new List<ICurve>();
    public Roof()
    {

    }
  }
}
