using System;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.Revit.HostApp;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Connectors.Revit.Bindings;

internal abstract class RevitBaseBinding : IBinding, IDisposable
{
  // POC: name and bridge might be better for them to be protected props?
  public string Name { get; protected set; }
  public IBridge Parent { get; protected set; }

  private bool _disposed = false;

  protected readonly RevitDocumentStore _store;
  protected readonly RevitContext _revitContext;

  public RevitBaseBinding(string name, RevitDocumentStore store, IBridge bridge, RevitContext revitContext)
  {
    Name = name;
    Parent = bridge;
    _store = store;
    _revitContext = revitContext;
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing)
  {
    if (disposing && !_disposed)
    {
      // give subclasses the chance to dispose
      Disposing(disposing, _disposed);

      _disposed = true;
    }
  }

  protected virtual void Disposing(bool isDipsosing, bool disposedState) { }

  // might be needed in future...
  ~RevitBaseBinding()
  {
    // POC: is there anything janky about calling virtuals during finalizer? :thinking-face
    Dispose(false);
  }
}
