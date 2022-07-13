using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using ConnectorRevit.Revit;
using DesktopUI2.Models;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;
using DesktopUI2.Views;
using DesktopUI2.Views.Windows.Dialogs;
using Revit.Async;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace Speckle.ConnectorRevit.UI
{
  public partial class ConnectorBindingsRevit2
  {
    /// <summary>
    /// Receives a stream and bakes into the existing revit file.
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    /// 



    public override async Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
    {
      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(ConnectorRevitUtils.RevitAppName);
      converter.SetContextDocument(CurrentDoc.Document);
      var previouslyReceiveObjects = state.ReceivedObjects;

      // set converter settings as tuples (setting slug, setting selection)
      var settings = new Dictionary<string, string>();
      CurrentSettings = state.Settings;
      foreach (var setting in state.Settings)
        settings.Add(setting.Slug, setting.Selection);
      converter.SetConverterSettings(settings);

      var transport = new ServerTransport(state.Client.Account, state.StreamId);

      var stream = await state.Client.StreamGet(state.StreamId);

      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
      {
        return null;
      }

      Commit myCommit = null;
      //if "latest", always make sure we get the latest commit when the user clicks "receive"
      if (state.CommitId == "latest")
      {
        var res = await state.Client.BranchGet(progress.CancellationTokenSource.Token, state.StreamId, state.BranchName, 1);
        myCommit = res.commits.items.FirstOrDefault();
      }
      else
      {
        myCommit = await state.Client.CommitGet(progress.CancellationTokenSource.Token, state.StreamId, state.CommitId);
      }
      string referencedObject = myCommit.referencedObject;

      var commitObject = await Operations.Receive(
          referencedObject,
          progress.CancellationTokenSource.Token,
          transport,
          onProgressAction: dict => progress.Update(dict),
          onErrorAction: (s, e) =>
          {
            progress.Report.LogOperationError(e);
            progress.CancellationTokenSource.Cancel();
          },
          onTotalChildrenCountKnown: count => { progress.Max = count; },
          disposeTransports: true
          );

      try
      {
        await state.Client.CommitReceived(new CommitReceivedInput
        {
          streamId = stream?.id,
          commitId = myCommit?.id,
          message = myCommit?.message,
          sourceApplication = ConnectorRevitUtils.RevitAppName
        });
      }
      catch
      {
        // Do nothing!
      }

      if (progress.Report.OperationErrorsCount != 0)
      {
        return state;
      }

      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
      {
        return null;
      }



      await RevitTask.RunAsync(app =>
      {
        using (var t = new Transaction(CurrentDoc.Document, $"Baking stream {state.StreamId}"))
        {
          var failOpts = t.GetFailureHandlingOptions();
          failOpts.SetFailuresPreprocessor(new ErrorEater(converter));
          failOpts.SetClearAfterRollback(true);
          t.SetFailureHandlingOptions(failOpts);

          t.Start();
          var flattenedObjects = FlattenCommitObject(commitObject, converter);
          converter.ReceiveMode = state.ReceiveMode;
          // needs to be set for editing to work 
          converter.SetPreviousContextObjects(previouslyReceiveObjects);
          // needs to be set for openings in floors and roofs to work
          converter.SetContextObjects(flattenedObjects.Select(x => new ApplicationPlaceholderObject { applicationId = x.applicationId, NativeObject = x }).ToList());
          var newPlaceholderObjects = ConvertReceivedObjects(flattenedObjects, converter, state, progress);
          // receive was cancelled by user
          if (newPlaceholderObjects == null)
          {
            progress.Report.LogConversionError(new Exception("fatal error: receive cancelled by user"));
            t.RollBack();
            return;
          }

          if (state.ReceiveMode == ReceiveMode.Update)
            DeleteObjects(previouslyReceiveObjects, newPlaceholderObjects);

          state.ReceivedObjects = newPlaceholderObjects;

          t.Commit();
          progress.Report.Merge(converter.Report);
        }

      });

      if (converter.Report.ConversionErrors.Any(x => x.Message.Contains("fatal error")))
      {
        // the commit is being rolled back
        return null;
      }

      return state;
    }

    //delete previously sent object that are no more in this stream
    private void DeleteObjects(List<ApplicationPlaceholderObject> previouslyReceiveObjects, List<ApplicationPlaceholderObject> newPlaceholderObjects)
    {
      foreach (var obj in previouslyReceiveObjects)
      {
        if (newPlaceholderObjects.Any(x => x.applicationId == obj.applicationId))
          continue;

        var element = CurrentDoc.Document.GetElement(obj.ApplicationGeneratedId);
        if (element != null)
        {
          CurrentDoc.Document.Delete(element.Id);
        }
      }
    }
    //private List<string> GetListProperties(List<Base> objects)
    //{
    //  List<string> listProperties = new List<string> { };
    //  foreach (var @object in objects)
    //  {
    //    try
    //    {
    //      //currently implemented only for Revit objects ~ object models need a bit of refactor for this to be a cleaner code
    //      var propInfo = @object.GetType().GetProperty("type").GetValue(@object) as string;
    //      listProperties.Add(propInfo);
    //    }
    //    catch
    //    {

    //    }

    //  }
    //  return listProperties.Distinct().ToList();
    //}

    //private List<string> GetHostDocumentPropeties(Document doc)
    //{
    //  var list = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol));
    //  List<string> familyType = list.Select(o => o.Name).Distinct().ToList();
    //  return familyType;
    //}

    //public static int LevenshteinDistance(string s, string t)
    //{
    //  // Default algorithim for computing the similarity between strings
    //  int n = s.Length;
    //  int m = t.Length;
    //  int[,] d = new int[n + 1, m + 1];
    //  if (n == 0)
    //  {
    //    return m;
    //  }
    //  if (m == 0)
    //  {
    //    return n;
    //  }
    //  for (int i = 0; i <= n; d[i, 0] = i++)
    //    ;
    //  for (int j = 0; j <= m; d[0, j] = j++)
    //    ;
    //  for (int i = 1; i <= n; i++)
    //  {
    //    for (int j = 1; j <= m; j++)
    //    {
    //      int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
    //      d[i, j] = Math.Min(
    //          Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
    //          d[i - 1, j - 1] + cost);
    //    }
    //  }
    //  return d[n, m];
    //}

    //public Dictionary<string, string> returnFirstPassMap(List<string> specklePropertyList, List<string> hostPropertyList)
    //{
    //  var mappings = new Dictionary<string, string> { };
    //  foreach (var item in specklePropertyList)
    //  {
    //    List<int> listVert = new List<int> { };
    //    foreach (var hostItem in hostPropertyList)
    //    {
    //      listVert.Add(LevenshteinDistance(item, hostItem));
    //    }
    //    var indexMin = listVert.IndexOf(listVert.Min());
    //    mappings.Add(item, hostPropertyList[indexMin]);
    //  }
    //  return mappings;
    //}

    //public void updateRecieveObject(Dictionary<string, string> Map, List<Base> objects)
    //{
    //  foreach (var @object in objects)
    //  {

    //    try
    //    {
    //      //currently implemented only for Revit objects ~ object models need a bit of refactor for this to be a cleaner code
    //      var propInfo = "";
    //      propInfo = @object.GetType().GetProperty("type").GetValue(@object) as string;
    //      if(propInfo != ""){
    //        string mappingProperty = "";
    //        Map.TryGetValue(propInfo, out mappingProperty);
    //        var prop = @object.GetType().GetProperty("type");
    //        prop.SetValue(@object, mappingProperty);
    //      }
    //    }
    //    catch{

    //    }
    //   }
    //}
    private List<ApplicationPlaceholderObject> ConvertReceivedObjects(List<Base> objects, ISpeckleConverter converter, StreamState state, ProgressViewModel progress)
    {
      var placeholders = new List<ApplicationPlaceholderObject>();
      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 1;

      // Get setting to skip linked model elements if necessary
      var receiveLinkedModelsSetting = (CurrentSettings.FirstOrDefault(x => x.Slug == "linkedmodels-receive") as CheckBoxSetting);
      var receiveLinkedModels = receiveLinkedModelsSetting != null ? receiveLinkedModelsSetting.IsChecked : false;

      // Get Settings for recieve on mapping 
      var receiveMappingsModelsSetting = (CurrentSettings.FirstOrDefault(x => x.Slug == "recieve-mappings") as CheckBoxSetting);
      var receiveMappings = receiveMappingsModelsSetting != null ? receiveMappingsModelsSetting.IsChecked : false;

      if (receiveMappings == true)
      {
        var listProperties = GetListProperties(objects);
        var listHostProperties = GetHostDocumentPropeties(CurrentDoc.Document);
        var mappings = returnFirstPassMap(listProperties, listHostProperties);
        //User to update logic from computer here;

        //var vm = new MappingViewModel(mappings);
        //var mappingView = new MappingView
        //{
        //  DataContext = vm
        //};

        //mappingView.ShowDialog(MainWindow.Instance);
        //vm.OnRequestClose += (s, e) => mappingView.Close();
        //var newMappings = await mappingView.ShowDialog<Dictionary<string, string>?>(MainWindow.Instance);
        //System.Diagnostics.Debug.WriteLine($"new mappings {newMappings}");

        updateRecieveObject(mappings, objects); 

      }

      foreach (var @base in objects)
      {
        if (progress.CancellationTokenSource.Token.IsCancellationRequested)
        {
          placeholders = null;
          break;
        }

        try
        {
          conversionProgressDict["Conversion"]++;
          progress.Update(conversionProgressDict);

          //skip element if is froma  linked file and setting is off
          if (!receiveLinkedModels && @base["isRevitLinkedModel"] != null && bool.Parse(@base["isRevitLinkedModel"].ToString()))
            continue;

          var convRes = converter.ConvertToNative(@base);
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
          progress.Report.LogConversionError(e);
        }
      }

      return placeholders;
    }

    //public ICommand UserMappingCommand = ReactiveCommand.CreateFromTask(async () =>
    //{
    //  var result = await mappingView.ShowDialog();
    //});

    //private async Dictionary<string,string> OpenMapping(Dictionary<string, string> mapping)
    //{
    //  MainViewModel.RouterInstance.Navigate.Execute(new MappingViewModel(mapping));
      
    //  return mapping;
    //}

    /// <summary>
    /// Recurses through the commit object and flattens it. 
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="converter"></param>
    /// <returns></returns>
    private List<Base> FlattenCommitObject(object obj, ISpeckleConverter converter)
    {
      List<Base> objects = new List<Base>();

      if (obj is Base @base)
      {
        if (converter.CanConvertToNative(@base))
        {
          objects.Add(@base);

          return objects;
        }
        else
        {
          foreach (var prop in @base.GetDynamicMembers())
          {
            objects.AddRange(FlattenCommitObject(@base[prop], converter));
          }
          return objects;
        }
      }

      if (obj is List<object> list)
      {
        foreach (var listObj in list)
        {
          objects.AddRange(FlattenCommitObject(listObj, converter));
        }
        return objects;
      }

      if (obj is IDictionary dict)
      {
        foreach (DictionaryEntry kvp in dict)
        {
          objects.AddRange(FlattenCommitObject(kvp.Value, converter));
        }
        return objects;
      }

      else
      {
        if (obj != null && !obj.GetType().IsPrimitive && !(obj is string))
          converter.Report.Log($"Skipped object of type {obj.GetType()}, not supported.");
      }

      return objects;
    }
  }
}
