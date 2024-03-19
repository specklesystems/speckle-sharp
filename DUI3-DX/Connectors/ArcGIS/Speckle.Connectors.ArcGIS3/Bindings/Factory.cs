using System.Collections.Generic;
using ConnectorArcGIS.Utils;
using Speckle.Connectors.DUI.Bindings;

namespace ConnectorArcGIS.Bindings;

public static class Factory
{
  private static readonly ArcGisDocumentStore s_store = new();

  public static List<IBinding> CreateBindings()
  {
    BasicConnectorBinding baseBindings = new(s_store);
    List<IBinding> bindingsList =
      new() { new ConfigBinding("ArcGIS"), new AccountBinding(), new TestBinding(), baseBindings };

    return bindingsList;
  }
}
