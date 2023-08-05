using System.Collections.Generic;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;

namespace ConnectorRhinoWebUI.Bindings;

/// <summary>
/// Creates the required bindings, in the correct order, and scaffolds any dependencies.
/// </summary>
public static class Factory
{
  public static List<IBinding> CreateBindings()
  {
    var documentState = new DocumentModelStore();
      
    var baseBindings = new BasicConnectorBindingRhino(documentState);
    // TODO: sendBindings
    var selectionBindings = new SelectionBinding();

    var bindingsList = new List<IBinding>
    {
      new ConfigBinding(),
      new AccountBinding(),
      baseBindings,
      selectionBindings
    };
      
    return bindingsList;
  }
}
