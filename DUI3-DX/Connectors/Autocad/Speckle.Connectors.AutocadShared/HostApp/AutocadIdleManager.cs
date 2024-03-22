using System.Collections.Concurrent;
using Autodesk.AutoCAD.ApplicationServices.Core;

namespace Speckle.Connectors.Autocad.HostApp;

public class AutocadIdleManager
{
  private readonly ConcurrentDictionary<string, Action> s_sCalls = new();
  private bool s_hasSubscribed;

  /// <summary>
  /// Subscribe deferred action to AutocadIdle event to run it whenever Autocad become idle.
  /// </summary>
  /// <param name="action"> Action to call whenever Autocad become Idle.</param>
  public void SubscribeToIdle(Action action)
  {
    s_sCalls[action.Method.Name] = action;

    if (s_hasSubscribed)
    {
      return;
    }

    s_hasSubscribed = true;
    Application.Idle += OnIdleHandler;
  }

  private void OnIdleHandler(object sender, EventArgs e)
  {
    foreach (var kvp in s_sCalls)
    {
      kvp.Value();
    }

    s_sCalls.Clear();
    s_hasSubscribed = false;
    Application.Idle -= OnIdleHandler;
  }
}
