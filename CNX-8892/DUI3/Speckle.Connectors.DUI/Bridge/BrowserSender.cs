using System;
using Speckle.Newtonsoft.Json;

namespace Speckle.Connectors.DUI.Bridge;

// POC: not sure we'll need this... :/
public class BrowserSender : IBrowserSender
{
  // not keen on this Action<string>
  private readonly JsonSerializerSettings _jsonSerializerSettings;
  private Action<string>? _scriptMethod;

  public BrowserSender(JsonSerializerSettings jsonSerializerSettings, Action<string> scriptMethod)
  {
    _jsonSerializerSettings = jsonSerializerSettings;
    _scriptMethod = scriptMethod;
  }

  public void Send(string frontendBoundName, string eventName)
  {
    var script = $"{frontendBoundName}.emit('{eventName}')";

    SendRaw(script);
  }

  public void Send<T>(string frontendBoundName, string eventName, T data)
    where T : class
  {
    string payload = JsonConvert.SerializeObject(data, _jsonSerializerSettings);
    var script = $"{frontendBoundName}.emit('{eventName}', '{payload}')";

    SendRaw(script);
  }

  public void SendRaw(string script)
  {
    // POC: tight coupling here?
    _scriptMethod!(script);
  }
}
