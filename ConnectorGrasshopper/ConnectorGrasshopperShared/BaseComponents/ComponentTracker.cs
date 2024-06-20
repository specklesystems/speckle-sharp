using System.Collections.Generic;
using Grasshopper.Kernel;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;

namespace ConnectorGrasshopper;

public class ComponentTracker
{
  public GH_Component Parent;

  public ComponentTracker(GH_Component parent)
  {
    Parent = parent;
  }

  private static void AppendHostAppInfoToProperties(Dictionary<string, object> properties)
  {
    properties["hostAppVersion"] = Loader.GetGrasshopperHostAppVersion();
    properties["hostApp"] = HostApplications.Grasshopper.Slug;
  }

  public void TrackEvent(Speckle.Core.Logging.Analytics.Events eventName, Dictionary<string, object> properties)
  {
    AppendHostAppInfoToProperties(properties);
    Speckle.Core.Logging.Analytics.TrackEvent(eventName, properties);
  }

  public void TrackNodeCreation(string? name = null)
  {
    var properties = new Dictionary<string, object> { { "name", name ?? Parent?.Name ?? "unset" } };
    AppendHostAppInfoToProperties(properties);
    Speckle.Core.Logging.Analytics.TrackEvent(Speckle.Core.Logging.Analytics.Events.NodeCreate, properties);
  }

  public void TrackNodeRun(string? name = null, string? node = null)
  {
    var customProperties = new Dictionary<string, object> { { "name", name ?? Parent?.Name ?? "unset" } };
    if (node != null)
    {
      customProperties.Add("node", node);
    }
    AppendHostAppInfoToProperties(customProperties);
    Speckle.Core.Logging.Analytics.TrackEvent(Speckle.Core.Logging.Analytics.Events.NodeRun, customProperties);
  }

  public void TrackNodeSend(Account acc, bool auto, bool sync = false)
  {
    var customProperties = new Dictionary<string, object>();
    if (auto)
    {
      customProperties.Add("auto", auto);
    }

    if (sync)
    {
      customProperties.Add("sync", sync);
    }

    AppendHostAppInfoToProperties(customProperties);
    Speckle.Core.Logging.Analytics.TrackEvent(acc, Speckle.Core.Logging.Analytics.Events.Send, customProperties);
  }

  public void TrackNodeReceive(Account acc, bool auto)
  {
    var properties = new Dictionary<string, object> { { "auto", auto } };
    AppendHostAppInfoToProperties(properties);
    Speckle.Core.Logging.Analytics.TrackEvent(acc, Speckle.Core.Logging.Analytics.Events.Receive, properties);
  }
}
