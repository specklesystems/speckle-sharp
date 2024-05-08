using Speckle.Connectors.DUI.Bindings;

namespace Speckle.Connectors.DUI.Bridge;

/// <summary>
/// Describes a bridge - a wrapper class around a specific browser host. Not needed right now,
/// but if in the future we will have other bridge classes (e.g, ones that wrap around other browsers),
/// it just might be useful.
/// </summary>
public interface IBridge
{
  // POC: documnetation comments
  string? FrontendBoundName { get; }

  void AssociateWithBinding(IBinding binding, Action<string> scriptMethod, object browser, Action showDevToolsAction);

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

  public void Send(string eventName);

  public void Send<T>(string eventName, T data)
    where T : class;
}
