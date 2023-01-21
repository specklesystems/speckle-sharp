using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Interop;
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

    // used to store the Stream State settings when sending
    private List<ISetting> CurrentSettings { get; set; }


    // Stub - Preview send is not supported
    public override async void PreviewSend(StreamState state, ProgressViewModel progress)
    {
      await Task.Delay(TimeSpan.FromMilliseconds(500));
      // TODO!
    }

    private enum ConversionState
    {
      Converted = 0,
      Skipped = 1,
      ToConvert = 2
    }

    public override async Task<string> SendStream(StreamState state, ProgressViewModel progress)
    {
      List<string> filteredObjects = new List<string>();
      Progress progressBar = Application.BeginProgress("Send to Speckle.");

      DefaultKit = KitManager.GetDefaultKit();

      if (DefaultKit == null)
      {
        progress.Report.LogOperationError(new SpeckleException("Could not find any Kit!"));
        return null;
      }

      NavisworksConverter = DefaultKit.LoadConverter(VersionedAppName);
      if (NavisworksConverter == null)
      {
        progress.Report.LogOperationError(new SpeckleException($"Could not find Converter{VersionedAppName}!"));
        return null;
      }

      NavisworksConverter.SetContextDocument(Doc);

      NavisworksConverter.Report.ReportObjects.Clear();

      CurrentSettings = state.Settings;

      Dictionary<string, string> settings =
        state.Settings.ToDictionary(setting => setting.Slug, setting => setting.Selection);

      NavisworksConverter.SetConverterSettings(settings);

      string streamId = state.StreamId;
      Client client = state.Client;


      if (state.Filter != null)
      {
        progressBar.BeginSubOperation(0, $"Building object-tree from {state.Filter.Selection.Count} selections.");
        IEnumerable<string> objects = GetObjectsFromFilter(state.Filter);

        if (objects != null)
        {
          filteredObjects.AddRange(objects);
        }

        state.SelectedObjectIds = filteredObjects.ToList();
      }

      if (filteredObjects.Count == 0)
      {
        progress.Report.LogOperationError(new SpeckleException(
          "Zero objects selected; send stopped. Please select some objects, or check that your filter can actually select something."));
        progressBar.Cancel();
        Application.EndProgress();
        return null;
      }

      progress.Report = new ProgressReport();
      ConcurrentDictionary<string, int> conversionProgressDict = new ConcurrentDictionary<string, int>
      {
        ["Conversion"] = 0
      };

      progress.Max = state.SelectedObjectIds.Count;

      Base commitObject = new Base
      {
        ["units"] = GetUnits(Doc)
      };

      int convertedCount = 0;

      SortedDictionary<string, ConversionState> toConvertDictionary =
        new SortedDictionary<string, ConversionState>(new PseudoIdComparer());
      state.SelectedObjectIds.ForEach(pseudoId =>
      {
        if (pseudoId != RootNodePseudoId) toConvertDictionary.Add(pseudoId, ConversionState.ToConvert);
      });

      progressBar.EndSubOperation();
      progressBar.BeginSubOperation(1, $"Converting {state.SelectedObjectIds.Count} Objects.");
      progressBar.Update(0);

      // We have to disable autosave because it explodes everything if it tries saving mid send process.
      bool autosaveSetting;

      using (LcUOptionLock optionLock = new LcUOptionLock())
      {
        LcUOptionSet rootOptions = GetRoot(optionLock);
        autosaveSetting = rootOptions.GetBoolean("general.autosave.enable");
        if (autosaveSetting)
        {
          rootOptions.SetBoolean("general.autosave.enable", false);
          SaveGlobalOptions();
        }
      }

      while (toConvertDictionary.Any(kv => kv.Value == ConversionState.ToConvert))
      {
        double navisworksProgressState = Math.Min((float)progress.Value / progress.Max, 1);
        progressBar.Update(navisworksProgressState);

        if (progressBar.IsCanceled)
        {
          progress.CancellationTokenSource.Cancel();
          progressBar.Cancel();
          Application.EndProgress();
          return null;
        }

        if (progress.CancellationTokenSource.Token.IsCancellationRequested)
        {
          progressBar.Cancel();
          Application.EndProgress();
          return null;
        }

        var nextToConvert = toConvertDictionary.First(kv => kv.Value == ConversionState.ToConvert);

        Base converted = null;

        string applicationId = string.Empty;


        var pseudoId = nextToConvert.Key;
        var descriptor = ObjectDescriptor(pseudoId);

        bool alreadyConverted = NavisworksConverter.Report.GetReportObject(pseudoId, out int index);

        ApplicationObject reportObject = alreadyConverted
          ? NavisworksConverter.Report.ReportObjects[index]
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
            logItem: $"Sending this object type is not supported in Navisworks");
          progress.Report.Log(reportObject);

          toConvertDictionary[pseudoId] = ConversionState.Converted;
          continue;
        }

        NavisworksConverter.Report.Log(reportObject);

        // All Conversions should be on the main thread
        if (Control.InvokeRequired)
        {
          Control.Invoke(new Action(() => converted = NavisworksConverter.ConvertToSpeckle(pseudoId)));
        }
        else
        {
          converted = NavisworksConverter.ConvertToSpeckle(pseudoId);
        }

        if (converted == null)
        {
          reportObject.Update(status: ApplicationObject.State.Failed,
            logItem: $"Conversion returned null");
          progress.Report.Log(reportObject);
          toConvertDictionary[pseudoId] = ConversionState.Skipped;
          continue;
        }

        if (commitObject[$"@Elements"] == null)
        {
          commitObject[$"@Elements"] = new List<Base>();
        }

        ((List<Base>)commitObject[$"@Elements"]).Add(converted);

        // read back the pseudoIds of nested children already converted
        if (!(converted["__convertedIds"] is List<string> convertedChildrenAndSelf))
        {
          continue;
        }

        convertedChildrenAndSelf.ForEach(x => toConvertDictionary[x] = ConversionState.Converted);
        conversionProgressDict["Conversion"] += convertedChildrenAndSelf.Count;

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

        convertedCount += convertedChildrenAndSelf.Count;
      }

      progressBar.Update(1.0);

      // Better set the users autosave back on or they might get cross
      if (autosaveSetting)
      {
        using (LcUOptionLock optionLock = new LcUOptionLock())
        {
          LcUOptionSet rootOptions = GetRoot(optionLock);
          rootOptions.SetBoolean("general.autosave.enable", true);
          SaveGlobalOptions();
        }
      }

      progressBar.EndSubOperation();
      progressBar.BeginSubOperation(1, $"Sending {convertedCount} objects to Speckle.");

      progress.Report.Merge(NavisworksConverter.Report);

      if (convertedCount == 0)
      {
        progress.Report.LogOperationError(
          new SpeckleException("Zero objects converted successfully. Send stopped.", false));
        return null;
      }

      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
      {
        return null;
      }

      progress.Max = convertedCount;

      List<ITransport> transports = new List<ITransport>()
      {
        new ServerTransport(client.Account, streamId)
      };

      void ErrorAction(string s, Exception e)
      {
        progress.Report.LogOperationError(e);
        progress.CancellationTokenSource.Cancel();
      }

      string objectId = await Operations.Send(
        @object: commitObject,
        cancellationToken: progress.CancellationTokenSource.Token,
        transports: transports,
        onProgressAction: progress.Update,
        onErrorAction: ErrorAction,
        disposeTransports: true
      );

      if (progress.Report.OperationErrorsCount != 0)
      {
        Application.EndProgress();
        progressBar.Dispose();
        return null;
      }

      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
      {
        Application.EndProgress();
        progressBar.Dispose();
        return null;
      }

      CommitCreateInput commit = new CommitCreateInput
      {
        streamId = streamId,
        objectId = objectId,
        branchName = state.BranchName,
        message = state.CommitMessage ?? $"Sent {convertedCount} elements from {HostApplications.Navisworks.Name}.",
        sourceApplication = HostApplications.Navisworks.Slug
      };

      try
      {
        progressBar.Update(0.5);
        string commitId = await client.CommitCreate(commit);

        progressBar.Update(1.0);
        Application.EndProgress();
        progressBar.Dispose();

        return commitId;
      }
      catch (Exception ex)
      {
        progress.Report.LogOperationError(ex);
      }

      progressBar.Update(1.0);
      Application.EndProgress();
      progressBar.Dispose();

      return null;
    }
  }
}