using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DesktopUI2.Models;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;
using Speckle.ConnectorNavisworks.Objects;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;

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

    public override async Task<string> SendStream(StreamState state, ProgressViewModel progress)
    {
      List<ConvertableObject> filteredObjects = new List<ConvertableObject>();

      // check for converter
      ISpeckleKit defaultKit = KitManager.GetDefaultKit();
      if (defaultKit == null)
      {
        progress.Report.LogOperationError(new SpeckleException("Could not find any Kit!"));
        return null;
      }

      ISpeckleConverter converter = defaultKit.LoadConverter(Utils.VersionedAppName);
      if (converter == null)
      {
        progress.Report.LogOperationError(new SpeckleException($"Could not find Converter{Utils.VersionedAppName}!"));
        return null;
      }

      converter.Report.ReportObjects.Clear();

      CurrentSettings = state.Settings;

      Dictionary<string, string> settings =
        state.Settings.ToDictionary(setting => setting.Slug, setting => setting.Selection);

      converter.SetConverterSettings(settings);

      string streamId = state.StreamId;
      Client client = state.Client;


      if (state.Filter != null)
      {
        filteredObjects.AddRange(GetObjectsFromFilter(state.Filter));
        state.SelectedObjectIds = filteredObjects.Select(x => x.ObjectId).ToList();
      }

      if (filteredObjects.Count == 0)
      {
        progress.Report.LogOperationError(new SpeckleException(
          "Zero objects selected; send stopped. Please select some objects, or check that your filter can actually select something."));
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
        ["units"] = Utils.GetUnits(Doc)
      };

      int convertedCount = 0;

      foreach (ConvertableObject obj in filteredObjects)
      {
        if (progress.CancellationTokenSource.Token.IsCancellationRequested)
        {
          return null;
        }

        Base converted = null;

        string applicationId = string.Empty;

        ApplicationObject reportObject = new ApplicationObject("id", "object_type")
        {
          applicationId = applicationId
        };

        if (obj != null)
        {
          if (!converter.CanConvertToSpeckle(obj))
          {
            reportObject.Update(status: ApplicationObject.State.Skipped,
              logItem: $"Sending this object type is not supported in Navisworks");
            progress.Report.Log(reportObject);
            continue;
          }

          converter.Report.Log(reportObject);
          converted = converter.ConvertToSpeckle(obj);

          if (converted == null)
          {
            reportObject.Update(status: ApplicationObject.State.Failed,
              logItem: $"Conversion returned null");
            progress.Report.Log(reportObject);
            continue;
          }
        }
        else
        {
          progress.Report.LogOperationError(new Exception($"Failed to find doc object."));
          continue;
        }

        conversionProgressDict["Conversion"]++;
        progress.Update(conversionProgressDict);

        converted.applicationId = applicationId;
        if (converted["@SpeckleSchema"] is Base newSchemaBase)
        {
          newSchemaBase.applicationId = applicationId;
          converted["@SpeckleSchema"] = newSchemaBase;
        }

        reportObject.Update(status: ApplicationObject.State.Created,
          logItem: $"Sent as {converted.speckle_type}");
        progress.Report.Log(reportObject);

        convertedCount++;
      }

      progress.Report.Merge(converter.Report);

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
        return null;
      }

      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
      {
        return null;
      }

      CommitCreateInput commit = new CommitCreateInput
      {
        streamId = streamId,
        objectId = objectId,
        branchName = state.BranchName,
        message = state.CommitMessage ?? $"Sent {convertedCount} elements from Rhino.",
        sourceApplication = Utils.VersionedAppName
      };

      try
      {
        string commitId = await client.CommitCreate(commit);
        return commitId;
      }
      catch (Exception ex)
      {
        progress.Report.LogOperationError(ex);
      }

      return null;
    }
  }
}