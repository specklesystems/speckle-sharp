using System;
using System.Collections.Generic;
using Speckle.Newtonsoft.Json;

namespace Speckle.Connectors.DUI.Bridge;

// POC: NEXT STEPS
// 1. complete browser sender, decoupling the Send and the need for the binding to know it's parent
// 2. Refactor bridge and inject the various actions
// 3. Fix UIApplication references not to accept the RevitPlugin type
// 4. At some point... possible above, understand the serializer optionsw

public class BrowserSender : IBrowserSender
{
  // not keen on this Action<string>
  private readonly IBrowserScriptExecuter _browserScriptExecuter;
  private readonly JsonSerializerSettings _jsonSerializerSettings;

  public BrowserSender(IBrowserScriptExecuter browserScriptExecuter, JsonSerializerSettings jsonSerializerSettings)
  {
    _browserScriptExecuter = browserScriptExecuter;
    _jsonSerializerSettings = jsonSerializerSettings;
  }

  public void Send(string frontendBoundName, string eventName)
  {
    var script = $"{frontendBoundName}.emit('{eventName}')";
    _browserScriptExecuter.Execute(script);
  }

  public void Send<T>(string frontendBoundName, string eventName, T data)
    where T : class
  {
    string payload = JsonConvert.SerializeObject(data, _jsonSerializerSettings);
    var script = $"{frontendBoundName}.emit('{eventName}', '{payload}')";
    _browserScriptExecuter.Execute(script);
  }

  public void SendRaw(string script)
  {
    _browserScriptExecuter.Execute(script);
  }
}
