using System.Collections.Generic;
using Autodesk.Revit.UI;
using DUI3;
using DUI3.Bindings;
using Speckle.ConnectorRevitDUI3.Utils;

namespace Speckle.ConnectorRevitDUI3.Bindings;

public class SendBinding : ISendBinding
{
  public string Name { get; set; } = "sendBinding";
  public IBridge Parent { get; set; }
  
  private RevitDocumentStore _store;

  private static UIApplication RevitApp;
  
  public SendBinding(RevitDocumentStore store)
  {
    RevitApp = RevitAppProvider.RevitApp;
    _store = store;
    // TODO expiry events
    // TODO filters need refresh events
  }
  
  public List<ISendFilter> GetSendFilters()
  {
    return new List<ISendFilter>
    {
      new RevitEverythingFilter(),
      new RevitSelectionFilter()
    };
  }

  public void Send(string modelId)
  {
    throw new System.NotImplementedException();
  }

  public void CancelSend(string modelId)
  {
    throw new System.NotImplementedException();
  }

  public void Highlight(string modelId)
  {
    throw new System.NotImplementedException();
  }
}
