using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Speckle.ConnectorRevit.Storage;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Speckle.DesktopUI.Utils;
using Stylet;
using RevitElement = Autodesk.Revit.DB.Element;

namespace Speckle.ConnectorRevit.UI
{
  public partial class ConnectorBindingsRevit
  {
    public List<StreamState> DocumentStreams { get; set; } = new List<StreamState>();


    public List<Exception> ConversionErrors { get; set; } = new List<Exception>();

    /// <summary>
    /// Keeps track of errors in the operations of send/receive.
    /// </summary>
    public List<Exception> OperationErrors { get; set; } = new List<Exception>();

    public override List<StreamState> GetStreamsInFile()
    {
      DocumentStreams = StreamStateManager.ReadState(CurrentDoc.Document);
      return DocumentStreams;
    }

    #region Local file i/o

    /// <summary>
    /// Adds a new stream to the file.
    /// </summary>
    /// <param name="state">StreamState passed by the UI</param>
    public override void AddNewStream(StreamState state)
    {
      var index = DocumentStreams.FindIndex(b => b.Stream.id == state.Stream.id);
      if (index == -1)
      {
        DocumentStreams.Add(state);
        WriteStateToFile();
      }
    }

    /// <summary>
    /// Removes a stream from the file.
    /// </summary>
    /// <param name="streamId"></param>
    public override void RemoveStreamFromFile(string streamId)
    {
      var streamState = DocumentStreams.FirstOrDefault(s => s.Stream.id == streamId);
      if (streamState != null)
      {
        DocumentStreams.Remove(streamState);
        WriteStateToFile();
      }
    }

    /// <summary>
    /// Update the stream state and adds adds the filtered objects
    /// </summary>
    /// <param name="state"></param>
    public override void PersistAndUpdateStreamInFile(StreamState state)
    {
      var index = DocumentStreams.FindIndex(b => b.Stream.id == state.Stream.id);
      if (index != -1)
      {
        DocumentStreams[index] = state;
        WriteStateToFile();
      }
    }

    /// <summary>
    /// Transaction wrapper around writing the local streams to the file.
    /// </summary>
    private void WriteStateToFile()
    {
      Queue.Add(new Action(() =>
      {
        using (Transaction t = new Transaction(CurrentDoc.Document, "Speckle Write State"))
        {
          t.Start();
          StreamStateManager.WriteStreamStateList(CurrentDoc.Document, DocumentStreams);
          t.Commit();
        }
      }));
      Executor.Raise();
    }

    #endregion

