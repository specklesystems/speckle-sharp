using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Interop;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using static Autodesk.Navisworks.Api.Interop.LcOpRegistry;
using static Autodesk.Navisworks.Api.Interop.LcUOption;
using static Speckle.ConnectorNavisworks.Utils;


namespace Speckle.ConnectorNavisworks.Bindings
{
  public partial class ConnectorBindingsNavisworks
  {
    public override bool CanPreviewSend => false;


    // Stub - Preview send is not supported
    public override async void PreviewSend(StreamState state, ProgressViewModel progress)
    {
      await Task.Delay(TimeSpan.FromMilliseconds(500));
      // TODO!
    }

    public override async Task<string> SendStream(StreamState state, ProgressViewModel progress)
    {
      if (Doc.ActiveSheet == null) throw new InvalidOperationException("Your Document is empty. Nothing to Send.");

      var filteredObjects = new List<Tuple<string, int>>();

      var progressBar = Application.BeginProgress("Send to Speckle.");

      progress.CancellationToken.Register(() =>
      {
        progressBar.Cancel();
        //progressBar.Update(1);
        Application.EndProgress();
      });

      DefaultKit = KitManager.GetDefaultKit();

      NavisworksConverter = DefaultKit.LoadConverter(VersionedAppName);

      CurrentSettings = state.Settings;

      var settings =
        state.Settings.ToDictionary(setting => setting.Slug, setting => setting.Selection);
      NavisworksConverter.SetContextDocument(Doc);
      NavisworksConverter.SetConverterSettings(settings);

      NavisworksConverter.Report.ReportObjects.Clear();

      var streamId = state.StreamId;
      var client = state.Client;

      if (state.Filter != null)
      {
        progressBar.BeginSubOperation(0, $"Building object-tree from {state.Filter.Selection.Count} selections.");

        IEnumerable<Tuple<string, int>> nodes;

        try
        {
          nodes = GetObjectsFromFilter(state.Filter);
        }
        catch
        {
          throw new Exception("An error occurred retrieving objects from your saved selection source.");
        }

        if (nodes != null) filteredObjects.AddRange(nodes);

        state.SelectedObjectIds = filteredObjects.Select(x => x.Item1).ToList();
      }

      if (filteredObjects.Count == 0)
      {
        if (state.Filter != null && state.Filter.Slug == "all")
        {
          throw new InvalidOperationException("Everything Mode is not yet implemented. Send stopped.");
        }
        else
        {
          throw new InvalidOperationException(
            "Zero objects selected; send stopped. Please select some objects, or check that your filter can actually select something.");
        }
      }

      progress.Report = new ProgressReport();
      var conversionProgressDict = new ConcurrentDictionary<string, int>
      {
        ["Conversion"] = 0
      };

      var totalObjects = filteredObjects.Sum(x => x.Item2);

      progress.Max = totalObjects;

      var commitObject = new Base
      {
        ["units"] = GetUnits(Doc)
      };

      var toConvertDictionary =
        new SortedDictionary<string, ConversionState>(new PseudoIdComparer());
      state.SelectedObjectIds.ForEach(pseudoId =>
      {
        //if (pseudoId != RootNodePseudoId)
        {
          toConvertDictionary.Add(pseudoId, ConversionState.ToConvert);
        }
      });

      progressBar.EndSubOperation();
      progressBar.BeginSubOperation(1, $"Converting {filteredObjects.Count + totalObjects} Objects.");
      progressBar.Update(0);

      // We have to disable autosave because it explodes everything if it tries saving mid send process.
      bool autosaveSetting;

      using (var optionLock = new LcUOptionLock())
      {
        var rootOptions = GetRoot(optionLock);
        autosaveSetting = rootOptions.GetBoolean("general.autosave.enable");
        if (autosaveSetting)
        {
          rootOptions.SetBoolean("general.autosave.enable", false);
          SaveGlobalOptions();
        }
      }

      NavisworksConverter.SetConverterSettings(new Dictionary<string, string> { { "_Mode", "objects" } });


      while (toConvertDictionary.Any(kv => kv.Value == ConversionState.ToConvert))
      {
        double navisworksProgressState = Math.Min((float)progress.Value / progress.Max, 1);
        progressBar.Update(navisworksProgressState);

        if (progressBar.IsCanceled)
        {
          progress.CancellationTokenSource.Cancel();
        }

        progress.CancellationToken.ThrowIfCancellationRequested();

        var nextToConvert = toConvertDictionary.First(kv => kv.Value == ConversionState.ToConvert);

        var applicationId = string.Empty;
        var pseudoId = nextToConvert.Key;
        var descriptor = ObjectDescriptor(pseudoId);

        var alreadyConverted =
          NavisworksConverter.Report.ReportObjects.TryGetValue(pseudoId, out var applicationObject);

        var reportObject = alreadyConverted
          ? applicationObject
          : new ApplicationObject(pseudoId, descriptor)
          {
            applicationId = pseudoId
          };

        if (alreadyConverted)
        {
          progress.Report.Log(reportObject);
          toConvertDictionary[pseudoId] = ConversionState.Converted;
          continue;
        }

        if (!NavisworksConverter.CanConvertToSpeckle(pseudoId))
        {
          reportObject.Update(status: ApplicationObject.State.Skipped,
            logItem: "Sending this object type is not supported in Navisworks");
          progress.Report.Log(reportObject);

          toConvertDictionary[pseudoId] = ConversionState.Skipped;
          continue;
        }

        NavisworksConverter.Report.Log(reportObject);

        var converted = Convert(pseudoId);

        if (converted == null)
        {
          reportObject.Update(status: ApplicationObject.State.Failed, logItem: "Conversion returned null");
          progress.Report.Log(reportObject);
          toConvertDictionary[pseudoId] = ConversionState.Failed;
          continue;
        }

        if (commitObject["@elements"] == null) commitObject["@elements"] = new List<Base>();

        ((List<Base>)commitObject["@elements"]).Add(converted);

        toConvertDictionary[pseudoId] = ConversionState.Converted;

        conversionProgressDict["Conversion"] += filteredObjects.Where(x => x.Item1 == pseudoId).Sum(x => x.Item2);

        progress.Update(conversionProgressDict);

        //converted.applicationId = applicationId;
        if (converted["@SpeckleSchema"] is Base newSchemaBase)
        {
          newSchemaBase.applicationId = applicationId;
          converted["@SpeckleSchema"] = newSchemaBase;
        }

        reportObject.Update(status: ApplicationObject.State.Created,
          logItem: $"Sent as {converted.speckle_type}");
        progress.Report.Log(reportObject);
      }

      var convertedCount = toConvertDictionary.Count(x => x.Value == ConversionState.Converted);

      progressBar.Update(1.0);

      // Better set the users autosave back on or they might get cross
      if (autosaveSetting)
        using (var optionLock = new LcUOptionLock())
        {
          var rootOptions = GetRoot(optionLock);
          rootOptions.SetBoolean("general.autosave.enable", true);
          SaveGlobalOptions();
        }

      progressBar.EndSubOperation();

      progress.Report.Merge(NavisworksConverter.Report);

      if (convertedCount == 0)
      {
        throw new SpeckleException("Zero objects converted successfully. Send stopped.");
      }

      #region Views

      var views = new List<Base>();

      NavisworksConverter.SetConverterSettings(new Dictionary<string, string> { { "_Mode", "views" } });
      if (state.Filter?.Slug == "views")
      {
        var selectedViews = state.Filter.Selection.Select(Convert).Where(c => c != null).ToList();
        views.AddRange(selectedViews);
      }

      if (CurrentSettings.Find(x => x.Slug == "current-view") is CheckBoxSetting checkBox && checkBox.IsChecked)
      {
        views.Add(Convert(Doc.CurrentViewpoint.ToViewpoint()));
      }

      if (views.Any()) commitObject["Views"] = views;

      #endregion

      var totalConversions = convertedCount + conversionProgressDict["Conversion"];

      progressBar.BeginSubOperation(0,
        $"Sending {convertedCount} objects and {conversionProgressDict["Conversion"]} children to Speckle.");

      NavisworksConverter.SetConverterSettings(new Dictionary<string, string> { { "_Mode", null } });

      progress.CancellationToken.ThrowIfCancellationRequested();

      progress.Max = totalConversions;

      var transports = new List<ITransport>
      {
        new ServerTransport(client.Account, streamId)
      };

      var objectId = await Operations.Send(
        commitObject,
        progress.CancellationToken,
        transports,
        onProgressAction: dict => {
          if (dict.TryGetValue("RemoteTransport", out var rc) && rc > 0) {
            var p = (double)rc / 2 / totalConversions;
            if (p <= 1) progressBar.Update(p);
          }
          progress.Update(dict);
        },

        // Deviation from the ideal pattern, which should throw to the DefaultErrorHandler
        onErrorAction: (err, ex) => { progress.Report.OperationErrors.Add(ex); },
        disposeTransports: true
      );

      if (progress.Report.OperationErrors.Any())
      {
        ConnectorHelpers.DefaultSendErrorHandler("", progress.Report.OperationErrors.Last());
      }

      progress.CancellationToken.ThrowIfCancellationRequested();

      var commit = new CommitCreateInput
      {
        streamId = streamId,
        objectId = objectId,
        branchName = state.BranchName,
        message = state.CommitMessage ?? $"Sent {totalConversions} elements from {HostApplications.Navisworks.Name}.",
        sourceApplication = HostApplications.Navisworks.Slug
      };

      var commitId = await ConnectorHelpers.CreateCommit(progress.CancellationToken, client, commit);
      return commitId;
    }

    private Base Convert(object inputObject)
    {
      try
      {
        Func<object, Base> convertToSpeckle = NavisworksConverter.ConvertToSpeckle;
        return (Base)InvokeOnUIThreadWithException(Control, convertToSpeckle, inputObject);
      }
      catch (TargetInvocationException ex)
      {
        // log the exception or re-throw it
        throw ex.InnerException ?? ex;
      }
    }

    private static object InvokeOnUIThreadWithException(System.Windows.Forms.Control control, Delegate method,
      params object[] args)
    {
      if (control == null) return null;

      object result = null;

      control.Invoke(new Action(() =>
      {
        try
        {
          result = method?.DynamicInvoke(args);
        }
        catch (TargetInvocationException ex)
        {
          // log the exception or re-throw it
          throw ex.InnerException ?? ex;
        }
      }));

      return result;
    }

    private enum ConversionState
    {
      Converted = 0,
      Skipped = 1,
      ToConvert = 2,
      Failed = 3
    }
  }
}