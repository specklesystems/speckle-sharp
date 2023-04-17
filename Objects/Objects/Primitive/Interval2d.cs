using Speckle.Core.Models;

namespace Objects.Primitive;

public class Interval2d : Base
{
  public Interval2d() { }

  public Interval2d(Interval u, Interval v)
  {
    this.u = u;
    this.v = v;
  }

  public Interval2d(double start_u, double end_u, double start_v, double end_v)
  {
    u = new Interval(start_u, end_u);
    v = new Interval(start_v, end_v);
  }

  public Interval u { get; set; }
  public Interval v { get; set; }
}
