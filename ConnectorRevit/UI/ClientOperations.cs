using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.DB;
using Newtonsoft.Json;
using Speckle.ConnectorRevit.Storage;
using Speckle.Core.Api;
using Speckle.Core.Models;
using Speckle.DesktopUI.Utils;

namespace Speckle.ConnectorRevit.UI
{
  public partial class ConnectorBindingsRevit
  {
    public override void AddNewStream(StreamBox streamBox)
    {
      // add stream and related data to the class
      StreamBoxesListWrapper.streamBoxes.Add(streamBox);
      LocalState.Add(streamBox.stream);

      // do the Revit dance to write to file
      Queue.Add(new Action(() =>
      {
        using (Transaction t = new Transaction(CurrentDoc.Document, "Adding Speckle Stream"))
        {
          t.Start();
          SpeckleStateManager.WriteState(CurrentDoc.Document, LocalState);
          StreamBoxStorageManager.WriteStreamBoxes(CurrentDoc.Document, StreamBoxesListWrapper);
          t.Commit();
        }
      }));
      Executor.Raise();

      // TODO add client id
      GetSelectionFilterObjects(streamBox.filter, streamBox.accountId, streamBox.stream.id);
    }

    // public override void AddNewClient(string args)
    // {
    //   var client = JsonConvert.DeserializeObject<dynamic>(args);
    //   StreamBoxesListWrapper.clients.Add(client);
    //
    //   // TODO: Add stream to LocalState (do we actually need to??? hm...).
    //   var myStream = new Stream() { id = (string)client.streamId };
    //
    //   LocalState.Add(myStream);
    //
    //   Queue.Add(new Action(() =>
    //   {
    //     using (Transaction t = new Transaction(CurrentDoc.Document, "Adding Speckle Sender"))
    //     {
    //       t.Start();
    //       SpeckleStateManager.WriteState(CurrentDoc.Document, LocalState);
    //       StreamBoxStorageManager.WriteClients(CurrentDoc.Document, StreamBoxesListWrapper);
    //       t.Commit();
    //     }
    //   }));
    //   Executor.Raise();
    //
    //   ISelectionFilter filter = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(client.filter), GetFilterType(client.filter.Type.ToString()));
    //   GetSelectionFilterObjects(filter, client._id.ToString(), client.streamId.ToString());
    //
    // }

    public override void UpdateClient(string args)
    {
      var client = JsonConvert.DeserializeObject<dynamic>(args);
      var index = StreamBoxesListWrapper.streamBoxes.FindIndex(cl => (string)cl.accountId == (string)client._id);
      StreamBoxesListWrapper.streamBoxes[index] = client;

      var myStream = LocalState.FirstOrDefault(st => st.id == (string)client.streamId);
      myStream.name = (string)client.name;

      Queue.Add(new Action(() =>
      {
        using (Transaction t = new Transaction(CurrentDoc.Document, "Update Speckle Sender"))
        {
          t.Start();
          SpeckleStateManager.WriteState(CurrentDoc.Document, LocalState);
          StreamBoxStorageManager.WriteStreamBoxes(CurrentDoc.Document, StreamBoxesListWrapper);
          t.Commit();
        }
      }));
      Executor.Raise();

      ISelectionFilter filter = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(client.filter), GetFilterType(client.filter.Type.ToString()));
      GetSelectionFilterObjects(filter, client._id.ToString(), client.streamId.ToString());
    }

    // NOTE: This is actually triggered when clicking "Push!"
    // TODO: Orchestration
    // Create buckets, send sequentially, notify ui re upload progress
    // NOTE: Problems with local context and cache: we seem to not sucesffuly pass through it
    // perhaps we're not storing the right sent object (localcontext.addsentobject)
    //public override void PushClient(string args)
    //{
    //  var client = JsonConvert.DeserializeObject<dynamic>(args);

    //  //if it's a category or property filter we need to refresh the list of objects
    //  //if it's a selection filter just use the objects that were stored previously
    //  ISelectionFilter filter = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(client.filter), GetFilterType(client.filter.Type.ToString()));
    //  IEnumerable<Base> objects = new List<Base>();

    //  objects = GetSelectionFilterObjects(filter, client._id.ToString(), client.streamId.ToString());

    //  // TODO add account to client so it can be used here
    //  var apiClient = new Client(AccountManager.GetDefaultAccount());
    //  var convertedObjects = new List<Base>();
    //  var placeholders = new List<Base>();

    //  var units = CurrentDoc.Document.GetUnits().GetFormatOptions(UnitType.UT_Length).DisplayUnits.ToString().ToLowerInvariant().Replace("dut_", "");
    //  //InjectScaleInKits(GetScale(units)); // this is used for feet to sane units conversion.

