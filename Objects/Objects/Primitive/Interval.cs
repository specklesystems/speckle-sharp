using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Newtonsoft.Json;

namespace Objects.Primitive
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

    public override string ToString()
    {
      return base.ToString() + $"[{this.start}, {this.end}]";
    }

    [JsonIgnore] public double Length => Math.Abs((end ?? 0) - (start ?? 0));
  }
}
