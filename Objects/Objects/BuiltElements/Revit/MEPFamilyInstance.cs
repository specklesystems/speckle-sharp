using System.Collections.Generic;
using Objects.BuiltElements.Revit.Interfaces;
using Objects.Other.Revit;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Revit
{
  public class RevitMEPFamilyInstance : RevitInstance, IHasMEPConnectors
  {
    public string RevitPartType { get; set; }

    [DetachProperty]
    public List<RevitMEPConnector> Connectors { get; set; } = new();
    public List<ICurve> Curves { get; set; } = new();
  }
}
