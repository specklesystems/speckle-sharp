using System.Collections.Generic;

namespace Objects.BuiltElements.Revit.Interfaces;

public interface IHasMEPConnectors
{
  List<RevitMEPConnector> Connectors { get; set; }
}
