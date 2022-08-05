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
    /// Updates the flattenedBase object with user selected types
    /// </summary>
    /// <param name="state"></param>
    /// <param name="progress"></param>
    /// <param name="flattenedBase"></param>
    /// <param name="sourceApp"></param>
    /// <returns></returns>
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

    /// <summary>
    /// This method creates a mapping dictionary by looping through all of the incoming types and mapping them to the most similar host type of the same category
    /// </summary>
    /// <param name="incomingTypesDict"></param>
    /// <param name="hostTypes"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    public Dictionary<string, List<MappingValue>> returnFirstPassMap(Dictionary<string, List<string>> incomingTypesDict, Dictionary<string, List<string>> hostTypes, ProgressViewModel progress)
    {
      var mappings = new Dictionary<string, List<MappingValue>> { };
      foreach (var incomingTypeCategory in incomingTypesDict.Keys)
      {
        foreach (var speckleType in incomingTypesDict[incomingTypeCategory])
        {
          string mappedValue = GetMappedValue(hostTypes, incomingTypeCategory, speckleType, progress);
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

        if (customType.categories != null && customType.categories.Count > 0)
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
      progress.Report.Log($"num objs {objects.Count} host app {Regex.Replace(sourceApp.ToLower(), @"[\d-]", string.Empty)}");
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
            progress.Report.Log("tekla");
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
  }
}
