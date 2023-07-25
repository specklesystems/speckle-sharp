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
    [DetachProperty]
    public List<RevitMEPConnector> Connectors { get; set; } = new();

    [JsonIgnore]
    public Graph Graph { get; set; }
  }
}
