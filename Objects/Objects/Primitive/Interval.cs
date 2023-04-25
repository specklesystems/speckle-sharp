using System;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.Primitive;

public class Interval : Base
{
  public Interval() { }

  public Interval(double start, double end)
  {
    this.start = start;
    this.end = end;
  }

  public double? start { get; set; }
  public double? end { get; set; }

  [JsonIgnore]
  public double Length => Math.Abs((end ?? 0) - (start ?? 0));

  public override string ToString()
  {
    return base.ToString() + $"[{start}, {end}]";
  }
}
