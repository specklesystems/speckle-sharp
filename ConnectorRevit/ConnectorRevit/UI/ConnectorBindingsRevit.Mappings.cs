using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Autodesk.Revit.DB;
using DesktopUI2.Models;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;
using DesktopUI2.Views.Windows.Dialogs;
using Newtonsoft.Json;
using Revit.Async;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using static DesktopUI2.ViewModels.ImportFamiliesDialogViewModel;
using static DesktopUI2.ViewModels.MappingViewModel;

namespace Speckle.ConnectorRevit.UI
{

  public partial class ConnectorBindingsRevit
  {

    /// <summary>
    /// Updates the flattenedBase object with user selected types
    /// </summary>
    /// <param name="state"></param>
    /// <param name="progress"></param>
    /// <param name="sourceApp"></param>
    /// <returns></returns>
    public async Task UpdateForCustomMapping(StreamState state, ProgressViewModel progress, string sourceApp)
    {
      // Get Settings for recieve on mapping 
      var receiveMappingsModelsSetting = (CurrentSettings.FirstOrDefault(x => x.Slug == "receive-mappings") as MappingSeting);
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
        Dictionary<string, List<string>> incomingTypesDict = GetIncomingTypes(progress, sourceApp);

        // if mappings already exist, update them and return true
        bool newTypesExist = UpdateExistingMapping(settingsMapping, hostTypesDict, incomingTypesDict, progress);

        Dictionary<string, List<MappingValue>> Mapping = settingsMapping;
        if (Mapping == null)
          Mapping = ReturnFirstPassMap(incomingTypesDict, hostTypesDict, progress);

        try
        {
          // show custom mapping dialog if the settings corrospond to what is being received
          if (newTypesExist || receiveMappings == everyReceive)
          {
            var vm = new MappingViewModel(Mapping, hostTypesDict, newTypesExist && !isFirstTimeReceiving);
            MappingViewDialog mappingView = new MappingViewDialog
            {
              DataContext = vm
            };

            Mapping = await mappingView.ShowDialog<Dictionary<string, List<MappingValue>>>();

            while (vm.DoneMapping == false)
            {
              hostTypesDict = await ImportFamilyTypes(hostTypesDict);

              vm = new MappingViewModel(Mapping, hostTypesDict, newTypesExist && !isFirstTimeReceiving);
              mappingView = new MappingViewDialog
              {
                DataContext = vm
              };

              Mapping = await mappingView.ShowDialog<Dictionary<string, List<MappingValue>>>();
            }

            // close the dialog
            MainViewModel.CloseDialog();

            receiveMappingsModelsSetting.MappingJson = JsonConvert.SerializeObject(Mapping);

          }
          // update the mapping object for the user mapped types
          SetMappedValues(Mapping, progress, sourceApp);
        }
        catch (Exception ex)
        {
          progress.Report.Log($"Could not make new mapping {ex}");
        }
      }
    }

