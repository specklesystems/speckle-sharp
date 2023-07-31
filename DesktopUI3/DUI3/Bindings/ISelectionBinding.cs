using System.Collections.Generic;


namespace DUI3.Bindings;

public interface ISelectionBinding : IBinding
{
  public SelectionInfo GetSelection();
}

public static class SelectionBindingEvents
{
  public static readonly string SetSelection = "setSelection";
}

public class SelectionInfo
{
  public List<string> ObjectIds { get; set; }
  public string Summary { get; set; }
}

