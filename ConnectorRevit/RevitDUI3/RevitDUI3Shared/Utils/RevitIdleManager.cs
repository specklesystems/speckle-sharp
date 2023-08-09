using System;
using System.Collections.Generic;
using Autodesk.Revit.UI.Events;

namespace Speckle.ConnectorRevitDUI3.Utils;

public static class RevitIdleManager
{
  private static Dictionary<string, Action> calls = new Dictionary<string, Action>();
  private static bool _hasSubscribed = false;

  /// <summary>
  /// Subscribe deferred action to Idling event to run it whenever Revit becomes idle.
  /// </summary>
  /// <param name="action"> Action to call whenever Revit becomes Idle.</param>
  public static void SubscribeToIdle(Action action)
  {
    calls[action.Method.Name ?? Guid.NewGuid().ToString()] = action;
    
    if (_hasSubscribed) return;
    _hasSubscribed = true;
    RevitAppProvider.RevitApp.Idling += RevitAppOnIdle;
  }

  private static void RevitAppOnIdle(object sender, IdlingEventArgs e)
  {
    foreach (var kvp in calls)
    {
      kvp.Value();
    }
    calls = new Dictionary<string, Action>();
    _hasSubscribed = false;
    RevitAppProvider.RevitApp.Idling -= RevitAppOnIdle;
  }
}
