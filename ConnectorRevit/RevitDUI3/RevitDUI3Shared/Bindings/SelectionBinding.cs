using DUI3;
using DUI3.Bindings;

namespace Speckle.ConnectorRevitDUI3.Bindings;

public class SelectionBinding : ISelectionBinding
{
  public string Name { get; set; }
  public IBridge Parent { get; set; }
  
  public SelectionInfo GetSelection()
  {
    throw new System.NotImplementedException();
  }
}
