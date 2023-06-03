#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;
using DesktopUI2.Views.Windows.Dialogs;
using RevitSharedResources.Interfaces;
using Speckle.ConnectorRevit.UI;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;
using DesktopUI2.Models.TypeMappingOnReceive;

namespace ConnectorRevit
{
  internal class ElementTypeMapper
  {
    public void Map(ISpeckleConverter converter, ISetting mapOnReceiveSetting, List<ApplicationObject> flattenedCommit, Dictionary<string, Base> storedObjects)
    {
      if (converter is not IRevitElementTypeRetriever<ElementType> typeRetriever)
      {
        throw new ArgumentException($"Converter does not implement interface {nameof(IRevitElementTypeRetriever<ElementType>)}");
      }

      // Get Settings for recieve on mapping 
      if (mapOnReceiveSetting is not MappingSeting mappingSetting
        || mappingSetting.Selection == null
        || mappingSetting.Selection == ConnectorBindingsRevit.noMapping)
      {
        return;
      }

      var incomingTypesDict = GetIncomingTypes(typeRetriever, flattenedCommit, storedObjects);
      var currentMapping = DeserializeMapping(mappingSetting);

      if (currentMapping == null)
      {
        // TODO
      }

      currentMapping.AddIncomingTypes(incomingTypesDict, out var newTypesExist);
      if (!newTypesExist && mappingSetting.Selection != ConnectorBindingsRevit.everyReceive) { return; }

      // show custom mapping dialog if the settings corrospond to what is being received
      var vm = new MappingViewModel(currentMapping, hostTypesDict, newTypesExist);
      MappingViewDialog mappingView = new MappingViewDialog
      {
        DataContext = vm
      };

      mapping = await mappingView.ShowDialog<Dictionary<string, List<MappingValue>>>().ConfigureAwait(true);

      while (vm.DoneMapping == false)
      {
        hostTypesDict = await ImportFamilyTypes(hostTypesDict).ConfigureAwait(true);

        vm = new MappingViewModel(mapping, hostTypesDict, newTypesExist && !isFirstTimeReceiving);
        mappingView = new MappingViewDialog
        {
          DataContext = vm
        };

        mapping = await mappingView.ShowDialog<Dictionary<string, List<MappingValue>>>();
      }

      // close the dialog
      MainViewModel.CloseDialog();

      mappingSetting.MappingJson = JsonConvert.SerializeObject(mapping);

      // update the mapping object for the user mapped types
      SetMappedValues(mapping, progress, sourceApp);
    }

    public Dictionary<string, List<MappingValue>> GetIncomingTypes(IRevitElementTypeRetriever<ElementType> typeRetriever, List<ApplicationObject> flattenedCommit, Dictionary<string, Base> storedObjects)
    {
      var incomingTypes = new Dictionary<string, List<MappingValue>>();
      foreach (var appObj in flattenedCommit)
      {
        var @base = storedObjects[appObj.OriginalId];

        var incomingType = typeRetriever.GetRevitTypeOfBase(@base);
        if (incomingType == null) continue; // TODO: do we want to throw an error (or at least log it)

        var category = typeRetriever.GetRevitCategoryOfBase(@base);
        var elementTypes = typeRetriever.GetAndCacheAvailibleTypes(@base);
        var exactTypeMatch = typeRetriever.CacheContainsTypeWithName(incomingType);

        if (exactTypeMatch) continue;

        if (!incomingTypes.TryGetValue(category, out var categoryTypes))
        {
          categoryTypes = new List<MappingValue>();
          incomingTypes[category] = categoryTypes;
        }

        var initialGuess = GetMappedValue(elementTypes, category, incomingType);
        categoryTypes.Add(new MappingValue(incomingType, initialGuess, true));
      }

      return incomingTypes;
    }

    public Dictionary<string, List<MappingValue>>? DeserializeMappingAsDict(MappingSeting mappingSetting)
    {
      if (mappingSetting.MappingJson != null)
      {
        return JsonConvert.DeserializeObject<Dictionary<string, List<MappingValue>>>(mappingSetting.MappingJson);
      }
      return null;
    }
    
    public TypeMap? DeserializeMapping(MappingSeting mappingSetting)
    {
      if (mappingSetting.MappingJson != null)
      {
        return JsonConvert.DeserializeObject<TypeMap>(mappingSetting.MappingJson);
      }
      return null;
    }

    /// <summary>
    /// Gets the most similar host type of the same category for a single incoming type
    /// </summary>
    /// <param name="hostTypes"></param>
    /// <param name="category"></param>
    /// <param name="speckleType"></param>
    /// <returns>name of host type as string</returns>
    private string GetMappedValue(IEnumerable<ElementType> elementTypes, string category, string speckleType)
    {
      var shortestDistance = int.MaxValue;
      var closestType = $"No families of the category \"{category}\" are loaded into the project";

      foreach (var elementType in elementTypes)
      {
        var distance = LevenshteinDistance(speckleType, elementType.Name);
        if (distance < int.MaxValue)
        {
          shortestDistance = distance;
          closestType = elementType.Name;
        }
      }

      return closestType;
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
  }
}
