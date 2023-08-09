using System;

namespace Speckle.Core.Helpers;

public class State<T> : IDisposable where T : State<T>, new()
{
  static T root;
  static T current;
  T previous = current;

  private static readonly object StateChangeLock = new();

  protected State()
  {
    lock (StateChangeLock)
    {
      current = (T)this;
      if (root == null)
        root = (T)this;
    }
  }

  void IDisposable.Dispose()
  {
    lock (StateChangeLock)
    {
      if (previous == null)
        return; // Already disposed or root

      if (current == this) current = previous;
      else
      {
        // If this state is still in the stack is safe to pop it
        var state = this;
        do
        {
          if (state == root) { current = previous; break; }

          state = state.previous;
        } while (state != null);
      }

      previous = null;
    }
  }

  public static T Peek => current;

  public static T Push()
  {
    lock (StateChangeLock)
    {
      var peek = current ?? new T();
      var top = (T)peek.MemberwiseClone();
      top.previous = current;

      return current = top;
    }
  }

  public static void Pop()
  {
    lock (StateChangeLock)
    {
      ((IDisposable)current).Dispose();
    }
  }

  protected void Pull()
  {
    lock (StateChangeLock)
    {
      ((IDisposable)this).Dispose();
    }
  }
}
