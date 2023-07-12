using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DUI3
{
  /// <summary>
  /// Describes the most basic binding.
  /// </summary>
  public interface IBinding
  {
    /// <summary>
    /// This will be the name under which it will be available in the Frontend, e.g. 
    /// window.superBinding, or window.mapperBinding. Please use camelCase even if
    /// it hurts. 
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Bindings will be wrapped by a browser specific bridge, and they will need 
    /// to use that bridge to send events to the Frontend, via <see cref="IBridge.SendToBrowser(IHostAppEvent)">SendToBrowser(IHostAppEvent)</see> or <see cref="IBridge.SendToBrowser(string)">SendToBrowser(string)</see>.
    /// </summary>
    public IBridge Parent { get; set; }

    public void test()
    {
      Parent.SendToBrowser("wow");
    }
  }

  public interface IBridge
  {
    /// <summary>
    /// Sends to the Frontend a IHostAppEvent. If you want to send an empty event (no data), try the SendToBrowser(string eventName).
    /// </summary>
    /// <param name="eventData"></param>
    public void SendToBrowser(IHostAppEvent eventData);

    /// <summary>
    /// Sends to the Frontend a simple notification to do something. 
    /// </summary>
    /// <param name="eventName"></param>
    public void SendToBrowser(string eventName);
  }

  public interface IHostAppEvent
  {
    public string EventName { get; set; }

    /// <summary>
    /// Note: Data needs to be serializable.
    /// </summary>
    public object Data { get; set; }
  }

  public interface IBasicAppBinding : IBinding
  {
    public string GetSourceApplicationName() => "Mock App";

    public string GetSourceApplicationVersion() => "42";

  }
}
