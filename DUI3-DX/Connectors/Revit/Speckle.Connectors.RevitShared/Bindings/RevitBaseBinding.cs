using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Converters.Revit2023.DependencyInjection;

namespace Speckle.Connectors.Revit.Bindings;

internal abstract class RevitBaseBinding : IBinding
{
  // POC: name and bridge might be better for them to be protected props?
  public string Name { get; protected set; }
  public IBridge Parent { get; protected set; }

  protected readonly DocumentModelStore Store;
  protected readonly RevitContext RevitContext;

  public RevitBaseBinding(string name, DocumentModelStore store, IBridge bridge, RevitContext revitContext)
  {
    Name = name;
    Parent = bridge;
    Store = store;
    RevitContext = revitContext;
  }
}