    /// <summary>
    /// This method creates a mapping dictionary by looping through all of the incoming types and mapping them to the most similar host type of the same category
    /// </summary>
    /// <param name="incomingTypesDict"></param>
    /// <param name="hostTypes"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    private Dictionary<string, List<MappingValue>> ReturnFirstPassMap(Dictionary<string, List<string>> incomingTypesDict, Dictionary<string, List<string>> hostTypes, ProgressViewModel progress)
    {
      var mappings = new Dictionary<string, List<MappingValue>> { };
      foreach (var incomingTypeCategory in incomingTypesDict.Keys)
      {
        foreach (var speckleType in incomingTypesDict[incomingTypeCategory])
        {
          string mappedValue = GetMappedValue(hostTypes, incomingTypeCategory, speckleType);
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

    /// <summary>
    /// Gets the most similar host type of the same category for a single incoming type
    /// </summary>
    /// <param name="hostTypes"></param>
    /// <param name="category"></param>
    /// <param name="speckleType"></param>
    /// <returns>name of host type as string</returns>
    private string GetMappedValue(Dictionary<string, List<string>> hostTypes, string category, string speckleType)
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

    /// <summary>
    /// Returns the distance between two strings
    /// </summary>
    /// <param name="s"></param>
    /// <param name="t"></param>
    /// <returns>distance as an integer</returns>
    private static int LevenshteinDistance(string s, string t)
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

    private const string TypeCatMaterials = "Materials";
    private const string TypeCatFloors = "Floors";
    private const string TypeCatWalls = "Walls";
    private const string TypeCatFraming = "Framing";
    private const string TypeCatColumns = "Columns";
    private const string TypeCatMisc = "Miscellaneous"; // Warning, this string need to be the same as the strings in the MappingViewModel
    private List<string> allTypeCategories = new List<string>
    {
      TypeCatColumns,
      TypeCatFloors,
      TypeCatFraming,
      TypeCatMaterials,
      TypeCatMisc,
      TypeCatWalls
    };

    /// <summary>
    /// Gets the category of a given base object
    /// </summary>
    /// <param name="obj"></param>
    /// <returns>name of category type as string</returns>
    private string GetTypeCategory(object @object)
    {
      string speckleType = null;
      Base obj = null;

      if (@object is Base baseObj)
      {
        obj = baseObj;
        speckleType = baseObj.speckle_type.Split('.').LastOrDefault().ToLower();
      }
      else if (@object is string s)
      {
        speckleType = s.ToLower();
      }
      else
      {
        throw new Exception("@object must be base obj or string obj");
      }

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
          if (obj.GetMemberNames().Contains("property") && obj["property"] is Base prop)
          {
            if (prop.GetMemberNames().Contains("type") && (int)obj["type"] is int type)
            {
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
        case string c when c.Contains("framing"):
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

    /// <summary>
    /// Helper class for creating filters for all the different Revit types
    /// </summary>
    private class customTypesFilter
    {
      public string key;
      public Type objectClass;
      public ICollection<BuiltInCategory> categories;

      public customTypesFilter(string key, Type objectClass = null, ICollection<BuiltInCategory> categories = null)
      {
        this.key = key;
        this.objectClass = objectClass;
        this.categories = categories;
      }
    }

    /// <summary>
    /// Get an object with all the Revit types in the current project
    /// </summary>
    /// <returns>A dictionary where the keys are type categories and the value is a list of all the revit types that fit that category in the existing project</returns>
    private Dictionary<string, List<string>> GetHostTypes()
    {
      var returnDict = new Dictionary<string, List<string>>();
      var exclusionFilterIds = new List<ElementId>();
      foreach (var customType in allTypeCategories)
      {
        var collector = new FilteredElementCollector(CurrentDoc.Document).WhereElementIsElementType();
        FilteredElementCollector list = GetFilteredElements(customType, collector);

        var types = list.Select(o => o.Name).Distinct().ToList();
        exclusionFilterIds.AddRange(list.Select(o => o.Id).Distinct().ToList());
        returnDict[customType] = types;
      }
      return returnDict;
    }

    private customTypesFilter GetCustomTypeFilter(string category)
    {
      switch (category)
      {
        case TypeCatMaterials:
          return new customTypesFilter(TypeCatMaterials, typeof(Autodesk.Revit.DB.Material));
        case TypeCatFloors:
          return new customTypesFilter(TypeCatFloors, typeof(FloorType));
        case TypeCatWalls:
          return new customTypesFilter(TypeCatWalls, typeof(WallType));
        case TypeCatFraming:
          return new customTypesFilter(TypeCatFraming, typeof(FamilySymbol), new List<BuiltInCategory> { BuiltInCategory.OST_StructuralFraming });
        case TypeCatColumns:
          return new customTypesFilter(TypeCatColumns, typeof(FamilySymbol), new List<BuiltInCategory> { BuiltInCategory.OST_Columns, BuiltInCategory.OST_StructuralColumns });
        case TypeCatMisc:
        default:
          return new customTypesFilter(TypeCatMisc);
      }
    }

    private FilteredElementCollector GetFilteredElements(string category, FilteredElementCollector? collector)
    {
      if (!allTypeCategories.Contains(category))
        throw new Exception($"Category string {category} is not a recognized category");

      collector ??= new FilteredElementCollector(CurrentDoc.Document);

      FilteredElementCollector list = null;
      var customTypeFilter = GetCustomTypeFilter(category);

      if (customTypeFilter.categories != null && customTypeFilter.categories.Count > 0)
      {
        var filter = new ElementMulticategoryFilter(customTypeFilter.categories);
        list = collector.OfClass(customTypeFilter.objectClass).WherePasses(filter);
      }
      else if (customTypeFilter.objectClass != null)
      {
        list = collector.OfClass(customTypeFilter.objectClass);
      }
      else
      {
        list = collector;
      }
      return list;
    }

    /// <summary>
    /// Get an object with all the incoming types for the receive object
    /// </summary>
    /// <returns>A dictionary where the keys are type categories and the value is a list of all the incoming types that fit that category</returns>
    private Dictionary<string, List<string>> GetIncomingTypes(ProgressViewModel progress, string sourceApp)
    {
      var returnDict = new Dictionary<string, List<string>>();
      string typeCategory = null;

      foreach (var obj in Preview)
      {
        var @object = StoredObjects[obj.OriginalId];
        string type = null;

        switch (Regex.Replace(sourceApp.ToLower(), @"[\d-]", string.Empty))
        {
          case "etabs":
            if (@object.GetMembers().ContainsKey("elements") && @object["elements"] is List<Base> els)
            {
              foreach (var el in els)
              {
                typeCategory = GetTypeCategory(el);
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
            typeCategory = GetTypeCategory(@object);
            if (!returnDict.ContainsKey(typeCategory))
              returnDict[typeCategory] = new List<string>();
            if (!string.IsNullOrEmpty(@object["type"] as string))
              returnDict[typeCategory].Add(@object["type"] as string);

            break;
          case "teklastructures":
            typeCategory = GetTypeCategory(@object);
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
        returnDict[key] = returnDict[key].Distinct().ToList();
      }
      return returnDict;
    }

    /// <summary>
    /// Update receive object to include the user's custom mapping
    /// </summary>
    private void SetMappedValues(Dictionary<string, List<MappingValue>> userMap, ProgressViewModel progress, string sourceApp)
    {

      string typeCategory = null;
      List<string> mappedValues = new List<string>();

      foreach (var obj in Preview)
      {
        var @object = StoredObjects[obj.OriginalId];
        string type = null;

        switch (Regex.Replace(sourceApp.ToLower(), @"[\d-]", string.Empty))
        {
          case "etabs":
            if (@object.GetMembers().ContainsKey("elements") && @object["elements"] is List<Base> els)
            {
              foreach (var el in els)
              {
                typeCategory = GetTypeCategory(el);
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
            typeCategory = GetTypeCategory(@object);
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
          case "teklastructures":
            typeCategory = GetTypeCategory(@object);
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

      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Mappings Applied" } });
    }

    /// <summary>
    /// Update the custom type mapping that the user has saved
    /// </summary>
    /// <returns>A bool indicating whether there are new incoming types or not</returns>
    private bool UpdateExistingMapping(Dictionary<string, List<MappingValue>> settingsMapping, Dictionary<string, List<string>> hostTypesDict, Dictionary<string, List<string>> incomingTypesDict, ProgressViewModel progress)
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
            string mappedValue = GetMappedValue(hostTypesDict, typeCategory, type);
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
              string mappedValue = GetMappedValue(hostTypesDict, typeCategory, type);
              settingsMapping[typeCategory].Add(new MappingValue(type, mappedValue, true));
            }
          }
        }
      }
      return newTypesExist;
    }


    /// <summary>
    /// Imports family symbols into Revit
    /// </summary>
    /// <param name="Mapping"></param>
    /// <returns>
    /// New mapping value with newly imported types added (if applicable)
    /// </returns>
    public override async Task<Dictionary<string, List<MappingValue>>> ImportFamilyCommand(Dictionary<string, List<MappingValue>> Mapping)
    {
      return Mapping;
    }

    /// <summary>
    /// Imports new family types into Revit
    /// </summary>
    /// <param name="hostTypesDict"></param>
    /// <returns>
    /// New host types dictionary with newly imported types added (if applicable)
    /// </returns>
    public async Task<Dictionary<string, List<string>>> ImportFamilyTypes(Dictionary<string, List<string>> hostTypesDict)
    {
      var windowsDialog = new OpenFileDialog();
      windowsDialog.Title = "Choose Revit Families";
      windowsDialog.Filter = "Revit Families (*.rfa)|*.rfa";
      windowsDialog.Multiselect = true;
      var result = windowsDialog.ShowDialog();

      if (result == DialogResult.Cancel)
      {
        return hostTypesDict;
      }

      var allSymbols = new Dictionary<string, List<Symbol>>();
      var familyInfo = new Dictionary<string, FamilyInfo>();

      foreach (var path in windowsDialog.FileNames)
      {
        string pathClone = string.Copy(path);

        //open family file as xml to extract all family symbols without loading all of them into the project
        var symbols = new List<string>();
        CurrentDoc.Document.Application.ExtractPartAtomFromFamilyFile(path, path + ".xml");
        XmlDocument xmlDoc = new XmlDocument(); // Create an XML document object
        xmlDoc.Load(path + ".xml");

        XmlNamespaceManager nsman = new XmlNamespaceManager(xmlDoc.NameTable);
        nsman.AddNamespace("ab", "http://www.w3.org/2005/Atom");

        string familyName = pathClone.Split('\\').LastOrDefault().Split('.').FirstOrDefault();
        if (string.IsNullOrEmpty(familyName))
          continue;

        Family match = null;
        var catRoot = xmlDoc.GetElementsByTagName("category");
        var category = TypeCatMisc;

        foreach (var node in catRoot)
        {
          if (node is XmlElement xmlNode)
          {
            var term = xmlNode.SelectSingleNode("ab:term", nsman);
            if (term != null)
            {
              category = GetTypeCategory(term.InnerText);
              if (category == TypeCatMisc)
                continue;

              var filter = GetCustomTypeFilter(category);
              var families = new FilteredElementCollector(CurrentDoc.Document).OfClass(typeof(Family));
              var list = families.ToElements().Cast<Family>().ToList();

              match = list.FirstOrDefault(x => x.Name == familyName && filter.categories.Contains((BuiltInCategory)x.FamilyCategory?.Id.IntegerValue));
              if (match != null)
                break;
            }
          }
        }

        familyInfo.Add(familyName, new FamilyInfo(path, category));

        // see which types have already been loaded into the project from the selected family
        var loadedSymbols = new List<string>();
        if (match != null) //family exists in project
        {
          var symbolIds = match.GetFamilySymbolIds();
          foreach (var id in symbolIds)
          {
            var sym = CurrentDoc.Document.GetElement(id);
            loadedSymbols.Add(sym.Name);
          }
        }

        // get all types from XML document
        XmlNodeList familySymbols;
        try
        {
          var familyRoot = xmlDoc.GetElementsByTagName("A:family");
          if (familyRoot.Count == 1)
          {
            nsman.AddNamespace("A", familyRoot[0].NamespaceURI);
            nsman.AddNamespace("ab", "http://www.w3.org/2005/Atom");
            familySymbols = familyRoot[0].SelectNodes("A:part/ab:title", nsman);
            if (familySymbols.Count > 0)
              allSymbols[familyName] = new List<Symbol>();
            foreach (var symbol in familySymbols)
            {
              if (symbol is XmlElement el)
              {
                if (loadedSymbols.Contains(el.InnerText))
                  allSymbols[familyName].Add(new Symbol(el.InnerText, familyName, true));
                else
                  allSymbols[familyName].Add(new Symbol(el.InnerText, familyName));
              }
            }
          }
        }
        catch (Exception e)
        { }

        // delete the newly created xml file
        try
        {
          System.IO.File.Delete(path + ".xml");
        }
        catch (Exception ex)
        { }
      }

      //close current dialog body
      MainViewModel.CloseDialog();

      var vm = new ImportFamiliesDialogViewModel(allSymbols);
      var importFamilies = new ImportFamiliesDialog
      {
        DataContext = vm
      };

      await importFamilies.ShowDialog<object>();

      if (vm.selectedFamilySymbols.Count == 0)
      {
        //close current dialog body
        MainViewModel.CloseDialog();
        return hostTypesDict;
      }

      var newHostTypes = await RevitTask.RunAsync(app =>
      {
        using (var t = new Transaction(CurrentDoc.Document, $"Import family types"))
        {
          t.Start();
          bool symbolLoaded = false;

          foreach (var symbol in vm.selectedFamilySymbols)
          {
            bool successfullyImported = CurrentDoc.Document.LoadFamilySymbol(familyInfo[symbol.FamilyName].Path, symbol.Name);
            if (successfullyImported)
            {
              symbolLoaded = true;
              // add newly imported type to host types dict
              hostTypesDict[familyInfo[symbol.FamilyName].Category].Add(symbol.Name);
            }
          }

          if (symbolLoaded)
          {
            t.Commit();
            Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() {
              { "name", "Mappings Import Families" },
              { "count", vm.selectedFamilySymbols.Count }});
          }

          else
            t.RollBack();
          return hostTypesDict;
        }
      });




      //close current dialog body
      MainViewModel.CloseDialog();

      return newHostTypes;
    }

    public class FamilyInfo
    {
      public string Path { get; set; }
      public string Category { get; set; }
      public FamilyInfo(string path, string category)
      {
        Path = path;
        Category = category;
      }
    }
  }
}
