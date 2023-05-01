#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Avalonia.Threading;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;
using Revit.Async;
using Serilog.Context;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace Speckle.ConnectorRevit.UI
{
  public partial class ConnectorBindingsRevit
  {
    // used to store the Stream State settings when sending/receiving
    private List<ISetting>? CurrentSettings { get; set; }

    /// <summary>
    /// Converts the Revit elements that have been added to the stream by the user, sends them to
    /// the Server and the local DB, and creates a commit with the objects.
    /// </summary>
    /// <param name="state">StreamState passed by the UI</param>
    public override async Task<string> SendStream(StreamState state, ProgressViewModel progress)
    {
      //make sure to instance a new copy so all values are reset correctly
      var converter = (ISpeckleConverter)Activator.CreateInstance(Converter.GetType());
      converter.SetContextDocument(CurrentDoc.Document);
      converter.Report.ReportObjects.Clear();

      // set converter settings as tuples (setting slug, setting selection)
      var settings = new Dictionary<string, string>();
      CurrentSettings = state.Settings;
      foreach (var setting in state.Settings)
        settings.Add(setting.Slug, setting.Selection);
      converter.SetConverterSettings(settings);

      var streamId = state.StreamId;
      var client = state.Client;

      var selectedObjects = GetSelectionFilterObjects(state.Filter);
      state.SelectedObjectIds = selectedObjects.Select(x => x.UniqueId).ToList();

      if (!selectedObjects.Any())
        throw new InvalidOperationException(
          "There are zero objects to send. Please use a filter, or set some via selection."
        );

      converter.SetContextObjects(
        selectedObjects
          .Select(x => new ApplicationObject(x.UniqueId, x.GetType().ToString()) { applicationId = x.UniqueId })
          .ToList()
      );
      var commitObject = converter.ConvertToSpeckle(CurrentDoc.Document) ?? new Base();

      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 0;

      progress.Max = selectedObjects.Count;
      var convertedCount = 0;

      await RevitTask
        .RunAsync(_ =>
        {
          foreach (var revitElement in selectedObjects)
          {
            if (progress.CancellationToken.IsCancellationRequested)
              break;

            bool isAlreadyConverted = GetOrCreateApplicationObject(
              revitElement,
              converter.Report,
              out ApplicationObject reportObj
            );
            if (isAlreadyConverted)
              continue;

            progress.Report.Log(reportObj);

            //Add context to logger
            using var _d0 = LogContext.PushProperty("conversionDirection", nameof(ISpeckleConverter.ConvertToSpeckle));
            using var _d1 = LogContext.PushProperty("elementType", revitElement.GetType());
            using var _d2 = LogContext.PushProperty("elementCategory", revitElement.Category.Name);

            try
            {
              converter.Report.Log(reportObj); // Log object so converter can access

              Base result = ConvertToSpeckle(revitElement, converter);

              HandleToSpeckleObject(result, commitObject, reportObj, revitElement);
            }
            catch (ConversionSkippedException ex)
            {
              reportObj.Update(status: ApplicationObject.State.Skipped, logItem: ex.Message);
            }
            catch (Exception ex)
            {
              SpeckleLog.Logger.Error(ex, "Object failed during conversion");
              reportObj.Update(status: ApplicationObject.State.Failed, logItem: $"{ex.Message}");
            }

            conversionProgressDict["Conversion"]++;
            progress.Update(conversionProgressDict);

            convertedCount++;

            YeildToUIThread(TimeSpan.FromMilliseconds(1));
          }
        })
        .ConfigureAwait(false);

      progress.Report.Merge(converter.Report);

      progress.CancellationToken.ThrowIfCancellationRequested();

      if (convertedCount == 0)
      {
        throw new SpeckleException("Zero objects converted successfully. Send stopped.");
      }

      var transports = new List<ITransport>() { new ServerTransport(client.Account, streamId) };

      var objectId = await Operations
        .Send(
          @object: commitObject,
          cancellationToken: progress.CancellationToken,
          transports: transports,
          onProgressAction: dict => progress.Update(dict),
          onErrorAction: ConnectorHelpers.DefaultSendErrorHandler,
          disposeTransports: true
        )
        .ConfigureAwait(true);

      progress.CancellationToken.ThrowIfCancellationRequested();

      var actualCommit = new CommitCreateInput()
      {
        streamId = streamId,
        objectId = objectId,
        branchName = state.BranchName,
        message = state.CommitMessage ?? $"Sent {convertedCount} objects from {ConnectorRevitUtils.RevitAppName}.",
        sourceApplication = ConnectorRevitUtils.RevitAppName,
      };

      if (state.PreviousCommitId != null)
      {
        actualCommit.parents = new List<string>() { state.PreviousCommitId };
      }

      var commitId = await ConnectorHelpers
        .CreateCommit(progress.CancellationToken, client, actualCommit)
        .ConfigureAwait(false);

      return commitId;
    }

    private static bool GetOrCreateApplicationObject(
      Element revitElement,
      ProgressReport report,
      out ApplicationObject reportObj
    )
    {
      if (report.ReportObjects.TryGetValue(revitElement.UniqueId, out var applicationObject))
      {
        reportObj = applicationObject;
        return true;
      }
      
      string descriptor = ConnectorRevitUtils.ObjectDescriptor(revitElement);
      reportObj = new(revitElement.UniqueId, descriptor) { applicationId = revitElement.UniqueId };
      return false;
    }

    private static void YeildToUIThread(TimeSpan delay)
    {
      using CancellationTokenSource s = new(delay);
      Dispatcher.UIThread.MainLoop(s.Token);
    }

    private static Base ConvertToSpeckle(Element revitElement, ISpeckleConverter converter)
    {
      if (!converter.CanConvertToSpeckle(revitElement))
      {
        string skipMessage = revitElement switch
        {
          RevitLinkInstance e => "Enable linked model support from the settings to send this object",
          _ => "Sending this object type is not supported yet"
        };

        throw new ConversionSkippedException(skipMessage, revitElement);
      }

      Base conversionResult = converter.ConvertToSpeckle(revitElement);

      if (conversionResult == null)
        throw new SpeckleException($"Conversion of {revitElement.UniqueId} (ToSpeckle) returned null");

      return conversionResult;
    }

    private static void HandleToSpeckleObject(
      Base conversionResult,
      Base commitObject,
      ApplicationObject reportObject,
      Element revitElement
    )
    {
      // here we are checking to see if we're receiving an object that has a host
      // but the host doesn't know that it is a host
      if (conversionResult["speckleHost"] is Base host && host["category"] is string catName)
      {
        //ensure we use english names for hosted elements too!
        var cat = ConnectorRevitUtils.GetCategories(CurrentDoc.Document)[catName];
        catName = ConnectorRevitUtils.GetEnglishCategoryName(cat);
        var objs = (List<Base>)(commitObject[$"@{catName}"] ??= new List<Base>());

        var hostIndex = objs.FindIndex(obj => obj.applicationId == host.applicationId);
        // if the "host" is present, then it has already been converted and we need to
        // attach the current, dependent, elements as a hosted element
        if (hostIndex != -1)
        {
          objs[hostIndex]["elements"] ??= new List<Base>();
          ((List<Base>)objs[hostIndex]["elements"]).Add(conversionResult);
        }
        // if host is not present, then it hasn't been converted yet
        // create a placeholder that will be overridden later, but that will contain the hosted element
        else
        {
          var newBase = new Base
          {
            applicationId = host.applicationId,
            ["elements"] = new List<Base>() { conversionResult }
          };
          objs.Add(newBase);
        }

        // remove the speckleHost element that we added
        conversionResult["speckleHost"] = null;

        reportObject.Update(
          status: ApplicationObject.State.Created,
          logItem: $"Attached as hosted element to {host.applicationId}"
        );
      }
      //is an element type, nest it under Types instead
      else if (revitElement is ElementType)
      {
        var category = $"@{ConnectorRevitUtils.GetEnglishCategoryName(revitElement.Category)}";

        var types = (Base)(commitObject["Types"] ??= new Base());
        var c = (IList<Base>)(types[category] ??= new List<Base>());

        c.Add(conversionResult);
      }
      else
      {
        var category = conversionResult.GetType().Name switch
        {
          "Network" => "@Networks",
          "FreeformElement" => "@FreeformElement",
          _ => $"@{ConnectorRevitUtils.GetEnglishCategoryName(revitElement.Category)}"
        };

        var objs = (List<Base>)(commitObject[category] ??= new List<Base>());
        var hostIndex = objs.FindIndex(obj => obj.applicationId == conversionResult.applicationId);

        // here we are checking to see if we're converting a host that doesn't know it is a host
        // and if dependent elements of that host have already been converted
        if (hostIndex != -1 && objs[hostIndex]["elements"] is List<Base> elements)
        {
          objs.RemoveAt(hostIndex);
          if (conversionResult["elements"] is List<Base> els)
            els.AddRange(elements);
          else
            conversionResult["elements"] = elements;
        }

        objs.Add(conversionResult);
      }

      reportObject.Update(
        status: ApplicationObject.State.Created,
        logItem: $"Sent as {ConnectorRevitUtils.SimplifySpeckleType(conversionResult.speckle_type)}"
      );
    }
  }
}
