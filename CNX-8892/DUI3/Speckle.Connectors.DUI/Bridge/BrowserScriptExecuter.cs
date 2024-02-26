using System;

namespace Speckle.Connectors.DUI.Bridge;

// POC: could maybe come out, depends how complex BrowserSender gets
public class BrowserScriptExecuter : IBrowserScriptExecuter
{
  private readonly Action<string> _executeScriptAsync;

  public BrowserScriptExecuter(Action<string> executeScriptAsync)
  {
    _executeScriptAsync = executeScriptAsync;
  }

  public void Execute(string script)
  {
    _executeScriptAsync(script);
  }
}
