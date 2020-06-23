using Speckle.Elements.Primitive;
using Speckle.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Elements.Geometry
{
  public class Polycurve : Geometry
  {
    public List<Geometry> segments { get; set; }
    public Interval domain { get; set; }
    public bool closed { get; set; }

    public Polycurve()
    {

    }
  }
}
