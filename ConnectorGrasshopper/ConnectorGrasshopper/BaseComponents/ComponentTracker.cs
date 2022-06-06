using System.Collections.Generic;
using Grasshopper.Kernel;
using Speckle.Core;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;

namespace ConnectorGrasshopper
{
  public class ComponentTracker
  {
    public GH_Component Parent;

    public ComponentTracker(GH_Component parent)
    {
      Parent = parent;
    }

    public void TrackEvent(Speckle.Core.Logging.Analytics.Events eventName, Dictionary<string, object> properties)
    {
      Speckle.Core.Logging.Analytics.TrackEvent(eventName, properties);
    }
    public void TrackNodeCreation(string? name = null)
    {
      Speckle.Core.Logging.Analytics.TrackEvent(
        Speckle.Core.Logging.Analytics.Events.NodeCreate, 
        new Dictionary<string, object>{ { "name", name ?? Parent?.Name ?? "unset" } });
    }

    public void TrackNodeRun(string? name = null, string? node = null)
    {
      var customProperties = new Dictionary<string, object>{ { "name", name ?? Parent?.Name ?? "unset" } };
      if(node != null) customProperties.Add("node", node);
      Speckle.Core.Logging.Analytics.TrackEvent(
        Speckle.Core.Logging.Analytics.Events.NodeRun, 
        customProperties);
    }

    public void TrackNodeSend(Account acc, bool auto, bool sync = false)
    {
      var customProperties = new Dictionary<string, object>();
      if(auto) customProperties.Add("auto", auto);
      if(sync) customProperties.Add("sync", sync);
      Speckle.Core.Logging.Analytics.TrackEvent(
        acc, 
        Speckle.Core.Logging.Analytics.Events.Send, 
        customProperties);
    }

    public void TrackNodeReceive(Account acc, bool auto)
    {
      Speckle.Core.Logging.Analytics.TrackEvent(
        acc, 
        Speckle.Core.Logging.Analytics.Events.Receive, 
        new Dictionary<string, object>()
        {
          { "auto", auto }
        });
    }
  }
  
  
}
