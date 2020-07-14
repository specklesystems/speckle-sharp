using Speckle.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Objects.Primitive
{
  public class Interval : Base
  {
    public double? start { get; set; }
    public double? end { get; set; }

    public Interval() { }

    public Interval(double start, double end)
    {
      this.start = start;
      this.end = end;
    }
  }
}
