using System;

namespace DUI3;

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
  /// TODO: we'll probably need a factory class of sorts to handle the proper wrapping. Currently, on bridge instantiation the parent is set in the bindings class that has been wrapped around. Not vvv elegant.
  /// </summary>
  public IBridge Parent { get; set; }
}

/// <summary>
/// Describes a bridge - a wrapper class around a specific browser host. Not needed right now,
/// but if in the future we will have other bridge classes (e.g, ones that wrap around other browsers),
/// it just might be useful.
/// </summary>
public interface IBridge
{
  /// <summary>
  /// This method is called by the Frontend bridge to understand what it can actually call. It should return the method names of the bindings that this bridge wraps around.
  /// </summary>
  /// <returns></returns>
  public string[] GetBindingsMethodNames();

  /// <summary>
  /// This method is called by the Frontend bridge when invoking any of the wrapped binding's methods.
  /// </summary>
  /// <param name="methodName"></param>
  /// <param name="args"></param>
  /// <returns></returns>
  public void RunMethod(string methodName, string requestId, string args);

  /// <summary>
  /// Run actions on main thread.
  /// Some applications might need to run some operations on main thread as deferred actions.
  /// </summary>
  /// <param name="action"> Action to run on main thread.</param>
  public void RunOnMainThread(Action action);

  /// <summary>
  /// Sends to the Frontend an event with an optional payload.
  /// </summary>
  /// <param name="eventData"></param>
  public void SendToBrowser(string eventName, object data = null);
}
