using System.Collections.Generic;
using Autodesk.Revit.UI;
using DUI3;
using DUI3.Bindings;
using Speckle.ConnectorRevitDUI3.Utils;

namespace Speckle.ConnectorRevitDUI3.Bindings;

public static class Factory
{
  public static List<IBinding> CreateBindings(RevitDocumentStore store)
  {
    var baseBinding = new BasicConnectorBindingRevit(store);
    var selectionBinding = new SelectionBinding();
    var sendBinding = new SendBinding(store);
    var receiveBinding = new ReceiveBinding(store);
    var bindingsList = new List<IBinding>
    {
      new ConfigBinding(),
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