    //  int i = 0;
    //  long currentBucketSize = 0;
    //  var errorMsg = "";
    //  var failedToConvert = 0;
    //  var errors = new List<SpeckleException>();
    //  foreach (var obj in objects)
    //  {
    //    NotifyUi("update-client", JsonConvert.SerializeObject(new
    //    {
    //      _id = (string)client._id,
    //      loading = true,
    //      isLoadingIndeterminate = false,
    //      loadingProgress = 1f * i++ / objects.Count() * 100,
    //      loadingBlurb = string.Format("Converting and uploading objects: {0} / {1}", i, objects.Count())
    //    }));

    //    var id = 0;
    //    Element revitElement = null;
    //    try
    //    {
    //      revitElement = CurrentDoc.Document.GetElement((string)obj.Properties["revitUniqueId"]);
    //      id = revitElement.Id.IntegerValue;
    //    }
    //    catch (Exception e)
    //    {
    //      errors.Add(new SpeckleException(message: "Could not retrieve element", inner: e));
    //      continue;
    //    }

    //    try
    //    {
    //      var conversionResult = SpeckleCore.Converter.Serialise(new List<object>() { revitElement });
    //      var byteCount = Converter.getBytes(conversionResult).Length;
    //      currentBucketSize += byteCount;

    //      if (byteCount > 2e6)
    //      {
    //        errors.Add(new SpeckleException(message: $"Element {id} is bigger than 2MB, it will be skipped"));
    //        continue;
    //      }

    //      convertedObjects.AddRange(conversionResult);

    //      if (currentBucketSize > 5e5 || i >= objects.Count()) // aim for roughly 500kb uncompressed
    //      {
    //        LocalContext.PruneExistingObjects(convertedObjects, apiClient.BaseUrl);

    //        try
    //        {
    //          var chunkResponse = apiClient.ObjectCreateAsync(convertedObjects).Result.Resources;
    //          int m = 0;
    //          foreach (var objConverted in convertedObjects)
    //          {
    //            objConverted._id = chunkResponse[m++]._id;
    //            placeholders.Add(new SpecklePlaceholder() { _id = objConverted._id });
    //            if (objConverted.Type != "Placeholder") LocalContext.AddSentObject(objConverted, apiClient.BaseUrl);
    //          }
    //        }
    //        catch (Exception e)
    //        {
    //          errors.Add(new SpeckleException(message: $"Failed to send {convertedObjects.Count} objects", inner: e));
    //        }
    //        currentBucketSize = 0;
    //        convertedObjects = new List<Base>(); // reset the chunkness
    //      }
    //    }
    //    catch (Exception e)
    //    {
    //      failedToConvert++;
    //      errors.Add(new SpeckleException(message: $"Failed to convert element {revitElement.Name} of id {id}", inner: e));
    //    }
    //  }

    //  if (errors.Any())
    //  {
    //    if (failedToConvert > 0)
    //      errorMsg += string.Format("Failed to convert {0} objects ",
    //        failedToConvert,
    //        failedToConvert == 1 ? "" : "s");
    //    else
    //      errorMsg += string.Format("There {0} {1} error{2} ",
    //        errors.Count() == 1 ? "is" : "are",
    //        errors.Count(),
    //        errors.Count() == 1 ? "" : "s");
    //  }

    //  var myStream = new Stream() { Objects = placeholders };

    //  var ug = UnitUtils.GetUnitGroup(UnitType.UT_Length);
    //  var baseProps = new Dictionary<string, object>();

    //  baseProps["units"] = units;

    //  baseProps["unitsDictionary"] = GetAndClearUnitDictionary();

    //  myStream.BaseProperties = baseProps;
    //  //myStream.BaseProperties =  JsonConvert.SerializeObject(baseProps);

    //  NotifyUi("update-client", JsonConvert.SerializeObject(new
    //  {
    //    _id = (string)client._id,
    //    loading = true,
    //    isLoadingIndeterminate = true,
    //    loadingBlurb = "Updating stream."
    //  }));

    //  var response = apiClient.StreamUpdateAsync((string)client.streamId, myStream).Result;

    //  var plural = objects.Count() == 1 ? "" : "s";
    //  NotifyUi("update-client", JsonConvert.SerializeObject(new
    //  {
    //    _id = (string)client._id,
    //    loading = false,
    //    loadingBlurb = "",
    //    message = $"Done sending {objects.Count()} object{plural}.",
    //    errorMsg,
    //    errors
    //  }));
    //}

    /// <summary>
    /// Pass selected element ids to UI
    /// </summary>
    /// <param name="args"></param>
    public override List<string> GetSelectedObjects()
    {
      var doc = CurrentDoc.Document;
      var selectedObjects = CurrentDoc != null ? CurrentDoc.Selection.GetElementIds().Select(id => doc.GetElement(id).UniqueId).ToList() : new List<string>();

      return  selectedObjects;
    }

