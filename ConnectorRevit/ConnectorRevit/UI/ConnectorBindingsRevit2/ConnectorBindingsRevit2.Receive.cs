using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
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
        await RevitTask.RunAsync(() => UpdateForCustomMapping(state, progress, flattenedObjects, myCommit.sourceApplication));
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

    public async Task UpdateForCustomMapping(StreamState state, ProgressViewModel progress, List<Base> flattenedBase, string sourceApp)
    {
      progress.Report.Log($"UpdateForCustomMapping");
      // Get Settings for recieve on mapping 
      var receiveMappingsModelsSetting = (CurrentSettings.FirstOrDefault(x => x.Slug == "receive-mappings") as MappingSeting);
      var receiveMappings = receiveMappingsModelsSetting != null ? receiveMappingsModelsSetting.Selection : "";

      Dictionary<string, List<MappingValue>> settingsMapping = null;
      bool isFirstTimeReceiving = false;

      if (receiveMappingsModelsSetting.MappingJson != null)
        settingsMapping = JsonConvert.DeserializeObject<Dictionary<string, List<MappingValue>>>(receiveMappingsModelsSetting.MappingJson);
      else
        isFirstTimeReceiving = true;

      progress.Report.Log($"receiveMapping {receiveMappings}");

      if (receiveMappings == noMapping || receiveMappings == null)
        return;
      else
      {
        progress.Report.Log($"GetHostTypes");
        Dictionary<string, List<string>> hostTypesDict = GetHostTypes();
        Dictionary<string, List<string>> incomingTypesDict = GetIncomingTypes(flattenedBase, progress, sourceApp);

        progress.Report.Log($"UpdateMappingForNewObjects");
        bool newTypesExist = UpdateExistingMapping(settingsMapping, hostTypesDict, incomingTypesDict, progress);
        //bool newTypesExist = UpdateMappingForNewObjects(settingsMapping, flattenedBase, hostTypesDict, progress);
        progress.Report.Log($"GetInitialMapping");
        Dictionary<string, List<MappingValue>> Mapping = settingsMapping;
        if (Mapping == null)
        {
          progress.Report.Log($"Mapping null");
          Mapping = returnFirstPassMap(incomingTypesDict, hostTypesDict, progress);
        }

        progress.Report.Log($"InitialMapping");
        try
        {
          // show custom mapping dialog if the settings corrospond to what is being received
          if (newTypesExist || receiveMappings == everyReceive)
          {
            progress.Report.Log($"show dialog");
            var vm = new MappingViewModel(Mapping, hostTypesDict, progress, newTypesExist && !isFirstTimeReceiving);
            MappingViewDialog mappingView = new MappingViewDialog
            {
              DataContext = vm
            };

            Mapping = await mappingView.ShowDialog<Dictionary<string, List<MappingValue>>>();

            receiveMappingsModelsSetting.MappingJson = JsonConvert.SerializeObject(Mapping); ;

          }
          //updateRecieveObject(Mapping, flattenedBase);
          SetMappedValues(Mapping, flattenedBase, progress, sourceApp);
        }
        catch (Exception ex)
        {
          progress.Report.Log($"Could not make new mapping {ex}");
        }
        progress.Report.Log($"new mapping success");
      }
    }

    public Dictionary<string, List<MappingValue>> returnFirstPassMap(Dictionary<string, List<string>> flattenedBaseTypes, Dictionary<string, List<string>> hostTypes, ProgressViewModel progress)
    {
      var mappings = new Dictionary<string, List<MappingValue>> { };
      progress.Report.Log($"flattenedBaseTypes.Keys {string.Join(",", flattenedBaseTypes.Keys)}");
      foreach (var incomingTypeCategory in flattenedBaseTypes.Keys)
      {
        progress.Report.Log($"incomingTypeCategory {incomingTypeCategory}");
        foreach (var speckleType in flattenedBaseTypes[incomingTypeCategory])
        {
          progress.Report.Log($"speckleType {speckleType}");
          string mappedValue = GetMappedValue(hostTypes, incomingTypeCategory, speckleType, progress);
          progress.Report.Log($"mapped value {mappedValue}");
          if (mappings.ContainsKey(incomingTypeCategory))
          {
            mappings[incomingTypeCategory].Add(new MappingValue
              (
                speckleType, mappedValue
              ));
          }
          else
          {
            mappings[incomingTypeCategory] = new List<MappingValue>
              {
                new MappingValue
                (
                  speckleType, mappedValue
                )
              };
          }
        }
      }

      progress.Report.Log($"mapping.Keys {string.Join(",", mappings.Keys)}");
      return mappings;
    }

    public string GetMappedValue(Dictionary<string, List<string>> hostTypes, string category, string speckleType, ProgressViewModel progress)
    {
      string mappedValue = "";
      string hostCategory = "";
      List<int> listVert = new List<int> { };

      // if this count is zero, then there aren't any types of this category loaded into the project
      if (hostTypes.ContainsKey(category) && hostTypes[category].Count != 0)
        hostCategory = category;
      else
        hostCategory = TypeCatMisc;

      foreach (var revitType in hostTypes[hostCategory])
      {
        listVert.Add(LevenshteinDistance(speckleType, revitType));
      }

      mappedValue = hostTypes[hostCategory][listVert.IndexOf(listVert.Min())];

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

    //public void updateRecieveObject(Dictionary<string, List<MappingValue>> Map, List<Base> objects)
    //{
    //  foreach (var @object in objects)
    //  {
    //    try
    //    {
    //      //currently implemented only for Revit objects ~ object models need a bit of refactor for this to be a cleaner code
    //      var propInfo = @object.GetType().GetProperty("type").GetValue(@object) as string;
    //      string typeCategory = GetTypeCategory(@object);
         
    //      if (propInfo != "")
    //      {
    //        string mappingProperty = "";
    //        MappingValue mappingWithMatchingType = Map[typeCategory].Where(i => i.IncomingType == propInfo).First();
    //        mappingProperty = mappingWithMatchingType.OutgoingType ?? mappingWithMatchingType.InitialGuess;
    //        var prop = @object.GetType().GetProperty("type");
    //        prop.SetValue(@object, mappingProperty);
    //      }
    //    }
    //    catch
    //    {

    //    }
    //  }
    //}

    // Warning, these strings need to be the same as the strings in the MappingViewModel
    private string TypeCatMaterials = "Materials";
    private string TypeCatFloors = "Floors";
    private string TypeCatWalls = "Walls";
    private string TypeCatFraming = "Framing";
    private string TypeCatColumns = "Columns";
    private string TypeCatMisc = "Miscellaneous";

    public string GetTypeCategory(Base obj, ProgressViewModel progress)
    {
      string speckleType = obj.speckle_type.Split('.').LastOrDefault().ToLower();

      switch (speckleType)
      {
        #region CSI
        //case "CSIPier":
        //case "CSISpandrel":
        //case "CSIGridLines":
        case "csielement1d":
          switch ((int)obj["type"])
          {
            case (int)ElementType1D.Bar:
            case (int)ElementType1D.Beam:
            case (int)ElementType1D.Brace:
            case (int)ElementType1D.Cable:
              return TypeCatFraming;
            case (int)ElementType1D.Column:
              return TypeCatColumns;
          }
          return TypeCatMisc;
        case "csielement2d":
          progress.Report.Log("csielement2d");
          if (obj.GetMemberNames().Contains("property") && obj["property"] is Base prop)
          {
            progress.Report.Log("element 2d contains property");
            if (prop.GetMemberNames().Contains("type") && (int)obj["type"] is int type)
            {
              progress.Report.Log($"contains type {type}");
              switch (type)
              {
                case (int)PropertyType2D.Wall:
                  return TypeCatWalls;
                default:
                  return TypeCatFloors;
              }
            }
          }
          return TypeCatMisc;
        #endregion

        #region General
        case string a when a.Contains("beam"):
        case string b when b.Contains("brace"):
          return TypeCatFraming;

        case string a when a.Contains("column"):
          return TypeCatColumns;

        case string a when a.Contains("material"):
          return TypeCatMaterials;

        case string a when a.Contains("floor"):
          return TypeCatFloors;

        case string a when a.Contains("wall"):
          return TypeCatWalls;
        #endregion
        default:
          return TypeCatMisc;
      }
    }

    public class customTypesFilter
    {
      public string key;
      public Type objectClass;
      public List<BuiltInCategory> categories;

      public customTypesFilter(string key, Type objectClass = null, List<BuiltInCategory> categories = null)
      {
        this.key = key;
        this.objectClass = objectClass;
        this.categories = categories;
      }
    }

    public Dictionary<string, List<string>> GetHostTypes()
    {
      var customHostTypesFilter = new List<customTypesFilter>
      {
        new customTypesFilter(TypeCatMaterials, typeof(Autodesk.Revit.DB.Material)),
        new customTypesFilter(TypeCatFloors, typeof(FloorType)),
        new customTypesFilter(TypeCatWalls, typeof(WallType)),
        new customTypesFilter(TypeCatFraming, typeof(FamilySymbol), new List<BuiltInCategory>{ BuiltInCategory.OST_StructuralFraming}),
        new customTypesFilter(TypeCatColumns, typeof(FamilySymbol), new List<BuiltInCategory>{ BuiltInCategory.OST_Columns, BuiltInCategory.OST_StructuralColumns}),
        new customTypesFilter(TypeCatMisc), 
      };

      var returnDict = new Dictionary<string, List<string>>();
      var exclusionFilterIds = new List<ElementId>();
      FilteredElementCollector list = null;
      foreach (var customType in customHostTypesFilter)
      {
        var collector = new FilteredElementCollector(CurrentDoc.Document).WhereElementIsElementType();

        if (customType.categories != null && customType.categories.Count > 1)
        {
          var filter = new ElementMulticategoryFilter(customType.categories);
          list = collector.OfClass(typeof(FamilySymbol)).WherePasses(filter);
        }
        else if (customType.objectClass != null)
        {
          list = collector.OfClass(customType.objectClass);
        }
        else
        {
          list = collector;
        }

        var types = list.Select(o => o.Name).Distinct().ToList();
        exclusionFilterIds.AddRange(list.Select(o => o.Id).Distinct().ToList());
        returnDict[customType.key] = types;
      }

      return returnDict;
    }

    private Dictionary<string, List<string>> GetIncomingTypes(List<Base> objects, ProgressViewModel progress, string sourceApp)
    {
      progress.Report.Log($"num objs {objects.Count} ");
      progress.Report.Log($"source app no nums {Regex.Replace(sourceApp.ToLower(), @"[\d-]", string.Empty)}");
      var returnDict = new Dictionary<string, List<string>>();
      string typeCategory = null;

      foreach (var @object in objects)
      {
        string type = null;
        
        switch (Regex.Replace(sourceApp.ToLower(), @"[\d-]", string.Empty))
        {
          case "autocad":
            type = @object.GetType().GetProperty("type").GetValue(@object) as string;
            break;
          case "etabs":
            if (@object.GetMembers().ContainsKey("elements") && @object["elements"] is List<Base> els)
            {
              foreach (var el in els)
              {
                typeCategory = GetTypeCategory(el, progress);
                if (!returnDict.ContainsKey(typeCategory))
                  returnDict[typeCategory] = new List<string>();

                if (el.GetMemberNames().Contains("property") && el["property"] is Base prop)
                {
                  if (prop.GetMemberNames().Contains("name") && !string.IsNullOrEmpty(prop["name"] as string))
                  {
                    if (!returnDict[typeCategory].Contains(prop["name"] as string))
                      returnDict[typeCategory].Add(prop["name"] as string);
                  }
                }
              }
            }
            break;
          case "revit":
            typeCategory = GetTypeCategory(@object, progress);
            if (!returnDict.ContainsKey(typeCategory))
              returnDict[typeCategory] = new List<string>();
            returnDict[typeCategory].Add(@object.GetType().GetProperty("type").GetValue(@object) as string);

            break;
          case "rhino":
            type = @object.GetType().GetProperty("type").GetValue(@object) as string;
            break;
          case "teklastructures":
            typeCategory = GetTypeCategory(@object, progress);
            if (!returnDict.ContainsKey(typeCategory))
              returnDict[typeCategory] = new List<string>();

            if (@object.GetMemberNames().Contains("profile") && @object["profile"] is Base profile)
            {
              if (profile.GetMemberNames().Contains("name") && !string.IsNullOrEmpty(profile["name"] as string))
              {
                if (!returnDict[typeCategory].Contains(profile["name"] as string))
                  returnDict[typeCategory].Add(profile["name"] as string);
              }
            }
            break;
        }
      }

      // make sure list types in list are distinct
      var newDictionary = returnDict.ToDictionary(entry => entry.Key, entry => entry.Value);
      foreach (var key in newDictionary.Keys)
      {
        progress.Report.Log($"key {key} value {String.Join(",", returnDict[key])}");
        returnDict[key] = returnDict[key].Distinct().ToList();
      }
      return returnDict;
    }

    private void SetMappedValues(Dictionary<string,List<MappingValue>> userMap, List<Base> objects, ProgressViewModel progress, string sourceApp)
    {
      progress.Report.Log($"num objs {objects.Count} host app {sourceApp}");
      string typeCategory = null;
      List<string> mappedValues = new List<string>();

      foreach (var @object in objects)
      {
        string type = null;

        switch (Regex.Replace(sourceApp.ToLower(), @"[\d-]", string.Empty))
        {
          case "autocad":
            type = @object.GetType().GetProperty("type").GetValue(@object) as string;
            break;
          case "etabs":
            progress.Report.Log($"etabs");

            if (@object.GetMembers().ContainsKey("elements") && @object["elements"] is List<Base> els)
            {
              foreach (var el in els)
              {
                typeCategory = GetTypeCategory(el, progress);
                if (!userMap.ContainsKey(typeCategory))
                  continue;

                if (el.GetMemberNames().Contains("property") && el["property"] is Base prop)
                {
                  if (prop.GetMemberNames().Contains("name") && prop.GetType().GetProperty("name") is System.Reflection.PropertyInfo info)
                  {
                    if (mappedValues.Contains(info.GetValue(prop) as string))
                      continue;

                    MappingValue mappingWithMatchingType = userMap[typeCategory].Where(i => i.IncomingType == info.GetValue(prop) as string).First();
                    string mappingProperty = mappingWithMatchingType.OutgoingType ?? mappingWithMatchingType.InitialGuess;

                    info.SetValue(prop, mappingProperty);
                    mappedValues.Add(mappingProperty);
                  }
                }
              }
            }
            break;
          case "revit":
            typeCategory = GetTypeCategory(@object, progress);
            if (!userMap.ContainsKey(typeCategory))
              continue;

            if (@object.GetMemberNames().Contains("type") && @object.GetType().GetProperty("type") is System.Reflection.PropertyInfo revType)
            {
              if (mappedValues.Contains(revType.GetValue(@object) as string))
                continue;

              MappingValue mappingWithMatchingType = userMap[typeCategory].Where(i => i.IncomingType == revType.GetValue(@object) as string).First();
              string mappingProperty = mappingWithMatchingType.OutgoingType ?? mappingWithMatchingType.InitialGuess;

              revType.SetValue(@object, mappingProperty);
              mappedValues.Add(mappingProperty);
            }

            break;
          case "rhino":
            type = @object.GetType().GetProperty("type").GetValue(@object) as string;
            break;
          case "teklastructures":
            typeCategory = GetTypeCategory(@object, progress);
            if (!userMap.ContainsKey(typeCategory))
              continue;

            if (@object.GetMemberNames().Contains("profile") && @object["profile"] is Base profile)
            {
              if (profile.GetMemberNames().Contains("name") && profile.GetType().GetProperty("name") is System.Reflection.PropertyInfo info)
              {
                if (mappedValues.Contains(info.GetValue(profile) as string))
                  continue;

                MappingValue mappingWithMatchingType = userMap[typeCategory].Where(i => i.IncomingType == info.GetValue(profile) as string).First();
                string mappingProperty = mappingWithMatchingType.OutgoingType ?? mappingWithMatchingType.InitialGuess;

                info.SetValue(profile, mappingProperty);
                mappedValues.Add(mappingProperty);
              }
            }
            break;
        }
      }
    }

    public bool UpdateExistingMapping(Dictionary<string, List<MappingValue>> settingsMapping, Dictionary<string, List<string>> hostTypesDict, Dictionary<string, List<string>> incomingTypesDict, ProgressViewModel progress)
    {
      // no existing mappings exist
      if (settingsMapping == null)
        return true;

      bool newTypesExist = false;
      List<Base> objectsWithNewTypes = new List<Base>();

      foreach (var typeCategory in incomingTypesDict.Keys)
      {
        if (!settingsMapping.ContainsKey(typeCategory) && incomingTypesDict[typeCategory].Count > 0)
        {
          newTypesExist = true;
          settingsMapping[typeCategory] = new List<MappingValue>();
          foreach (var type in incomingTypesDict[typeCategory])
          {
            string mappedValue = GetMappedValue(hostTypesDict, typeCategory, type, progress);
            settingsMapping[typeCategory].Add(new MappingValue(type, mappedValue, true));
          }
        }
        else if (settingsMapping.ContainsKey(typeCategory))
        {
          foreach (var type in incomingTypesDict[typeCategory])
          {
            if (!settingsMapping[typeCategory].Any(i => i.IncomingType == type))
            {
              newTypesExist = true;
              string mappedValue = GetMappedValue(hostTypesDict, typeCategory, type, progress);
              settingsMapping[typeCategory].Add(new MappingValue(type, mappedValue, true));
            }
          }
        }
      }
      return newTypesExist;
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
