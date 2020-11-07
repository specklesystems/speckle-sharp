using Objects.Geometry;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{
  public class Floor : Element
  {
    public List<ICurve> voids { get; set; } = new List<ICurve>();
    public Floor()
    {

    }
  }
}
