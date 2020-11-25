using Objects.Geometry;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{
  public class Roof : IRoof
  {
    public ICurve outline { get; set; }

    public List<ICurve> voids { get; set; } = new List<ICurve>();

    public Roof()
    {

    }
  }
}
