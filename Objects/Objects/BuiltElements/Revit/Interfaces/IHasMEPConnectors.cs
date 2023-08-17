using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements.Revit.Interfaces
{
  public interface IHasMEPConnectors
  {
    List<RevitMEPConnector> Connectors { get; set; }
  }
}
