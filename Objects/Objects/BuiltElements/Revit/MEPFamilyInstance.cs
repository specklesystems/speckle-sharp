using System.Collections.Generic;
using Objects.BuiltElements.Revit.Interfaces;
using Objects.Organization;
using Objects.Other.Revit;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements.Revit
{
  public class RevitMEPFamilyInstance : RevitInstance, IHasMEPConnectors
  {
    public string RevitPartType { get; set; }

    [DetachProperty]
    public List<RevitMEPConnector> Connectors { get; set; } = new();

    [JsonIgnore]
    public Graph Graph { get; set; }
  }
}
