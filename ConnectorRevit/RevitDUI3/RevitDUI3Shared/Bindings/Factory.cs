using System.Collections.Generic;
using DUI3;
using DUI3.Bindings;
using Speckle.ConnectorRevitDUI3.Utils;

namespace Speckle.ConnectorRevitDUI3.Bindings;

public static class Factory
{
  public static List<IBinding> CreateBindings(RevitDocumentStore store)
  {
    BasicConnectorBindingRevit baseBinding = new(store);
    SelectionBinding selectionBinding = new();
    SendBinding sendBinding = new(store);
    // TODO: Revit receive is very flaky right now, removing
    // ReceiveBinding receiveBinding = new(store);
    List<IBinding> bindingsList =
      new()
      {
        new ConfigBinding("Revit"),
        new AccountBinding(),
        new TestBinding(),
        baseBinding,
        selectionBinding,
        sendBinding,
        // receiveBinding // See above note on receives
      };

    return bindingsList;
  }
}
