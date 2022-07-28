using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;
using Speckle.ConnectorTeklaStructures.Util;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DesktopUI2.Views.Windows.Dialogs;
using Newtonsoft.Json;
using static DesktopUI2.ViewModels.MappingViewModel;


namespace Speckle.ConnectorTeklaStructures.UI
{
  public partial class ConnectorBindingsTeklaStructures : ConnectorBindings

  {

    const string noMapping = "Never (default)";
    const string everyReceive = "Always";
    #region receiving
    public override async Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
    {
      Exceptions.Clear();

      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(ConnectorTeklaStructuresUtils.TeklaStructuresAppName);
      converter.SetContextDocument(Model);
      //var previouslyRecieveObjects = state.ReceivedObjects;

      var settings = new Dictionary<string, string>();
      CurrentSettings = state.Settings;
      foreach (var setting in state.Settings)
        settings.Add(setting.Slug, setting.Selection);
      converter.SetConverterSettings(settings);

      if (converter == null)
      {
        throw new Exception("Could not find any Kit!");
        //RaiseNotification($"Could not find any Kit!");
        progress.CancellationTokenSource.Cancel();
        //return null;
      }


      var stream = await state.Client.StreamGet(state.StreamId);

      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
      {
        return null;
      }

      var transport = new ServerTransport(state.Client.Account, state.StreamId);

      Exceptions.Clear();

      Commit commit = null;
      if (state.CommitId == "latest")
      {
        var res = await state.Client.BranchGet(progress.CancellationTokenSource.Token, state.StreamId, state.BranchName, 1);
        commit = res.commits.items.FirstOrDefault();
      }
      else
      {
        commit = await state.Client.CommitGet(progress.CancellationTokenSource.Token, state.StreamId, state.CommitId);
      }
      string referencedObject = commit.referencedObject;

      var commitObject = await Operations.Receive(
                referencedObject,
                progress.CancellationTokenSource.Token,
                transport,
                onProgressAction: dict => progress.Update(dict),
                onErrorAction: (Action<string, Exception>)((s, e) =>
                {
                  progress.Report.LogOperationError(e);
                  progress.CancellationTokenSource.Cancel();
                }),
                //onTotalChildrenCountKnown: count => Execute.PostToUIThread(() => state.Progress.Maximum = count),
                disposeTransports: true
                );

      if (progress.Report.OperationErrorsCount != 0)
      {
        return state;
      }

      try
      {
        await state.Client.CommitReceived(new CommitReceivedInput
        {
          streamId = stream?.id,
          commitId = commit?.id,
          message = commit?.message,
          sourceApplication = ConnectorTeklaStructuresUtils.TeklaStructuresAppName
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

      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 0;
      //Execute.PostToUIThread(() => state.Progress.Maximum = state.SelectedObjectIds.Count());

      Action updateProgressAction = () =>
      {
        conversionProgressDict["Conversion"]++;
        progress.Update(conversionProgressDict);
      };


      var commitObjs = FlattenCommitObject(commitObject, converter);
      foreach (var commitObj in commitObjs)
      {
        BakeObject(commitObj, state, converter);
        updateProgressAction?.Invoke();
      }


      Model.CommitChanges();
      try
      {
        //await state.RefreshStream();
        WriteStateToFile();
      }
      catch (Exception e)
      {
        progress.Report.LogOperationError(e);
        WriteStateToFile();
        //state.Errors.Add(e);
        //Globals.Notify($"Receiving done, but failed to update stream from server.\n{e.Message}");
      }
      progress.Report.Merge(converter.Report);
      return state;
    }






    /// <summary>
    /// conversion to native
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="state"></param>
    /// <param name="converter"></param>
    private void BakeObject(Base obj, StreamState state, ISpeckleConverter converter)
    {
      try
      {
        converter.ConvertToNative(obj);
      }
      catch (Exception e)
      {
        var exception = new Exception($"Failed to convert object {obj.id} of type {obj.speckle_type}\n with error\n{e}");
        converter.Report.LogOperationError(exception);
        return;
      }
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

      return objects;
    }

    #endregion

    #region mapping

    public async Task UpdateForCustomMapping(StreamState state, ProgressViewModel progress, List<Base> flattenedBase)
    {
      // Get Settings for recieve on mapping 
      var receiveMappingsModelsSetting = (CurrentSettings.FirstOrDefault(x => x.Slug == "recieve-mappings") as MappingSeting);
      var receiveMappings = receiveMappingsModelsSetting != null ? receiveMappingsModelsSetting.Selection : "";

      Dictionary<string, List<MappingValue>> settingsMapping = null;
      bool isFirstTimeReceiving = false;

      if (receiveMappingsModelsSetting.MappingJson != null)
        settingsMapping = JsonConvert.DeserializeObject<Dictionary<string, List<MappingValue>>>(receiveMappingsModelsSetting.MappingJson);
      else
        isFirstTimeReceiving = true;

      if (receiveMappings == noMapping || receiveMappings == null)
        return;
      else
      {
        Dictionary<string, List<string>> hostTypesDict = GetHostTypes();

        bool newTypesExist = UpdateMappingForNewObjects(settingsMapping, flattenedBase, hostTypesDict);
        Dictionary<string, List<MappingValue>> Mapping = settingsMapping;
        if (Mapping == null)
          Mapping = GetInitialMapping(flattenedBase, progress, hostTypesDict);

        try
        {
          // show custom mapping dialog if the settings corrospond to what is being received
          if (newTypesExist || receiveMappings == everyReceive)
          {

            var vm = new MappingViewModel(Mapping, hostTypesDict, progress, newTypesExist && !isFirstTimeReceiving);
            MappingViewDialog mappingView = new MappingViewDialog
            {
              DataContext = vm
            };

            Mapping = await mappingView.ShowDialog<Dictionary<string, List<MappingValue>>>();

            receiveMappingsModelsSetting.MappingJson = JsonConvert.SerializeObject(Mapping); ;

          }
          updateRecieveObject(Mapping, flattenedBase);
        }
        catch (Exception ex)
        {
          progress.Report.Log($"Could not make new mapping {ex}");
        }
      }
    }

    public bool UpdateMappingForNewObjects(Dictionary<string, List<MappingValue>> settingsMapping, List<Base> flattenedBase, Dictionary<string, List<string>> hostTypesDict)
    {
      // no existing mappings exist
      if (settingsMapping == null)
        return true;

      bool newTypesExist = false;
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
          newTypesExist = true;
          string category = GetTypeCategory(obj);
          string mappedValue = GetMappedValue(hostTypesDict, category, type);
          if (settingsMapping.ContainsKey(category))
            settingsMapping[category].Add(new MappingValue(type, mappedValue, true));
          else
            settingsMapping[category] = new List<MappingValue> { new MappingValue(type, mappedValue, true) };
        }
      }
      return newTypesExist;
    }

    public Dictionary<string, List<MappingValue>> GetInitialMapping(List<Base> flattenedBase, ProgressViewModel progress, Dictionary<string, List<string>> hostProperties)
    {
      var flattenedBaseTypes = GetFlattenedBaseTypes(flattenedBase, progress);

      var mappings = returnFirstPassMap(flattenedBaseTypes, hostProperties, progress);

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

    public void updateRecieveObject(Dictionary<string, List<MappingValue>> Map, List<Base> objects)
    {
      foreach (var @object in objects)
      {
        try
        {
          //currently implemented only for Revit objects ~ object models need a bit of refactor for this to be a cleaner code
          var propInfo = @object.GetType().GetProperty("type").GetValue(@object) as string;
          string typeCategory = GetTypeCategory(@object);

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
    }

    public Dictionary<string, List<string>> GetHostTypes()
    {
      // WARNING : The keys in this dictionary must match those found in GetFlattenedBaseTypes
      var returnDict = new Dictionary<string, List<string>>();
      
      return returnDict;
    }

    private Dictionary<string, List<string>> GetFlattenedBaseTypes(List<Base> objects, ProgressViewModel progress)
    {
      var returnDict = new Dictionary<string, List<string>>();

      foreach (var @object in objects)
      {
        try
        {
          //currently implemented only for Revit objects ~ object models need a bit of refactor for this to be a cleaner code
          var type = @object.GetType().GetProperty("type").GetValue(@object) as string;
          string typeCategory = GetTypeCategory(@object);


          if (returnDict.ContainsKey(typeCategory))
          {
            returnDict[typeCategory].Add(type);
          }
          else
          {
            returnDict[typeCategory] = new List<string> { type };
          }

          // try to get the material
          if (@object["materialQuantites"] is List<Base> mats)
          {
            foreach (var mat in mats)
            {
              if (mat["material"] is Base b)
              {
                if (b["name"] is string matName)
                {
                  if (returnDict.ContainsKey("Materials"))
                  {
                    returnDict["Materials"].Add(matName);
                  }
                  else
                  {
                    returnDict["Materials"] = new List<string> { matName };
                  }
                }
              }
            }
          }
        }
        catch
        {

        }
      }

      var newDictionary = returnDict.ToDictionary(entry => entry.Key, entry => entry.Value);
      foreach (var key in newDictionary.Keys)
      {
        progress.Report.Log($"key {key} value {String.Join(",", returnDict[key])}");
        returnDict[key] = returnDict[key].Distinct().ToList();
      }
      return returnDict;
    }

    public string GetTypeCategory(Base obj)
    {
      // WARNING : The keys in this dictionary must match those found in GetHostTypes
      string speckleType;
      try
      {
        speckleType = obj.speckle_type.Split(':')[0];
      }
      catch
      {
        speckleType = obj.speckle_type;
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
      return typeCategory;
    }

    private List<ApplicationPlaceholderObject> ConvertReceivedObjects(List<Base> objects, ISpeckleConverter converter, StreamState state, ProgressViewModel progress)
    {
      var placeholders = new List<ApplicationPlaceholderObject>();
      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 1;

      // Get setting to skip linked model elements if necessary
      var receiveLinkedModelsSetting = (CurrentSettings.FirstOrDefault(x => x.Slug == "linkedmodels-receive") as CheckBoxSetting);
      var receiveLinkedModels = receiveLinkedModelsSetting != null ? receiveLinkedModelsSetting.IsChecked : false;

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

#endregion
  }
}
