using System.Collections.Generic;
using Objects.BuiltElements.Revit.Interfaces;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Revit;

public class RevitCableTray : CableTray, IHasMEPConnectors
{
  public string family { get; set; }
  public string type { get; set; }
  public Level level { get; set; }
  public Base parameters { get; set; }
  public string elementId { get; set; }
  public List<RevitMEPConnector> Connectors { get; set; } = new();
}