    /// <summary>
    /// Converts the Revit elements that have been added to the stream by the user, sends them to
    /// the Server and the local DB, and creates a commit with the objects.
    /// </summary>
    /// <param name="state">StreamState passed by the UI</param>
    public override async Task<StreamState> SendStream(StreamState state)
    {
      ConversionErrors.Clear();
      OperationErrors.Clear();

      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(Applications.Revit);
      converter.SetContextDocument(CurrentDoc.Document);

      var streamId = state.Stream.id;
      var client = state.Client;

      var convertedObjects = new List<Base>();
      var failedConversions = new List<RevitElement>();

      var units = CurrentDoc.Document.GetUnits().GetFormatOptions(UnitType.UT_Length).DisplayUnits.ToString()
        .ToLowerInvariant().Replace("dut_", "");
      // InjectScaleInKits(GetScale(units)); // this is used for feet to sane units conversion.

      if (state.Filter != null)
      {
        state.Objects = GetSelectionFilterObjects(state.Filter);
      }

      if (state.Objects.Count == 0)
      {
        Globals.Notify("There are zero objects to send. Please create a filter, or set some via selection.");
        return state;
      }

      var commitObject = new Base();

      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 0;
      Execute.PostToUIThread(() => state.Progress.Maximum = state.Objects.Count());
      var convertedCount = 0;

      foreach (var obj in state.Objects)
      {
        RevitElement revitElement = null;
        if (obj.applicationId != null)
        {
          revitElement = CurrentDoc.Document.GetElement(obj.applicationId);
        }

        if (revitElement == null)
        {
          ConversionErrors.Add(new SpeckleException(message: $"Could not retrieve element: {obj.speckle_type}"));
          continue;
        }

        var conversionResult = converter.ConvertToSpeckle(revitElement);

        conversionProgressDict["Conversion"]++;
        UpdateProgress(conversionProgressDict, state.Progress);

        if (conversionResult == null)
        {
          ConversionErrors.Add(new Exception($"Failed to convert item with id {obj.applicationId}"));
          state.Errors.Add(new Exception($"Failed to convert item with id {obj.applicationId}"));
          continue;
        }

        convertedCount++;

        var category = $"@{revitElement.Category.Name}";
        if (commitObject[category] == null)
        {
          commitObject[category] = new List<Base>();
        }

        ((List<Base>)commitObject[category]).Add(conversionResult);

      }

      if (converter.ConversionErrors.Count != 0)
      {
        // TODO: Get rid of the custom Error class. It's not needed.
        // PS: The errors seem to be quite bare at the moment?
        ConversionErrors.AddRange(converter.ConversionErrors.Select(x => new Exception($"{x.Message}\n{x.Message}")));
      }

      if (convertedCount == 0)
      {
        Globals.Notify("Failed to convert any objects. Push aborted.");
        return state;
      }

      Execute.PostToUIThread(() => state.Progress.Maximum = (int)commitObject.GetTotalChildrenCount());

      if (state.CancellationTokenSource.Token.IsCancellationRequested)
      {
        return state;
      }

      var transports = new List<ITransport>() { new ServerTransport(client.Account, streamId) };

      var objectId = await Operations.Send(
        @object: commitObject,
        cancellationToken: state.CancellationTokenSource.Token,
        transports: transports,
        onProgressAction: dict => UpdateProgress(dict, state.Progress),
        onErrorAction: (s, e) =>
        {
          OperationErrors.Add(e); // TODO!
          state.Errors.Add(e);
          state.CancellationTokenSource.Cancel();
        } 
        );

      if (OperationErrors.Count != 0)
      {
        Globals.Notify("Failed to send.");
        return state;
      }

      if (state.CancellationTokenSource.Token.IsCancellationRequested)
      {
        return null;
      }

      var actualCommit = new CommitCreateInput()
      {
        streamId = streamId,
        objectId = objectId,
        branchName = state.Branch.name,
        message = state.CommitMessage != null ? state.CommitMessage : $"Pushed {convertedCount} objs from {Applications.Revit}."
      };

      if (state.PreviousCommitId != null) { actualCommit.previousCommitIds = new List<string>() { state.PreviousCommitId }; }

      try
      {
        var res = await client.CommitCreate(actualCommit);

        var updatedStream = await client.StreamGet(streamId);
        state.Branches = updatedStream.branches.items;
        state.Stream.name = updatedStream.name;
        state.Stream.description = updatedStream.description;

        WriteStateToFile();
        RaiseNotification($"{convertedCount} objects sent to Speckle 🚀");
      }
      catch (Exception e)
      {
        state.Errors.Add(e);
        Globals.Notify($"Failed to create commit.\n{e.Message}");
      }
      
      return state;
    }

