using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Connectors.Revit.Bindings;

internal abstract class RevitBaseBinding : IBinding
{
  // POC: name and bridge might be better for them to be protected props?
  public string Name { get; protected set; }
  public IBridge Parent { get; protected set; }

  protected readonly DocumentModelStore _store;
  protected readonly RevitContext _revitContext;

  public RevitBaseBinding(string name, DocumentModelStore store, IBridge bridge, RevitContext revitContext)
  {
    Name = name;
    Parent = bridge;
    _store = store;
    _revitContext = revitContext;
  }
}
