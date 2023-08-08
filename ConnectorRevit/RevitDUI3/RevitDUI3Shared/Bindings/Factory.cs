using System.Collections.Generic;
using Autodesk.Revit.UI;
using DUI3;
using DUI3.Bindings;
using Speckle.ConnectorRevitDUI3.Utils;

namespace Speckle.ConnectorRevitDUI3.Bindings;

public static class Factory
{
  public static List<IBinding> CreateBindings(UIApplication revitApp, RevitDocumentStore store)
  {
    var baseBinding = new BasicConnectorBindingRevit(revitApp, store);
    
    var bindingsList = new List<IBinding>
    {
      new ConfigBinding(),
      new AccountBinding(),
      new TestBinding(),
      baseBinding
    };
      
    return bindingsList;
  }
}
