using System;
using System.Collections.Generic;
using Rhino;

namespace ConnectorRhinoWebUI.Utils;

/// <summary>
/// Rhino Idle Manager is a helper util to manage deferred actions.
/// </summary>
public static class RhinoIdleManager
{
  private static Dictionary<string, Action> s_calls = new();
  private static bool s_hasSubscribed;

  /// <summary>
  /// Subscribe deferred action to RhinoIdle event to run it whenever Rhino become idle.
  /// </summary>
  /// <param name="action"> Action to call whenever Rhino become Idle.</param>
  public static void SubscribeToIdle(Action action)
  {
    s_calls[action.Method.Name] = action;

    if (s_hasSubscribed)
    {
      return;
    }

    s_hasSubscribed = true;
    RhinoApp.Idle += RhinoAppOnIdle;
  }

  private static void RhinoAppOnIdle(object sender, EventArgs e)
  {
    // NOTE: got a random collection was modified while iterating error.
    // we should probably ensure we don't subscribe to idle while this func does work
    foreach (KeyValuePair<string, Action> kvp in s_calls)
    {
      kvp.Value();
    }
    s_calls = new Dictionary<string, Action>();
    s_hasSubscribed = false;
    RhinoApp.Idle -= RhinoAppOnIdle;
  }
}
