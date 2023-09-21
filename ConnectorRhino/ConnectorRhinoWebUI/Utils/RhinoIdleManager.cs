using System;
using System.Collections.Generic;
using Rhino;

namespace ConnectorRhinoWebUI.Utils;

/// <summary>
/// Rhino Idle Manager is a helper util to manage deferred actions.
/// </summary>
public static class RhinoIdleManager
{
  private static Dictionary<string, Action> calls = new Dictionary<string, Action>();
  private static bool _hasSubscribed = false;

  /// <summary>
  /// Subscribe deferred action to RhinoIdle event to run it whenever Rhino become idle.
  /// </summary>
  /// <param name="action"> Action to call whenever Rhino become Idle.</param>
  public static void SubscribeToIdle(Action action)
  {
    calls[action.Method.Name] = action;
    
    if (_hasSubscribed) return;
    _hasSubscribed = true;
    RhinoApp.Idle += RhinoAppOnIdle;
  }

  private static void RhinoAppOnIdle(object sender, EventArgs e)
  {
    // NOTE: got a random collection was modified while iterating error. 
    // we should probably ensure we don't subscribe to idle while this func does work
    foreach (var kvp in calls)
    {
      kvp.Value();
    }
    calls = new Dictionary<string, Action>();
    _hasSubscribed = false;
    RhinoApp.Idle -= RhinoAppOnIdle;
  }
}

