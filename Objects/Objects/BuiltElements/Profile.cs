using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.BuiltElements
{
  public class Profile : Base, IDisplayValue<Polyline>
  {
    public List<ICurve> curves { get; set; }

    public string name { get; set; }

    public double startStation { get; set; }

    public double endStation { get; set; }

    public string units { get; set; }

    [DetachProperty]
    public Polyline displayValue { get; set; }
  }
}

namespace Objects.BuiltElements.Civil
{
  public class CivilProfile : Profile
  {
    public string type { get; set; }

    public string style { get; set; }

    public double offset { get; set; }

    /// <summary>
    /// Points of vertical intersection
    /// </summary>
    public List<Point> pvis { get; set; }

    /// <summary>
    /// Name of parent profile if this is an offset profile
    /// </summary>
    public string parent { get; set; }
  }
}
