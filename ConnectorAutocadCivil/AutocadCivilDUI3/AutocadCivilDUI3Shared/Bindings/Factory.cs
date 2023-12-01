using System.Collections.Generic;
using AutocadCivilDUI3Shared.Utils;
using ConnectorAutocadDUI3.Bindings;
using DUI3;
using DUI3.Bindings;
using Speckle.ConnectorAutocadDUI3.Bindings;

namespace AutocadCivilDUI3Shared.Bindings;

public static class Factory
{
  private static readonly AutocadDocumentModelStore s_store = new();

  public static List<IBinding> CreateBindings()
  {
    BasicConnectorBindingAutocad baseBindings = new(s_store);
    SendBinding sendBindings = new(s_store);
    ReceiveBinding receiveBindings = new(s_store);
    SelectionBinding selectionBinding = new();

    List<IBinding> bindingsList =
      new()
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
