using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ConnectorRevit.Revit;
using DesktopUI2.Models;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;
using DesktopUI2.Views.Windows.Dialogs;
using Newtonsoft.Json;
using Revit.Async;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using static DesktopUI2.ViewModels.MappingViewModel;

namespace Speckle.ConnectorRevit.UI
{
  [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
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
      progress.Report.Log($"start recieve");
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

      var flattenedObjects = FlattenCommitObject(commitObject, converter);
      converter.ReceiveMode = state.ReceiveMode;
      // needs to be set for editing to work 
      converter.SetPreviousContextObjects(previouslyReceiveObjects);
      // needs to be set for openings in floors and roofs to work
      converter.SetContextObjects(flattenedObjects.Select(x => new ApplicationPlaceholderObject { applicationId = x.applicationId, NativeObject = x }).ToList());

      // update flattened objects if the user has custom mappings
      progress.Report.Log($"flattedObjects {flattenedObjects}");

      try
      {
        await RevitTask.RunAsync(() => UpdateForCustomMapping(state, progress, flattenedObjects));
      }
      catch (Exception ex)
      {
        progress.Report.Log($" new mappings failed :( {ex}");
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

          progress.Report.Log($"commiting");
          t.Commit();
          progress.Report.Log($"commited {t.HasEnded()} {t.GetStatus()}");
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

    public async Task UpdateForCustomMapping(StreamState state, ProgressViewModel progress, List<Base> flattenedBase)
    {
      // Get Settings for recieve on mapping 
      var receiveMappingsModelsSetting = (CurrentSettings.FirstOrDefault(x => x.Slug == "recieve-mappings") as MappingSeting);
      var receiveMappings = receiveMappingsModelsSetting != null ? receiveMappingsModelsSetting.Selection : "";

      Dictionary<string, List<MappingValue>> settingsMapping = null;
      if (receiveMappingsModelsSetting.MappingJson != null)
        settingsMapping = JsonConvert.DeserializeObject<Dictionary<string, List<MappingValue>>>(receiveMappingsModelsSetting.MappingJson);

      if (receiveMappings == noMapping)
        return;
      else if (receiveMappings == everyReceive)
      {
        Dictionary<string, List<string>> hostTypesDict = GetHostTypes();

        UpdateMappingForNewObjects(settingsMapping, flattenedBase, hostTypesDict);
        Dictionary<string, List<MappingValue>> initialMapping = settingsMapping;
        if (initialMapping == null)
          initialMapping = GetInitialMapping(flattenedBase, progress, hostTypesDict);


        try
        {
          var vm = new MappingViewModel(initialMapping, hostTypesDict, progress);
          MappingViewDialog mappingView = new MappingViewDialog
          {
            DataContext = vm
          };

          Dictionary<string, List<MappingValue>> newMapping = await mappingView.ShowDialog<Dictionary<string, List<MappingValue>>>();

          //var newMapping = await mappingView.ShowDialog<Dictionary<string,MappingValue>>();

          receiveMappingsModelsSetting.MappingJson = JsonConvert.SerializeObject(newMapping);
          progress.Report.Log($"newmapping serialized {receiveMappingsModelsSetting.MappingJson}");

          List<Base> newFlatBase = updateRecieveObject(newMapping, flattenedBase);

          //progress.Report.Log($"newflatbase {newFlatBase}");
          return;
        }
        catch (Exception ex)
        {
          progress.Report.Log($"Could not make new mapping {ex}");
        }

        progress.Report.Log($"how did this work?");
        return;
      }
      //else if (receiveMappings == forNewTypes)
      //{

      //}
    }

    public void UpdateMappingForNewObjects(Dictionary<string, List<MappingValue>> settingsMapping, List<Base> flattenedBase, Dictionary<string, List<string>> hostTypesDict)
    {
      if (settingsMapping == null)
        return;

      List<Base> objectsWithNewTypes = new List<Base>();
      var loopValues = settingsMapping.Values;
      foreach (var obj in flattenedBase)
      {
        var type = obj.GetType().GetProperty("type").GetValue(obj) as string;
        bool containsObj = false;
        foreach (var mapValueList in loopValues)
        {
          if (mapValueList.Any(i => i.IncomingType == type))
          {
            containsObj = true;
            break;
          }
        }
        if (!containsObj)
        {
          string category = GetTypeCategory(obj);
          string mappedValue = GetMappedValue(hostTypesDict, category, type);
          settingsMapping[category].Add(new MappingValue(type, mappedValue, inNewType: true));
        }
      } 
    }

    public Dictionary<string, List<MappingValue>> GetInitialMapping(List<Base> flattenedBase, ProgressViewModel progress, Dictionary<string, List<string>> hostProperties)
    {
      var listProperties = GetListProperties(flattenedBase, progress);

      var mappings = returnFirstPassMap(listProperties, hostProperties, progress);

      return mappings;
    }

    public Dictionary<string, List<MappingValue>> returnFirstPassMap(Dictionary<string, List<string>> specklePropertyDict, Dictionary<string, List<string>> hostPropertyList, ProgressViewModel progress)
    {
      progress.Report.Log($"firstPassMap");
      var mappings = new Dictionary<string, List<MappingValue>> { };
      foreach (var category in specklePropertyDict.Keys)
      {
        progress.Report.Log($"cat {category}");
        foreach (var speckleType in specklePropertyDict[category])
        {
          string mappedValue = GetMappedValue(hostPropertyList, category, speckleType);

          if (mappings.ContainsKey(category))
          {
            mappings[category].Add(new MappingValue
              (
                speckleType, mappedValue
              ));
          }
          else
          {
            mappings[category] = new List<MappingValue>
              {
                new MappingValue
                (
                  speckleType, mappedValue
                )
              };
          }
        }
      }
      return mappings;
    }

    public string GetMappedValue(Dictionary<string, List<string>> hostPropertyList, string category, string speckleType)
    {
      string mappedValue = "";
      List<int> listVert = new List<int> { };

      // if this count is zero, then there aren't any types of this category loaded into the project
      if (hostPropertyList[category].Count != 0)
      {
        foreach (var revitType in hostPropertyList[category])
        {
          listVert.Add(LevenshteinDistance(speckleType, revitType));
        }
        mappedValue = hostPropertyList[category][listVert.IndexOf(listVert.Min())];
      }

      return mappedValue;
    }

    public static int LevenshteinDistance(string s, string t)
    {
      // Default algorithim for computing the similarity between strings
      int n = s.Length;
      int m = t.Length;
      int[,] d = new int[n + 1, m + 1];
      if (n == 0)
      {
        return m;
      }
      if (m == 0)
      {
        return n;
      }
      for (int i = 0; i <= n; d[i, 0] = i++)
        ;
      for (int j = 0; j <= m; d[0, j] = j++)
        ;
      for (int i = 1; i <= n; i++)
      {
        for (int j = 1; j <= m; j++)
        {
          int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
          d[i, j] = Math.Min(
              Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
              d[i - 1, j - 1] + cost);
        }
      }
      return d[n, m];
    }

    public List<Base> updateRecieveObject(Dictionary<string, List<MappingValue>> Map, List<Base> objects)
    {
      foreach (var @object in objects)
      {
        try
        {
          //currently implemented only for Revit objects ~ object models need a bit of refactor for this to be a cleaner code
          var propInfo = "";
          string speckleType = "";
          try
          {
            speckleType = @object.speckle_type.Split(':')[0];
          }
          catch
          {
            speckleType = @object.speckle_type;
          }

          string typeCategory = "";

          switch (speckleType)
          {
            case "Objects.BuiltElements.Floor":
              typeCategory = "Floors";
              break;
            case "Objects.BuiltElements.Wall":
              typeCategory = "Walls";
              break;
            case "Objects.BuiltElements.Beam":
              typeCategory = "Framing";
              break;
            case "Objects.BuiltElements.Column":
              typeCategory = "Columns";
              break;
            default:
              typeCategory = "Miscellaneous";
              break;
          }
          propInfo = @object.GetType().GetProperty("type").GetValue(@object) as string;
          if (propInfo != "")
          {
            string mappingProperty = "";
            MappingValue mappingWithMatchingType = Map[typeCategory].Where(i => i.IncomingType == propInfo).First();
            mappingProperty = mappingWithMatchingType.OutgoingType ?? mappingWithMatchingType.InitialGuess;
            var prop = @object.GetType().GetProperty("type");
            prop.SetValue(@object, mappingProperty);
          }
        }
        catch
        {

        }
      }
      return objects;
    }

    private List<ApplicationPlaceholderObject> ConvertReceivedObjects(List<Base> objects, ISpeckleConverter converter, StreamState state, ProgressViewModel progress)
    {
      var placeholders = new List<ApplicationPlaceholderObject>();
      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 1;

      // Get setting to skip linked model elements if necessary
      var receiveLinkedModelsSetting = (CurrentSettings.FirstOrDefault(x => x.Slug == "linkedmodels-receive") as CheckBoxSetting);
      var receiveLinkedModels = receiveLinkedModelsSetting != null ? receiveLinkedModelsSetting.IsChecked : false;

      // Get Settings for recieve on mapping 
      //var receiveMappingsModelsSetting = (CurrentSettings.FirstOrDefault(x => x.Slug == "recieve-mappings") as CheckBoxSetting);
      //var receiveMappings = receiveMappingsModelsSetting != null ? receiveMappingsModelsSetting.IsChecked : false;

      //try
      //{
      //  var vm = new MappingViewModel();
      //  var mappingView = new MappingViewDialog
      //  {
      //    DataContext = vm
      //  };


      //  await mappingView.ShowDialog();
      //  //var dialog = new MappingViewDialog();
      //  //await dialog.ShowDialog();
      //}
      //catch (Exception ex)
      //{
      //  progress.Report.Log($"Could show dialog {ex}");
      //}

      //try
      //{

      //  var dialog = new MappingViewDialog();
      //  var result = await dialog.ShowDialog<string>();
      //  ButtonSetting mappingSetting = (CurrentSettings.FirstOrDefault(x => x.Slug == "mapping") as ButtonSetting);
      //  var mappingDict = JsonConvert.DeserializeObject<Dictionary<string, List<MappingValue>>>(mappingSetting.JsonSelection);
      //  updateRecieveObject(mappingDict, objects);
      //}
      //catch (Exception ex)
      //{
      //  progress.Report.Log($"Could not update objects with user-defined type mapping {ex}");
      //}

      //try
      //{
      //  //var mappingDict = JsonConvert.DeserializeObject(mappingSetting.JsonSelection) as Dictionary<string, string>;
      //  progress.Report.Log($"mappingDict {mappingSetting.JsonSelection}");
      //}
      //catch
      //{

      //}

      //try
      //{
      //  var mappingDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(mappingSetting.JsonSelection);
      //  progress.Report.Log($"mappingDict {mappingDict}");
      //}
      //catch
      //{

      //}

      //try
      //{
      //  var mappingDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(mappingSetting.JsonSelection);
      //  foreach (var item in mappingDict)
      //  {
      //    progress.Report.Log($"DESERIALIZED DICT {item.Key} {item.Value}");
      //  }
      //}
      //catch
      //{

      //}

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

    public override async Task<Dictionary<string, List<MappingValue>>> ImportFamily(Dictionary<string, List<MappingValue>> Mapping)
    {
      FileOpenDialog dialog = new FileOpenDialog("Revit Families (*.rfa)|*.rfa");
      dialog.ShowPreview = true;
      var result = dialog.Show();

      if (result == ItemSelectionDialogResult.Canceled)
      {
        return Mapping;
      }

      string path = "";
      path = ModelPathUtils.ConvertModelPathToUserVisiblePath(dialog.GetSelectedModelPath());

      return await RevitTask.RunAsync(app =>
      {
        using (var t = new Transaction(CurrentDoc.Document, $"Importing family symbols"))
        {
          t.Start();
          bool symbolLoaded = false;

          foreach (var category in Mapping.Keys)
          {
            foreach (var mappingValue in Mapping[category])
            {
              if (!mappingValue.Imported)
              {
                bool successfullyImported = CurrentDoc.Document.LoadFamilySymbol(path, mappingValue.IncomingType);

                if (successfullyImported)
                {
                  mappingValue.Imported = true;
                  mappingValue.OutgoingType = mappingValue.IncomingType;
                  symbolLoaded = true;
                }
              }
            }
          }

          if (symbolLoaded)
            t.Commit();
          else
            t.RollBack();
          return Mapping;
        }
      });
    }
  }
}
