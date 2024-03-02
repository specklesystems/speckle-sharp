using System.Collections.Generic;

namespace Speckle.Connectors.DUI.Bindings;

public interface ISelectionBinding : IBinding
{
  public SelectionInfo GetSelection();
}

public static class SelectionBindingEvents
{
  public const string SET_SELECTION = "setSelection";
}

public class SelectionInfo
{
  public List<string> SelectedObjectIds { get; set; }
  public string Summary { get; set; }
}
