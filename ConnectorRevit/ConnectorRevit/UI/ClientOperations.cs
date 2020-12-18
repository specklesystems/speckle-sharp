using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
      var converter = kit.LoadConverter(ConnectorRevitUtils.RevitAppName);
      converter.SetContextDocument(CurrentDoc.Document);

      var streamId = state.Stream.id;
      var client = state.Client;

      var convertedObjects = new List<Base>();

      if (state.Filter != null)
      {
        state.Objects = GetSelectionFilterObjects(state.Filter);
      }

      if (state.Objects.Count == 0)
      {
        state.Errors.Add(new Exception("There are zero objects to send. Please create a filter, or set some via selection."));
        return state;
      }

      var commitObject = new Base();

      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 0;
      Execute.PostToUIThread(() => state.Progress.Maximum = state.Objects.Count());
      var convertedCount = 0;

      var placeholders = new List<Base>();

      converter.SetContextObjects(state.Objects.Select(obj => new ApplicationPlaceholderObject { applicationId = obj.applicationId }).ToList());
      foreach (var obj in state.Objects)
      {
        try
        {
          RevitElement revitElement = null;
          if (obj.applicationId != null)
          {
            revitElement = CurrentDoc.Document.GetElement(obj.applicationId);
          }

          if (revitElement == null)
          {
            continue;
          }

          if (!converter.CanConvertToSpeckle(revitElement))
          {
            state.Errors.Add(new Exception($"Skipping {revitElement.GetType()}, not supported"));
            continue;
          }



          var conversionResult = converter.ConvertToSpeckle(revitElement);

          conversionProgressDict["Conversion"]++;
          UpdateProgress(conversionProgressDict, state.Progress);

          placeholders.Add(new ApplicationPlaceholderObject { applicationId = obj.applicationId, ApplicationGeneratedId = obj.applicationId });

          convertedCount++;



          //hosted elements will be returned as `null` by the ConvertToSpeckle method 
          //since they are handled when converting their parents
          if (conversionResult != null)
          {
            var category = $"@{revitElement.Category.Name}";
            if (commitObject[category] == null)
            {
              commitObject[category] = new List<Base>();
            }
             ((List<Base>)commitObject[category]).Add(conversionResult);
          }

        }
        catch (Exception e)
        {
          state.Errors.Add(e);
        }

        //var level = (Level) CurrentDoc.Document.GetElement(revitElement.LevelId);

        //if(commitObject[$"@{level.Name}"] == null)
        //{
        //  commitObject[$"@{level.Name}"] = new Base
        //  {
        //    ["@objects"] = new List<Base>(),
        //    ["elevation"] = level.Elevation, // TODO Cast! 
        //  };
        //}

      }

      if (converter.ConversionErrors.Count != 0)
      {
        // TODO: Get rid of the custom Error class. It's not needed.
        ConversionErrors.AddRange(converter.ConversionErrors.Select(x => new Exception($"{x.Message}\n{x.details}")));
        state.Errors.AddRange(converter.ConversionErrors.Select(x => new Exception($"{x.Message}\n{x.details}")));
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
        state.Errors.AddRange(OperationErrors);
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
        message = state.CommitMessage != null ? state.CommitMessage : $"Sent {convertedCount} objs from {ConnectorRevitUtils.RevitAppName}."
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

    /// <summary>
    /// Receives a stream and bakes into the existing revit file.
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    public override async Task<StreamState> ReceiveStream(StreamState state)
    {
      ConversionErrors.Clear();
      OperationErrors.Clear();

      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(ConnectorRevitUtils.RevitAppName);
      converter.SetContextDocument(CurrentDoc.Document);

      var transport = new ServerTransport(state.Client.Account, state.Stream.id);

      string referencedObject = state.Commit.referencedObject;

      //if "latest", always make sure we get the latest commit when the user clicks "receive"
      if (state.Commit.id == "latest")
      {
        var res = await state.Client.BranchGet(state.CancellationTokenSource.Token, state.Stream.id, state.Branch.name, 1);
        referencedObject = res.commits.items.FirstOrDefault().referencedObject;
      }

      var commit = state.Commit;

      if (state.CancellationTokenSource.Token.IsCancellationRequested)
      {
        return null;
      }

      var commitObject = await Operations.Receive(
          referencedObject,
          state.CancellationTokenSource.Token,
          transport,
          onProgressAction: dict => UpdateProgress(dict, state.Progress),
          onErrorAction: (s, e) =>
          {
            OperationErrors.Add(e);
            state.Errors.Add(e);
            state.CancellationTokenSource.Cancel();
          },
          onTotalChildrenCountKnown: count => Execute.PostToUIThread(() => state.Progress.Maximum = count)
          );

      if (OperationErrors.Count != 0)
      {
        Globals.Notify("Failed to get commit.");
        return state;
      }

      if (state.CancellationTokenSource.Token.IsCancellationRequested)
      {
        return null;
      }

      UpdateProgress(new ConcurrentDictionary<string, int>() { ["Converting"] = 1 }, state.Progress);

      // Bake the new ones.
      Queue.Add(() =>
      {
        // needs to be set for editing to work
        converter.SetPreviousContextObjects(state.ReceivedObjects);

        //TODO: flatten stream and add its stuff here
        //converter.SetContextObjects(state.ReceivedObjects);

        using (var t = new Transaction(CurrentDoc.Document, $"Baking stream {state.Stream.name}"))
        {
          t.Start();

          var newPlaceholderObjects = HandleAndConvertToNative(commitObject, converter, state);

          foreach (var obj in state.ReceivedObjects)
          {
            var indexInNew = newPlaceholderObjects.FindIndex(x => x.applicationId == obj.applicationId);
            if (indexInNew == -1)
            {
              var existing = CurrentDoc.Document.GetElement(obj.ApplicationGeneratedId);
              if (existing != null)
              {
                CurrentDoc.Document.Delete(existing.Id);
              }
            }
          }

          state.ReceivedObjects = newPlaceholderObjects;
          state.Errors.AddRange(converter.ConversionErrors.Select(e => new Exception($"{e.message}: {e.details}")));

          t.Commit();
        }
      });

      Executor.Raise();

      try
      {
        var updatedStream = await state.Client.StreamGet(state.Stream.id);
        state.Branches = updatedStream.branches.items;
        state.Stream.name = updatedStream.name;
        state.Stream.description = updatedStream.description;

        WriteStateToFile();
      }
      catch (Exception e)
      {
        WriteStateToFile();
        state.Errors.Add(e);
        Globals.Notify($"Receiving done, but failed to update stream from server.\n{e.Message}");
      }

      return state;
    }

    /// <summary>
    /// Recurses through the commit object and converts objects as it goes though them. 
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="converter"></param>
    /// <returns></returns>
    private List<ApplicationPlaceholderObject> HandleAndConvertToNative(object obj, ISpeckleConverter converter, StreamState state)
    {
      HashSet<string> appIds = new HashSet<string>();
      List<Base> objects = new List<Base>();

      List<ApplicationPlaceholderObject> placeholders = new List<ApplicationPlaceholderObject>();

      if (obj is Base baseItem)
      {
        if (converter.CanConvertToNative(baseItem))
        {
          if (baseItem.applicationId != null)
          {
            appIds.Add(baseItem.applicationId);
          }

          objects.Add(baseItem);

          try
          {

            // --> return a list of ApplicationPlaceholderObject so we can do deletion afterwards
            var convRes = converter.ConvertToNative(baseItem);
            if (convRes is ApplicationPlaceholderObject placeholder)
            {
              placeholders.Add(placeholder);
            }
            else if (convRes is List<ApplicationPlaceholderObject> placeholderList)
            {
              placeholders.AddRange(placeholderList);
            }
          }
          catch (Exception e)
          {
            state.Errors.Add(e);
          }

          return placeholders;
        }
        else
        {
          foreach (var prop in baseItem.GetDynamicMembers())
          {
            placeholders.AddRange(HandleAndConvertToNative(baseItem[prop], converter, state));
          }
          return placeholders;
        }
      }

      if (obj is List<object> list)
      {
        foreach (var listObj in list)
        {
          placeholders.AddRange(HandleAndConvertToNative(listObj, converter, state));
        }
        return placeholders;
      }

      if (obj is IDictionary dict)
      {
        foreach (DictionaryEntry kvp in dict)
        {
          placeholders.AddRange(HandleAndConvertToNative(kvp.Value, converter, state));
        }
        return placeholders;
      }

      return placeholders;
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

    #region selection, views and filters

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
