using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.Revit.HostApp;
using Speckle.Connectors.Revit.Plugin;

namespace Speckle.Connectors.Revit.Bindings;

internal abstract class RevitBaseBinding : IBinding
{
  // POC: name and bridge might be possible for them to be protected?
  public string Name { get; protected set; }
  public IBridge Bridge { get; protected set; }

  protected readonly RevitDocumentStore _store;
  protected readonly IBrowserSender _browserSender;
  protected readonly RevitContext _revitContext;

  public RevitBaseBinding(
    string name,
    RevitDocumentStore store,
    IBridge bridge,
    IBrowserSender browserSender,
    RevitContext revitContext
  )
  {
    Name = name;
    Bridge = bridge;
    _store = store;
    _browserSender = browserSender;
    _revitContext = revitContext;
  }
}
