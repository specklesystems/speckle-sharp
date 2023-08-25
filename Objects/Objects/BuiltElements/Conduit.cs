using System.Collections.Generic;
using Objects.BuiltElements.Revit.Interfaces;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.BuiltElements
{
  public class Conduit : Base, IDisplayValue<List<Mesh>>
  {
    public ICurve baseCurve { get; set; }
    public double diameter { get; set; }
    public double length { get; set; }

    public string units { get; set; }

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitConduit : Conduit, IHasMEPConnectors
  {
    public string family { get; set; }

    public string type { get; set; }

    public Level level { get; set; }

    public Base parameters { get; set; }

    public string elementId { get; set; }
    public List<RevitMEPConnector> Connectors { get; set; } = new();
  }
}