    public override void RemoveSelectionFromClient(string args)
    {
      throw new NotImplementedException();
    }

    #region private methods

    private Type GetFilterType(string typeString)
    {
      Assembly ass = typeof(ISelectionFilter).Assembly;
      return ass.GetType(typeString);
    }

    /// <summary>
    /// Given the filter in use by a stream returns the document elements that match it
    /// </summary>
    /// <returns></returns>
    private IEnumerable<Base> GetSelectionFilterObjects(ISelectionFilter filter, string userId, string streamId)
    {
      var doc = CurrentDoc.Document;
      IEnumerable<Base> objects = new List<Base>();

      var selectionIds = new List<string>();

      if (filter.Name == "Selection")
      {
        var selFilter = filter as ElementsSelectionFilter;
        selectionIds = selFilter.Selection;
      }
      else if (filter.Name == "Category")
      {
        var catFilter = filter as ListSelectionFilter;
        var bics = new List<BuiltInCategory>();
        var categories = Globals.GetCategories(doc);
        IList<ElementFilter> elementFilters = new List<ElementFilter>();

        foreach (var cat in catFilter.Selection)
        {
          elementFilters.Add(new ElementCategoryFilter(categories[cat].Id));
        }
        LogicalOrFilter categoryFilter = new LogicalOrFilter(elementFilters);

        selectionIds = new FilteredElementCollector(doc)
          .WhereElementIsNotElementType()
          .WhereElementIsViewIndependent()
          .WherePasses(categoryFilter)
          .Select(x => x.UniqueId).ToList();

      }
      else if (filter.Name == "View")
      {
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
      }
      else if (filter.Name == "Parameter")
      {
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
              query = query.Where(fi => GetStringValue(fi.LookupParameter(propFilter.PropertyName)) == propFilter.PropertyValue);
              break;
            case "contains":
              query = query.Where(fi => GetStringValue(fi.LookupParameter(propFilter.PropertyName)).Contains(propFilter.PropertyValue));
              break;
            case "is greater than":
              query = query.Where(fi => UnitUtils.ConvertFromInternalUnits(
                fi.LookupParameter(propFilter.PropertyName).AsDouble(),
                fi.LookupParameter(propFilter.PropertyName).DisplayUnitType) > double.Parse(propFilter.PropertyValue));
              break;
            case "is less than":
              query = query.Where(fi => UnitUtils.ConvertFromInternalUnits(
                fi.LookupParameter(propFilter.PropertyName).AsDouble(),
                fi.LookupParameter(propFilter.PropertyName).DisplayUnitType) < double.Parse(propFilter.PropertyValue));
              break;
            default:
              break;
          }

          selectionIds = query.Select(x => x.UniqueId).ToList();

        }
        catch (Exception e)
        {
          Console.WriteLine(e);
        }
      }

      // LOCAL STATE management
      objects = selectionIds.Select(id =>
      {
        var temp = new Base();
        temp.applicationId = id;
        temp["__type"] = "Sent Object";
        return temp;
      });

      var myStream = LocalState.FirstOrDefault(st => st.id == streamId);


      var myStreamBox = StreamBoxesListWrapper.streamBoxes.FirstOrDefault(
        cl => (string)cl.accountId == (string)userId
        );
      myStreamBox.objects = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(objects));

      // Persist state and clients to revit file
      Queue.Add(new Action(() =>
      {
        using (Transaction t = new Transaction(CurrentDoc.Document, "Update local storage"))
        {
          t.Start();
          SpeckleStateManager.WriteState(CurrentDoc.Document, LocalState);
          StreamBoxStorageManager.WriteStreamBoxes(CurrentDoc.Document, StreamBoxesListWrapper);
          t.Commit();
        }
      }));
      Executor.Raise();
      var plural = objects.Count() == 1 ? "" : "s";

      if (objects.Count() != 0)
        NotifyUi( new RetrievedFilteredObjectsEvent()
        {
          Notification = $"You have added {objects.Count()} object{plural} to this stream.",
          UserId = userId,
          Objects = objects
        });

      return objects;
    }

    private string GetStringValue(Parameter p)
    {
      string value = "";
      if (!p.HasValue)
        return value;
      if (string.IsNullOrEmpty(p.AsValueString()) && string.IsNullOrEmpty(p.AsString()))
        return value;
      if (!string.IsNullOrEmpty(p.AsValueString()))
        return p.AsValueString().ToLowerInvariant();
      else
        return p.AsString().ToLowerInvariant();
    }
    #endregion
  }
}