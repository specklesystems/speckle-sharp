using Speckle.Objects.Geometry;
using Speckle.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Objects
{
  public class Roof : Element
  {
    public List<ICurve> holes { get; set; } = new List<ICurve>();
    public Roof()
    {

    }
  }
}