    public override async Task<StreamState> ReceiveStream(StreamState state)
    {
      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(Applications.Revit);
      converter.SetContextDocument(CurrentDoc.Document);

      var transport = new ServerTransport(state.Client.Account, state.Stream.id);
      var newStream = await state.Client.StreamGet(state.Stream.id);
      var commit = newStream.branches.items[0].commits.items[0];
      Base commitObject;

      if (state.CancellationTokenSource.Token.IsCancellationRequested)
      {
        return null;
      }

      commitObject = await Operations.Receive(commit.referencedObject, state.CancellationTokenSource.Token, transport,
        onProgressAction: dict => UpdateProgress(dict, state.Progress),
        onTotalChildrenCountKnown: count => Execute.PostToUIThread(() => state.Progress.Maximum = count));

      if (state.CancellationTokenSource.Token.IsCancellationRequested)
      {
        return null;
      }

      var newObjects = new List<Base>();
      var oldObjects = state.Objects;

      var data = (List<object>)commitObject["@data"];
      try
      {
        newObjects = data.Select(o => (Base)o)?.ToList();
      }
      catch (Exception e)
      {
        Log.CaptureException(e);
        state.Stream = newStream;
        state.Objects = new List<Base>() { commitObject };
        WriteStateToFile();
        RaiseNotification($"Received stream, but could not convert objects to Revit");
        return state;
      }

      // TODO: edit objects from connector so we don't need to delete and recreate everything
      // var toDelete = oldObjects.Except(newObjects, new BaseObjectComparer()).ToList();
      // var toCreate = newObjects;
      var toDelete = oldObjects;
      var toUpdate = newObjects;

      var revitElements = new List<object>();
      var errors = new List<SpeckleException>();

      // TODO diff stream states

      // delete
      Queue.Add(() =>
      {
        using (var t = new Transaction(CurrentDoc.Document, $"Speckle Delete: ({state.Stream.id})"))
        {
          t.Start();
          foreach (var oldObj in toDelete)
          {
            var revitElement = CurrentDoc.Document.GetElement(oldObj.applicationId);
            if (revitElement == null)
            {
              errors.Add(new SpeckleException(message: "Could not retrieve element"));
              Debug.WriteLine(
                $"Could not retrieve element (id: {oldObj.applicationId}, type: {oldObj.speckle_type})");
              continue;
            }

            CurrentDoc.Document.Delete(revitElement.Id);
          }

          t.Commit();
        }
      });
      Executor.Raise();

      // update or create
      Queue.Add(() =>
      {
        using (var t = new Transaction(CurrentDoc.Document, $"Speckle Receive: ({state.Stream.id})"))
        {
          // TODO `t.SetFailureHandlingOptions`
          t.Start();
          revitElements = converter.ConvertToNative(toUpdate);
          t.Commit();
        }
      });
      Executor.Raise();

      if (errors.Any() || converter.ConversionErrors.Any())
      {
        var convErrors = converter.ConversionErrors.Count;
        var err = errors.Count;
        Log.CaptureException(new SpeckleException(
          $"{convErrors} conversion error{Formatting.PluralS(convErrors)} and {err} error{Formatting.PluralS(err)}"));
      }

      state.Stream = newStream;
      state.Objects = newObjects;
      WriteStateToFile();
      RaiseNotification($"Deleting {toDelete.Count} elements and updating {toUpdate.Count} elements...");

      return state;
    }

    private void UpdateProgress(ConcurrentDictionary<string, int> dict, ProgressReport progress)
    {
      if (progress == null)
      {
        return;
      }

      Execute.PostToUIThread(() =>
      {
        progress.ProgressDict = dict;
        progress.Value = dict.Values.Last();
      });
    }

    public override List<string> GetSelectedObjects()
    {
      if (CurrentDoc == null)
      {
        return new List<string>();
      }

      var selectedObjects = CurrentDoc.Selection.GetElementIds().Select(id => CurrentDoc.Document.GetElement(id).UniqueId).ToList();
      return selectedObjects;
    }

    public override List<string> GetObjectsInView()
    {
      if (CurrentDoc == null)
      {
        return new List<string>();
      }

      var collector = new FilteredElementCollector(CurrentDoc.Document, CurrentDoc.Document.ActiveView.Id).WhereElementIsNotElementType();
      var elementIds = collector.ToElements().Select(el => el.UniqueId).ToList(); ;

      return elementIds;
    }

    #region private methods

