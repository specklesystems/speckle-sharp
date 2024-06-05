using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Rhino.DocObjects;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace SpeckleRhino;

public partial class ConnectorBindingsRhino : ConnectorBindings
{
  public PreviewConduit PreviewConduit { get; set; }

  public override bool CanPreviewSend => true;

  public override void PreviewSend(StreamState state, ProgressViewModel progress)
  {
    // report and converter
    progress.Report = new ProgressReport();
    var converter = KitManager.GetDefaultKit().LoadConverter(Utils.GetRhinoHostAppVersion());

    converter.SetContextDocument(Doc);

    var filterObjs = GetObjectsFromFilter(state.Filter);
    var idsToSelect = new List<string>();
    int successful = 0;
    foreach (var id in filterObjs)
    {
      if (Utils.FindObjectBySelectedId(Doc, id, out object obj, out string descriptor))
      {
        // create applicationObject
        ApplicationObject reportObj = new(id, descriptor);
        var applicationId = string.Empty;
        switch (obj)
        {
          case RhinoObject o:
            applicationId = o.Attributes.GetUserString(ApplicationIdKey) ?? id;
            if (converter.CanConvertToSpeckle(obj))
            {
              reportObj.Update(status: ApplicationObject.State.Created);
            }
            else
            {
              reportObj.Update(
                status: ApplicationObject.State.Failed,
                logItem: "Object type conversion to Speckle not supported"
              );
            }

            idsToSelect.Add(id);
            successful++;
            break;
          case Layer o:
            applicationId = o.GetUserString(ApplicationIdKey) ?? id;
            reportObj.Update(status: ApplicationObject.State.Created);
            successful++;
            break;
          case ViewInfo o:
            reportObj.Update(status: ApplicationObject.State.Created);
            successful++;
            break;
        }
        reportObj.applicationId = applicationId;
        progress.Report.Log(reportObj);
      }
      else
      {
        progress.Report.Log(
          new ApplicationObject(id, "Unknown")
          {
            Status = ApplicationObject.State.Failed,
            Log = new List<string> { "Could not find object in document" }
          }
        );
      }
    }

    if (successful == 0)
    {
      throw new InvalidOperationException("No valid objects selected, nothing will be sent!");
    }

    // TODO: instead of selection, consider saving current visibility of objects in doc, hiding everything except selected, and restoring original states on cancel
    Doc.Objects.UnselectAll(false);
    SelectClientObjects(idsToSelect);
    Doc.Views.Redraw();
  }

  public override bool CanPreviewReceive => true;

  private static bool IsPreviewIgnore(Base @object)
  {
    return @object.speckle_type.Contains("Instance")
      || @object.speckle_type.Contains("View")
      || @object.speckle_type.Contains("Level")
      || @object.speckle_type.Contains("GridLine")
      || @object.speckle_type.Contains("Collection")
      || @object.speckle_type.Contains("PolygonElement")
      || @object.speckle_type.Contains("GisFeature");
  }

  public override async Task<StreamState> PreviewReceive(StreamState state, ProgressViewModel progress)
  {
    // first check if commit is the same and preview objects have already been generated
    Commit commit = await ConnectorHelpers.GetCommitFromState(state, progress.CancellationToken);
    progress.Report = new ProgressReport();

    if (commit.id != SelectedReceiveCommit)
    {
      // check for converter
      var converter = KitManager.GetDefaultKit().LoadConverter(Utils.GetRhinoHostAppVersion());
      converter.SetContextDocument(Doc);

      // set converter settings
      CurrentSettings = state.Settings;
      var settings = GetSettingsDict(CurrentSettings);
      converter.SetConverterSettings(settings);

      var commitObject = await ConnectorHelpers.ReceiveCommit(commit, state, progress);

      SelectedReceiveCommit = commit.id;
      ClearStorage();

      var commitLayerName = DesktopUI2.Formatting.CommitInfo(state.CachedStream.name, state.BranchName, commit.id); // get commit layer name
      Preview = FlattenCommitObject(commitObject, converter);
      Doc.Notes += "%%%" + commitLayerName; // give converter a way to access commit layer info

      // Convert preview objects
      foreach (var previewObj in Preview)
      {
        previewObj.CreatedIds = new List<string> { previewObj.OriginalId }; // temporary store speckle id as created id for Preview report selection to work

        var storedObj = StoredObjects[previewObj.OriginalId];
        if (IsPreviewIgnore(storedObj))
        {
          var status = previewObj.Convertible ? ApplicationObject.State.Created : ApplicationObject.State.Skipped;
          previewObj.Update(status: status, logItem: "No preview available");
          progress.Report.Log(previewObj);
          continue;
        }

        if (previewObj.Convertible)
        {
          previewObj.Converted = ConvertObject(storedObj, converter);
        }
        else
        {
          foreach (var fallback in previewObj.Fallback)
          {
            var storedFallback = StoredObjects[fallback.OriginalId];
            fallback.Converted = ConvertObject(storedFallback, converter);
          }
        }

        if (previewObj.Converted == null || previewObj.Converted.Count == 0)
        {
          var convertedFallback = previewObj.Fallback.Where(o => o.Converted != null || o.Converted.Count > 0);
          if (convertedFallback != null && convertedFallback.Count() > 0)
          {
            previewObj.Update(
              status: ApplicationObject.State.Created,
              logItem: $"Creating with {convertedFallback.Count()} fallback values"
            );
          }
          else
          {
            previewObj.Update(
              status: ApplicationObject.State.Failed,
              logItem: "Couldn't convert object or any fallback values"
            );
          }
        }
        else
        {
          previewObj.Status = ApplicationObject.State.Created;
        }

        progress.Report.Log(previewObj);
      }
      progress.Report.Merge(converter.Report);

      // undo notes edit
      var segments = Doc.Notes.Split(new[] { "%%%" }, StringSplitOptions.None).ToList();
      Doc.Notes = segments[0];
    }
    else // just generate the log
    {
      foreach (var previewObj in Preview)
      {
        progress.Report.Log(previewObj);
      }
    }

    // create display conduit
    PreviewConduit = new PreviewConduit(Preview);
    if (PreviewConduit.Preview.Count == 0)
    {
      SpeckleLog.Logger.Information("No previewable geometry was found.");
      progress.Report.OperationErrors.Add(new Exception($"No previewable objects found."));
      ResetDocument();
      return null;
    }

    PreviewConduit.Enabled = true;
    Doc.Views.ActiveView.ActiveViewport.ZoomBoundingBox(PreviewConduit.bbox);
    Doc.Views.Redraw();

    if (progress.CancellationToken.IsCancellationRequested)
    {
      PreviewConduit.Enabled = false;
      ResetDocument();
      return null;
    }

    return state;
  }
}
