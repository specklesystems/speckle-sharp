using System;
using System.Collections.Generic;
using System.Linq;
using Dynamo.Interfaces;
using Dynamo.ViewModels;
using ProtoCore.Mirror;
using Speckle.Core.Credentials;
using Speckle.Core.Helpers;
using Speckle.Core.Logging;

namespace Speckle.ConnectorDynamo.Extension;

public class SpeckleWatchHandler : IWatchHandler
{
  private readonly IWatchHandler baseHandler;
  private readonly IPreferences preferences;

  public SpeckleWatchHandler(IPreferences prefs)
  {
    baseHandler = new DefaultWatchHandler(prefs);
    preferences = prefs;
  }

  private WatchViewModel ProcessThing(
    Account account,
    List<string> preferredDictionaryOrdering,
    ProtoCore.RuntimeCore runtimeCore,
    string tag,
    bool showRawData,
    WatchHandlerCallback callback
  )
  {
    var node = new WatchViewModel(account.userInfo.email, tag, RequestSelectGeometry);

    node.Clicked += () =>
    {
      Open.Url(account.serverInfo.url);
    };

    node.Link = account.serverInfo.url;

    return node;
  }

  //If no dispatch target is found, then invoke base watch handler.
  private WatchViewModel ProcessThing(
    object obj,
    List<string> preferredDictionaryOrdering,
    ProtoCore.RuntimeCore runtimeCore,
    string tag,
    bool showRawData,
    WatchHandlerCallback callback
  )
  {
    return baseHandler.Process(obj, preferredDictionaryOrdering, runtimeCore, tag, showRawData, callback);
  }

  private WatchViewModel ProcessThing(
    MirrorData data,
    List<string> preferredDictionaryOrdering,
    ProtoCore.RuntimeCore runtimeCore,
    string tag,
    bool showRawData,
    WatchHandlerCallback callback
  )
  {
    try
    {
      return baseHandler.Process(data, preferredDictionaryOrdering, runtimeCore, tag, showRawData, callback);
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      return callback(data.Data, preferredDictionaryOrdering, runtimeCore, tag, showRawData);
    }
  }

  public WatchViewModel Process(
    dynamic value,
    IEnumerable<string> preferredDictionaryOrdering,
    ProtoCore.RuntimeCore runtimeCore,
    string tag,
    bool showRawData,
    WatchHandlerCallback callback
  )
  {
    return Object.ReferenceEquals(value, null)
      ? new WatchViewModel("null", tag, RequestSelectGeometry)
      : ProcessThing(value, preferredDictionaryOrdering?.ToList(), runtimeCore, tag, showRawData, callback);
  }

  public event Action<string> RequestSelectGeometry;
}
