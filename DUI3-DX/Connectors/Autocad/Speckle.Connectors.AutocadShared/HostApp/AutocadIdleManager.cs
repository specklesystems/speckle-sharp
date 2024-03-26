using System.Collections.Concurrent;

namespace Speckle.Connectors.Autocad.HostApp;

public class AutocadIdleManager
{
  private readonly ConcurrentDictionary<string, Action> _sCalls = new();
  private bool _hasSubscribed;

  /// <summary>
  /// Subscribe deferred action to AutocadIdle event to run it whenever Autocad become idle.
  /// </summary>
  /// <param name="action"> Action to call whenever Autocad become Idle.</param>
  public void SubscribeToIdle(Action action)
  {
    _sCalls[action.Method.Name] = action;

    if (_hasSubscribed)
    {
      return;
    }

    _hasSubscribed = true;
    Application.Idle += OnIdleHandler;
  }

  private void OnIdleHandler(object sender, EventArgs e)
  {
    foreach (var kvp in _sCalls)
    {
      kvp.Value();
    }

    _sCalls.Clear();
    _hasSubscribed = false;
    Application.Idle -= OnIdleHandler;
  }
}
