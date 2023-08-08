using System;
using System.Collections.Generic;
using AutocadCivilDUI3Shared.Utils;
using DUI3;
using DUI3.Bindings;

namespace AutocadCivilDUI3Shared.Bindings
{
  public class SendBinding : IBinding
  {
    public string Name { get; set; } = "sendBinding";

    public IBridge Parent { get; set; }
    
    private AutocadDocumentModelStore _store;
    
    private HashSet<string> _changedObjectIds { get; set; } = new();

    public SendBinding(AutocadDocumentModelStore store)
    {
      _store = store;
    }

    public List<ISendFilter> GetSendFilters()
    {
      return new List<ISendFilter>()
      {
        new AutocadEverythingFilter(),
        new AutocadSelectionFilter()
      };
    }
  }
}
