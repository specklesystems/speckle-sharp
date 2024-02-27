using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;

namespace Speckle.Connectors.Revit.Plugin;

// POC: needs interface
// is probably misnamed, perhaps OnIdleCallbackManager
internal class RevitIdleManager : IRevitIdleManager
{
  private readonly UIApplication _uiApplication;

  private readonly ConcurrentDictionary<object, Action> _calls = new();

  // POC: still not thread safe
  private volatile bool _hasSubscribed = false;

  public RevitIdleManager(RevitContext revitContext)
  {
    _uiApplication = revitContext.UIApplication;
  }

  /// <summary>
  /// Subscribe deferred action to Idling event to run it whenever Revit becomes idle.
  /// </summary>
  /// <param name="action"> Action to call whenever Revit becomes Idle.</param>
  /// some events in host app are trigerred many times, we might get 10x per object
  /// Making this more like a deferred action, so we don't update the UI many times
  public void SubscribeToIdle(Action action)
  {
    // POC: key for method is brittle | thread safe is not this is
    // I want to be called back ONCE when the host app has become idle once more
    // would this work "action.Method.Name" with anonymous function, including the SAME function
    // does this work across class instances? Should it? What about functions of the same name? Fully qualified name might be better
    _calls[action] = action;

    if (_hasSubscribed)
    {
      return;
    }

    _hasSubscribed = true;
    _uiApplication.Idling += RevitAppOnIdle;
  }

  private void RevitAppOnIdle(object sender, IdlingEventArgs e)
  {
    foreach (KeyValuePair<object, Action> kvp in _calls)
    {
      kvp.Value();
    }

    _calls.Clear();
    _uiApplication.Idling -= RevitAppOnIdle;

    // setting last will delay ntering re-subscritption
    _hasSubscribed = false;
  }
}
