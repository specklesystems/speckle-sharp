using Speckle.Objects.Geometry;
using Speckle.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Objects
{
  public class Floor : Element
  {
    public List<ICurve> holes { get; set; } = new List<ICurve>();
    public Floor()
    {

    }
  }
}
