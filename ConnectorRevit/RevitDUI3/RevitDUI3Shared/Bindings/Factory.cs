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
    ReceiveBinding receiveBinding = new(store);
    List<IBinding> bindingsList =
      new()
      {
        new ConfigBinding("Revit"),
        new AccountBinding(),
        new TestBinding(),
        baseBinding,
        selectionBinding,
        sendBinding,
        receiveBinding
      };

    return bindingsList;
  }
}