    /// <summary>
    /// Given the filter in use by a stream returns the document elements that match it.
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    private List<Base> GetSelectionFilterObjects(ISelectionFilter filter)
    {
      var doc = CurrentDoc.Document;

      var selectionIds = new List<string>();

      switch (filter.Name)
      {
        case "Category":
          var catFilter = filter as ListSelectionFilter;
          var bics = new List<BuiltInCategory>();
          var categories = ConnectorRevitUtils.GetCategories(doc);
          IList<ElementFilter> elementFilters = new List<ElementFilter>();

          foreach (var cat in catFilter.Selection)
          {
            elementFilters.Add(new ElementCategoryFilter(categories[cat].Id));
          }

          var categoryFilter = new LogicalOrFilter(elementFilters);

          selectionIds = new FilteredElementCollector(doc)
            .WhereElementIsNotElementType()
            .WhereElementIsViewIndependent()
            .WherePasses(categoryFilter)
            .Select(x => x.UniqueId).ToList();
          break;

        case "View":
          var viewFilter = filter as ListSelectionFilter;

          var views = new FilteredElementCollector(doc)
            .WhereElementIsNotElementType()
            .OfClass(typeof(View))
            .Where(x => viewFilter.Selection.Contains(x.Name));

          foreach (var view in views)
          {
            var ids = new FilteredElementCollector(doc, view.Id)
              .WhereElementIsNotElementType()
              .WhereElementIsViewIndependent()
              .Where(x => x.IsPhysicalElement())
              .Select(x => x.UniqueId).ToList();

            selectionIds = selectionIds.Union(ids).ToList();
          }
          break;

        case "Parameter":
          try
          {
            var propFilter = filter as PropertySelectionFilter;
            var query = new FilteredElementCollector(doc)
              .WhereElementIsNotElementType()
              .WhereElementIsNotElementType()
              .WhereElementIsViewIndependent()
              .Where(x => x.IsPhysicalElement())
              .Where(fi => fi.LookupParameter(propFilter.PropertyName) != null);

            propFilter.PropertyValue = propFilter.PropertyValue.ToLowerInvariant();

            switch (propFilter.PropertyOperator)
            {
              case "equals":
                query = query.Where(fi =>
                  GetStringValue(fi.LookupParameter(propFilter.PropertyName)) == propFilter.PropertyValue);
                break;
              case "contains":
                query = query.Where(fi =>
                  GetStringValue(fi.LookupParameter(propFilter.PropertyName)).Contains(propFilter.PropertyValue));
                break;
              case "is greater than":
                query = query.Where(fi => UnitUtils.ConvertFromInternalUnits(
                                            fi.LookupParameter(propFilter.PropertyName).AsDouble(),
                                            fi.LookupParameter(propFilter.PropertyName).DisplayUnitType) >
                                          double.Parse(propFilter.PropertyValue));
                break;
              case "is less than":
                query = query.Where(fi => UnitUtils.ConvertFromInternalUnits(
                                            fi.LookupParameter(propFilter.PropertyName).AsDouble(),
                                            fi.LookupParameter(propFilter.PropertyName).DisplayUnitType) <
                                          double.Parse(propFilter.PropertyValue));
                break;
            }

            selectionIds = query.Select(x => x.UniqueId).ToList();
          }
          catch (Exception e)
          {
            Log.CaptureException(e);
          }
          break;
      }

      return selectionIds.Select(id => new Base { applicationId = id }).ToList();
    }

    private string GetStringValue(Parameter p)
    {
      string value = "";
      if (!p.HasValue)
      {
        return value;
      }

      if (string.IsNullOrEmpty(p.AsValueString()) && string.IsNullOrEmpty(p.AsString()))
      {
        return value;
      }

      if (!string.IsNullOrEmpty(p.AsValueString()))
      {
        return p.AsValueString().ToLowerInvariant();
      }
      else
      {
        return p.AsString().ToLowerInvariant();
      }
    }

    #endregion
  }
}
