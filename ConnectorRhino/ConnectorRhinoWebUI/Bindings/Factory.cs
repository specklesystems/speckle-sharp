using System.Collections.Generic;
using ConnectorRhinoWebUI.Utils;
using DUI3;
using DUI3.Bindings;

namespace ConnectorRhinoWebUI.Bindings;

/// <summary>
/// Creates the required bindings, in the correct order, and scaffolds any dependencies.
/// </summary>
public static class Factory
{
  private static readonly RhinoDocumentStore Store = new RhinoDocumentStore();
  public static List<IBinding> CreateBindings()
  {
    var baseBindings = new BasicConnectorBinding(Store);
    var sendBindings = new SendBinding(Store);
    // TODO: expiryBindings (?) maybe part of sendBindings after all...
    // TODO: receiveBindings
    var selectionBindings = new SelectionBinding();

    var bindingsList = new List<IBinding>
    {
      new ConfigBinding(),
      new AccountBinding(),
      new TestBinding(),
      baseBindings,
      sendBindings,
      selectionBindings
    };
      
    return bindingsList;
  }
}


