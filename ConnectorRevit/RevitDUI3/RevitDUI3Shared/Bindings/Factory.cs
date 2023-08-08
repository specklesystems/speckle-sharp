using System.Collections.Generic;
using DUI3;
using DUI3.Bindings;

namespace Speckle.ConnectorRevitDUI3.Bindings;

public static class Factory
{
  public static List<IBinding> CreateBindings()
  {
    var bindingsList = new List<IBinding>
    {
      new ConfigBinding(),
      new AccountBinding(),
      new TestBinding(),
      
    };
      
    return bindingsList;
  }
}
