using Microsoft.Xaml.Behaviors;
using System;
using System.Windows.Input;

namespace ConnectorGSA.Utilities
{
  public class EnterKeyDownEventTrigger : EventTrigger
  {

    public EnterKeyDownEventTrigger() : base("KeyDown")
    {
    }

    protected override void OnEvent(EventArgs eventArgs)
    {
      var e = eventArgs as KeyEventArgs;
      if (e != null && e.Key == Key.Enter)
      {
        this.InvokeActions(eventArgs);
      }
    }
  }
}
