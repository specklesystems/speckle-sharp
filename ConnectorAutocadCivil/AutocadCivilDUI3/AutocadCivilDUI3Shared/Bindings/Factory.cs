using System;
using System.Collections.Generic;
using System.Text;
using AutocadCivilDUI3Shared.Utils;
using ConnectorAutocadDUI3.Bindings;
using DUI3;
using DUI3.Bindings;
using Speckle.ConnectorAutocadDUI3.Bindings;

namespace AutocadCivilDUI3Shared.Bindings
{
  public static class Factory
  {
    private static readonly AutocadDocumentModelStore Store = new AutocadDocumentModelStore();

    public static List<IBinding> CreateBindings()
    {
      BasicConnectorBindingAutocad baseBindings = new BasicConnectorBindingAutocad(Store);
      SendBinding sendBindings = new SendBinding(Store);
      ReceiveBinding receiveBindings = new ReceiveBinding(Store);
      SelectionBinding selectionBinding = new SelectionBinding();

      var bindingsList = new List<IBinding>
      {
        new ConfigBinding("Autocad"),
        new AccountBinding(),
        new TestBinding(),
        baseBindings,
        sendBindings,
        receiveBindings,
        selectionBinding
      };

      return bindingsList;
    }
  }
}
