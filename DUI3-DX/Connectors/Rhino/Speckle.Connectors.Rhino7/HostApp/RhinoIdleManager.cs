using System;
using System.Collections.Concurrent;
using Rhino;

namespace Speckle.Connectors.Rhino7.HostApp;

/// <summary>
/// Rhino Idle Manager is a helper util to manage deferred actions.
/// </summary>
public class RhinoIdleManager
{
  // NOTE: ConcurrentDictionary possibly removing the collection has been modified errors in here
  private readonly ConcurrentDictionary<string, Action> _sCalls = new();
  private bool _hasSubscribed;

  /// <summary>
  /// Subscribe deferred action to RhinoIdle event to run it whenever Rhino become idle.
  /// </summary>
  /// <param name="action"> Action to call whenever Rhino become Idle.</param>
  public void SubscribeToIdle(Action action)
  {
    _sCalls[action.Method.Name] = action;

    if (_hasSubscribed)
    {
      return;
    }

    _hasSubscribed = true;
    RhinoApp.Idle += RhinoAppOnIdle;
  }

  private void RhinoAppOnIdle(object sender, EventArgs e)
  {
    foreach (var kvp in _sCalls)
    {
      kvp.Value();
    }

    _sCalls.Clear();

    _hasSubscribed = false;
    RhinoApp.Idle -= RhinoAppOnIdle;
  }
}
