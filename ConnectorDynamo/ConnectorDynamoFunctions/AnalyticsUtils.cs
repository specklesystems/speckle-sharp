using System.Collections.Generic;
using Autodesk.DesignScript.Runtime;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using SpeckleAccount = Speckle.Core.Credentials.Account;

namespace Speckle.ConnectorDynamo.Functions;

[IsVisibleInDynamoLibrary(false)]
public static class AnalyticsUtils
{
  private static readonly string s_hostApp = HostApplications.Dynamo.Slug;
  private static readonly string s_hostAppVersion = HostApplications.Dynamo.GetVersion(HostAppVersion.vRevit);

  public static void TrackNodeRun(string name)
  {
    TrackEvent(Analytics.Events.NodeRun, new Dictionary<string, object>() { { "name", name }, });
  }

  public static void TrackNodeRun(SpeckleAccount account, string name)
  {
    TrackEvent(account, Analytics.Events.NodeRun, new Dictionary<string, object>() { { "name", name }, });
  }

  public static void TrackEvent(
    SpeckleAccount account,
    Analytics.Events eventName,
    Dictionary<string, object> customProperties = null
  )
  {
    AppendHostAppInfoToProperties(customProperties);
    Analytics.TrackEvent(account, eventName, customProperties);
  }

  public static void TrackEvent(Analytics.Events eventName, Dictionary<string, object> customProperties)
  {
    AppendHostAppInfoToProperties(customProperties);
    Analytics.TrackEvent(eventName, customProperties);
  }

  private static void AppendHostAppInfoToProperties(IDictionary<string, object> properties)
  {
    properties["hostAppVersion"] = s_hostAppVersion;
    properties["hostApp"] = s_hostApp;
  }
}
