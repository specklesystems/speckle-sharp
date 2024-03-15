using System;

namespace Speckle.Connectors.Revit.Plugin;

// POC: needs interface
// is probably misnamed, perhaps OnIdleCallbackManager
internal interface IRevitIdleManager
{
  /// <summary>
  /// Subscribe deferred action to Idling event to run it whenever Revit becomes idle.
  /// </summary>
  /// <param name="action"> Action to call whenever Revit becomes Idle.</param>
  /// some events in host app are trigerred many times, we might get 10x per object
  /// Making this more like a deferred action, so we don't update the UI many times
  void SubscribeToIdle(Action action);
}
