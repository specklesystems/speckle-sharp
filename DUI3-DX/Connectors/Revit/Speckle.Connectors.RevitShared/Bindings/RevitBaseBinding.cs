using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.Revit.HostApp;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Connectors.Revit.Bindings;

internal abstract class RevitBaseBinding : IBinding
{
  // POC: name and bridge might be better for them to be protected props?
  public string Name { get; protected set; }
  public IBridge Parent { get; protected set; }

  protected readonly RevitDocumentStore _store;
  protected readonly RevitContext _revitContext;

  public RevitBaseBinding(string name, RevitDocumentStore store, IBridge bridge, RevitContext revitContext)
  {
    Name = name;
    Parent = bridge;
    _store = store;
    _revitContext = revitContext;
  }
}
