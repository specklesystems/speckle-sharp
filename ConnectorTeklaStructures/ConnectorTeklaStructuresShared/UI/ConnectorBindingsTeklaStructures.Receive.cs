using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Serilog.Context;
using Speckle.ConnectorTeklaStructures.Util;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace Speckle.ConnectorTeklaStructures.UI;

public partial class ConnectorBindingsTeklaStructures : ConnectorBindings
{
  #region receiving
  public override bool CanPreviewReceive => false;

  public override async Task<StreamState> PreviewReceive(StreamState state, ProgressViewModel progress)
  {
    return null;
  }

  public override async Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
  {
    Exceptions.Clear();

    var kit = KitManager.GetDefaultKit();
    var converter = kit.LoadConverter(ConnectorTeklaStructuresUtils.TeklaStructuresAppName);
    converter.SetContextDocument(Model);
    //var previouslyRecieveObjects = state.ReceivedObjects;

    var settings = new Dictionary<string, string>();
    CurrentSettings = state.Settings;
    foreach (var setting in state.Settings)
    {
      settings.Add(setting.Slug, setting.Selection);
    }

    converter.SetConverterSettings(settings);

    Exceptions.Clear();

    Commit myCommit = await ConnectorHelpers.GetCommitFromState(state, progress.CancellationToken);
    state.LastCommit = myCommit;
    Base commitObject = await ConnectorHelpers.ReceiveCommit(myCommit, state, progress);
    await ConnectorHelpers.TryCommitReceived(
      state,
      myCommit,
      ConnectorTeklaStructuresUtils.TeklaStructuresAppName,
      progress.CancellationToken
    );

    var conversionProgressDict = new ConcurrentDictionary<string, int>();
    conversionProgressDict["Conversion"] = 1;
    //Execute.PostToUIThread(() => state.Progress.Maximum = state.SelectedObjectIds.Count());

    Action updateProgressAction = () =>
    {
      conversionProgressDict["Conversion"]++;
      progress.Update(conversionProgressDict);
    };

    using var d0 = LogContext.PushProperty("converterName", converter.Name);
    using var d1 = LogContext.PushProperty("converterAuthor", converter.Author);
    using var d2 = LogContext.PushProperty("conversionDirection", nameof(ISpeckleConverter.ConvertToNative));
    using var d3 = LogContext.PushProperty("conversionSettings", settings);
    using var d4 = LogContext.PushProperty("converterReceiveMode", converter.ReceiveMode);

    foreach (var commitObj in FlattenCommitObject(commitObject, converter))
    {
      BakeObject(commitObj, converter);
      updateProgressAction?.Invoke();
    }

    Model.CommitChanges();
    progress.Report.Merge(converter.Report);
    return state;
  }

  /// <summary>
  /// conversion to native
  /// </summary>
  /// <param name="obj"></param>
  /// <param name="state"></param>
  /// <param name="converter"></param>
  [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
  private void BakeObject(Base obj, ISpeckleConverter converter)
  {
    LogContext.PushProperty("fromType", obj.GetType());
    try
    {
      converter.ConvertToNative(obj);
    }
    catch (Exception ex)
    {
      var exception = new ConversionException(
        $"Failed to convert object {obj.id} of type {obj.speckle_type} with error\n{ex}"
      );
      converter.Report.LogOperationError(exception);
      ConnectorHelpers.LogConversionException(ex);
    }
  }

  /// <summary>
  /// Traverses the object graph, returning objects to be converted.
  /// </summary>
  /// <param name="obj">The root <see cref="Base"/> object to traverse</param>
  /// <param name="converter">The converter instance, used to define what objects are convertable</param>
  /// <returns>A flattened list of objects to be converted ToNative</returns>
  private static IEnumerable<Base> FlattenCommitObject(Base obj, ISpeckleConverter converter)
  {
    var traverseFunction = DefaultTraversal.CreateTraverseFunc(converter);

    return traverseFunction
      .Traverse(obj)
      .Select(tc => tc.current)
      .Where(b => b != null)
      .Where(converter.CanConvertToNative)
      .Reverse();
  }

  #endregion
}
