using Speckle.Elements.Primitive;
using Speckle.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Elements.Geometry
{
  public class Polycurve : Base, ICurve
  {
    public List<ICurve> segments { get; set; } = new List<ICurve>();
    public Interval domain { get; set; }
    public bool closed { get; set; }

    public Polycurve()
    {

    }
  }
}
