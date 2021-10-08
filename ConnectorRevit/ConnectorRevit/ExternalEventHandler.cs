using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Speckle.ConnectorRevit.UI;
using Speckle.Core.Logging;
using Speckle.DesktopUI;

namespace Speckle.ConnectorRevit
{
  /// <summary>
  /// Event invoker. Has a queue of actions that, in theory, this thing should iterate through. 
  /// Required to run transactions form a non modal window.
  /// </summary>
  public class SpeckleExternalEventHandler : IExternalEventHandler
  {
    public ConnectorBindingsRevit RevitBindings { get; set; }
    public bool Running = false;

    public SpeckleExternalEventHandler(ConnectorBindingsRevit revitBindings)
    {
      RevitBindings = revitBindings;
    }

    public void Execute(UIApplication app)
    {
      Debug.WriteLine("Current queue length is: " + RevitBindings.Queue.Count);
      if (Running) return; // queue will run itself through

      Running = true;

      try
      {
        RevitBindings.Queue[0]();
      }
      catch (Exception e)
      {
        Log.CaptureException(e);
      }

      RevitBindings.Queue.RemoveAt(0);
      Running = false;

      if (RevitBindings.Queue.Count != 0)
        RevitBindings.Executor.Raise();

    }

    public string GetName()
    {
      return "ConnectorRevit";
    }
  }
}
