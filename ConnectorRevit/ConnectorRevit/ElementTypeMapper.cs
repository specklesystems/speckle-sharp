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
using System.Threading.Tasks;
using ReactiveUI;
using Avalonia.Threading;

namespace ConnectorRevit
{
  internal class ElementTypeMapper
  {
    public static async Task Map(ISpeckleConverter converter, ISetting mapOnReceiveSetting, List<ApplicationObject> flattenedCommit, Dictionary<string, Base> storedObjects)
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

      var currentMapping = DeserializeMapping(mappingSetting);
      currentMapping ??= new TypeMap();

      var hostTypesContainer = GetHostTypesAndAddIncomingTypes(typeRetriever, flattenedCommit, storedObjects, currentMapping, out var newTypesExist);
      //if (currentMapping == null)
      //{
      //  // TODO
      //}

      //currentMapping.AddIncomingTypes(incomingTypes, out newTypesExist);
      if (!newTypesExist && mappingSetting.Selection != ConnectorBindingsRevit.everyReceive) { return; }

      // show custom mapping dialog if the settings corrospond to what is being received
      var vm = new TypeMappingOnReceiveViewModel(currentMapping, hostTypesContainer, newTypesExist);
      
      

      currentMapping = await Dispatcher.UIThread.InvokeAsync<ITypeMap>(() => {
        var mappingView = new MappingViewDialog
        {
          DataContext = vm
        };
        return mappingView.ShowDialog<ITypeMap>();
      }).ConfigureAwait(false);

       //await mappingView.ShowDialog<ITypeMap>().ConfigureAwait(true);

      while (vm.DoneMapping == false)
      {
        //hostTypesDict = await ImportFamilyTypes(hostTypesDict).ConfigureAwait(true);

        vm = new TypeMappingOnReceiveViewModel(currentMapping, hostTypesContainer, newTypesExist);
        currentMapping = await Dispatcher.UIThread.InvokeAsync<ITypeMap>(() => {
          var mappingView = new MappingViewDialog
          {
            DataContext = vm
          };
          return mappingView.ShowDialog<ITypeMap>();
        }).ConfigureAwait(false);
      }

      // close the dialog
      MainViewModel.CloseDialog();

      mappingSetting.MappingJson = JsonConvert.SerializeObject(currentMapping);

      // update the mapping object for the user mapped types
      SetMappedValues(typeRetriever, currentMapping);
    }

    private static void SetMappedValues(IRevitElementTypeRetriever<ElementType> typeRetriever, ITypeMap currentMapping)
    {
      foreach (var (@base, mappingValue) in currentMapping.GetAllBasesWithMappings())
      {
        typeRetriever.SetRevitTypeOfBase(@base, mappingValue.OutgoingType ?? mappingValue.InitialGuess);
      }
    }

    public static HostTypeAsStringContainer GetHostTypesAndAddIncomingTypes(IRevitElementTypeRetriever<ElementType> typeRetriever, List<ApplicationObject> flattenedCommit, Dictionary<string, Base> storedObjects, ITypeMap typeMap, out bool newTypesExist)
    {
      var incomingTypes = new Dictionary<string, List<ISingleValueToMap>>();
      var hostTypes = new HostTypeAsStringContainer();

      newTypesExist = false;
      foreach (var appObj in flattenedCommit)
      {
        var @base = storedObjects[appObj.OriginalId];

        var incomingType = typeRetriever.GetRevitTypeOfBase(@base);
        if (incomingType == null) continue; // TODO: do we want to throw an error (or at least log it)

        var category = typeRetriever.GetRevitCategoryOfBase(@base);
        var elementTypes = typeRetriever.GetAndCacheAvailibleTypes(@base);
        var exactTypeMatch = typeRetriever.CacheContainsTypeWithName(incomingType);

        if (exactTypeMatch) continue;

        hostTypes.AddCategoryWithTypesIfCategoryIsNew(category, elementTypes.Select(type => type.Name));

        var initialGuess = GetMappedValue(elementTypes, category, incomingType);
        typeMap.AddIncomingType(@base, incomingType, category, initialGuess, out var isNewType);
        if (isNewType) newTypesExist = true;
      }

      hostTypes.SetAllTypes(
        typeRetriever
          .GetAllCachedElementTypes()
          .Select(type => type.Name)
      );

      return hostTypes;
    }

    public Dictionary<string, List<MappingValue>>? DeserializeMappingAsDict(MappingSeting mappingSetting)
    {
      if (mappingSetting.MappingJson != null)
      {
        return JsonConvert.DeserializeObject<Dictionary<string, List<MappingValue>>>(mappingSetting.MappingJson);
      }
      return null;
    }
    
    public static ITypeMap? DeserializeMapping(MappingSeting mappingSetting)
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
    private static string GetMappedValue(IEnumerable<ElementType> elementTypes, string category, string speckleType)
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
