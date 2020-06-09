using Speckle.Elements.Primitive;
using Speckle.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Elements.Primitive
{
  public class Interval2d : Base
  {
    public Interval u { get; set; }
    public Interval v { get; set; }

    public Interval2d() { }

    public Interval2d(Interval u, Interval v)
    {
      this.u = u;
      this.v = v;
    }

    public Interval2d(double start_u, double end_u, double start_v, double end_v)
    {
      this.u = new Interval(start_u, end_u);
      this.v = new Interval(start_v, end_v);

    }
  }
}
