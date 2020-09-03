using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Speckle.ConnectorRevit
{
  /// <summary>
  /// Event invoker. Has a queue of actions that, in theory, this thing should iterate through. 
  /// Required to run transactions form a non modal window.
  /// </summary>
  public class ExternalEventHandler : IExternalEventHandler
  {

    public bool Running = false;
    public List<Action> Queue { get; set; }

    public ExternalEventHandler(List<Action> queue)
    {
      Queue = queue;
    }

    public void Execute(UIApplication app)
    {
      Debug.WriteLine("Current queue length is: " + Queue.Count);
      if (Running) return; // queue will run itself through

      Running = true;

      Queue[0]();

      Queue.RemoveAt(0);
      Running = false;
    }

    public string GetName()
    {
      return "ConnectorRevit";
    }
  }
}


