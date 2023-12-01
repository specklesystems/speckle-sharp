using System;
using System.Collections.Generic;
using Autodesk.Revit.UI.Events;

namespace Speckle.ConnectorRevitDUI3.Utils;

public static class RevitIdleManager
{
  private static Dictionary<string, Action> s_calls = new();
  private static bool s_hasSubscribed;

  /// <summary>
  /// Subscribe deferred action to Idling event to run it whenever Revit becomes idle.
  /// </summary>
  /// <param name="action"> Action to call whenever Revit becomes Idle.</param>
  public static void SubscribeToIdle(Action action)
  {
    s_calls[action.Method.Name ?? Guid.NewGuid().ToString()] = action;

    if (s_hasSubscribed)
    {
      return;
    }

    s_hasSubscribed = true;
    RevitAppProvider.RevitApp.Idling += RevitAppOnIdle;
  }

  private static void RevitAppOnIdle(object sender, IdlingEventArgs e)
  {
    foreach (KeyValuePair<string, Action> kvp in s_calls)
    {
      kvp.Value();
    }
    s_calls = new Dictionary<string, Action>();
    s_hasSubscribed = false;
    RevitAppProvider.RevitApp.Idling -= RevitAppOnIdle;
  }
}
