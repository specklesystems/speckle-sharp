using System.Collections.Generic;
using DUI3;
using DUI3.Bindings;

namespace Speckle.ConnectorRevitDUI3.Bindings;

public class SendBinding : ISendBinding
{
  public string Name { get; set; }
  public IBridge Parent { get; set; }
  
  public List<ISendFilter> GetSendFilters()
  {
    throw new System.NotImplementedException();
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
