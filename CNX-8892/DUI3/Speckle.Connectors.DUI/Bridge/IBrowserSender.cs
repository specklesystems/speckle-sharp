using System;

namespace Speckle.Connectors.DUI.Bridge;

// POC: not keen on this, relationship between this and IBrowserScriptExecutor could be something I could collapse
public interface IBrowserSender
{
  void Send<T>(string frontEndName, string eventName, T data)
    where T : class;

  void Send(string frontEndName, string eventName);

  void SendRaw(string script);
}
