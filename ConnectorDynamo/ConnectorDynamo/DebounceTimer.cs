using System;
using System.Timers;

namespace Speckle.ConnectorDynamo;

public class DebounceTimer
{
  private Timer timer;

  /// <summary>
  /// Adapted from: https://weblog.west-wind.com/posts/2017/jul/02/debouncing-and-throttling-dispatcher-events
  /// Debounce an event by resetting the event timeout every time the event is
  /// fired. The behavior is that the Action passed is fired only after events
  /// stop firing for the given timeout period.
  ///
  /// Use Debounce when you want events to fire only after events stop firing
  /// after the given interval timeout period.
  ///
  /// Wrap the logic you would normally use in your event code into
  /// the  Action you pass to this method to debounce the event.
  /// Example: https://gist.github.com/RickStrahl/0519b678f3294e27891f4d4f0608519a
  /// </summary>
  /// <param name="interval">Timeout in Milliseconds</param>
  /// <param name="action">Action<object> to fire when debounced event fires</object></param>
  public void Debounce(int interval, Action action)
  {
    // kill pending timer and pending ticks
    timer?.Stop();
    timer = null;

    // timer is recreated for each event and effectively
    // resets the timeout. Action only fires after timeout has fully
    // elapsed without other events firing in between
    timer = new Timer(interval);
    timer.Elapsed += delegate
    {
      if (timer == null)
      {
        return;
      }

      timer?.Stop();
      timer = null;
      action.Invoke();
    };

    timer.Start();
  }
}
