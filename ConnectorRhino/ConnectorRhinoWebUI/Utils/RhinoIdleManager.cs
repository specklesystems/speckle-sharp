using System;
using System.Collections.Concurrent;
using Rhino;

namespace ConnectorRhinoWebUI.Utils;

/// <summary>
/// Rhino Idle Manager is a helper util to manage deferred actions.
/// </summary>
public static class RhinoIdleManager
{
  // NOTE: ConcurrentDictionary possibly removing the collection has been modified errors in here
  private static readonly ConcurrentDictionary<string, Action> s_sCalls = new();
  private static bool s_hasSubscribed;

  /// <summary>
  /// Subscribe deferred action to RhinoIdle event to run it whenever Rhino become idle.
  /// </summary>
  /// <param name="action"> Action to call whenever Rhino become Idle.</param>
  public static void SubscribeToIdle(Action action)
  {
    s_sCalls[action.Method.Name] = action;

    if (s_hasSubscribed)
    {
      return;
    }

    s_hasSubscribed = true;
    RhinoApp.Idle += RhinoAppOnIdle;
  }

  private static void RhinoAppOnIdle(object sender, EventArgs e)
  {
    foreach (var kvp in s_sCalls)
    {
      kvp.Value();
    }

    s_sCalls.Clear();

    s_hasSubscribed = false;
    RhinoApp.Idle -= RhinoAppOnIdle;
  }
}
