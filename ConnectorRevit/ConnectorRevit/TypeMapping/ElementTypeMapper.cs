#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Avalonia.Threading;
using DesktopUI2.Models.Settings;
using DesktopUI2.Models.TypeMappingOnReceive;
using DesktopUI2.ViewModels;
using DesktopUI2.Views.Windows.Dialogs;
using RevitSharedResources.Interfaces;
using Speckle.ConnectorRevit.UI;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;
using Speckle.Newtonsoft.Json;
using DB = Autodesk.Revit.DB;

namespace ConnectorRevit.TypeMapping
{
  /// <summary>
  /// Responsible for parsing data received from Speckle for necessary <see cref="DB.ElementType"/>s, and then querying the current Revit document for <see cref="DB.ElementType"/>s that exist in the Project. This class then stores all of the data that it finds in an <see cref="ITypeMap"/> object that can be passed to DUI for user mapping.
  /// </summary>
  internal sealed class ElementTypeMapper
  {
    private readonly IAllRevitCategoriesExposer revitCategoriesExposer;
    private readonly IRevitElementTypeRetriever typeRetriever;
    private List<Base> speckleElements = new();
    private readonly Document document;

    /// <summary>
    /// Initialize ElementTypeMapper. Will throw if the provided <see cref="ISpeckleConverter"/> does not also implement <see cref="IRevitElementTypeRetriever"/> and <see cref="IElementTypeInfoExposer"/>
    /// </summary>
    /// <param name="converter"></param>
    /// <param name="flattenedCommit"></param>
    /// <param name="storedObjects"></param>
    /// <param name="doc"></param>
    /// <exception cref="ArgumentException"></exception>
    public ElementTypeMapper(ISpeckleConverter converter, List<ApplicationObject> flattenedCommit, Dictionary<string, Base> storedObjects, Document doc)
    {
      document = doc;

      if (converter is not IRevitElementTypeRetriever typeRetriever)
      {
        throw new ArgumentException($"Converter does not implement interface {nameof(IRevitElementTypeRetriever)}");
      }
      else this.typeRetriever = typeRetriever;

      if (converter is not IAllRevitCategoriesExposer typeInfoExposer)
      {
        throw new ArgumentException($"Converter does not implement interface {nameof(IRevitElementTypeRetriever)}");
      }
      else revitCategoriesExposer = typeInfoExposer;

      var traversalFunc = DefaultTraversal.CreateTraverseFunc(converter);
      foreach (var appObj in flattenedCommit)
      {
        // add base and traverse nested elements
        speckleElements.AddRange(traversalFunc.Traverse(storedObjects[appObj.OriginalId])
          .Select(c => c.current)
          .Where(converter.CanConvertToNative)
          .OfType<Base>()
        );
      }
    }
    public async Task Map(ISetting mapOnReceiveSetting)
    {
      // Get Settings for recieve on mapping 
      if (mapOnReceiveSetting is not MappingSetting mappingSetting
        || mappingSetting.Selection == ConnectorBindingsRevit.noMapping)
      {
        return;
      }

      var currentMapping = DeserializeMapping(mappingSetting);
      currentMapping ??= new TypeMap();

      var hostTypesContainer = GetHostTypesAndAddIncomingTypes(typeRetriever, currentMapping, out var numNewTypes);
      if (numNewTypes == 0 && mappingSetting.Selection != ConnectorBindingsRevit.everyReceive) { return; }

      if (mappingSetting.Selection == null)
      {
        if (await Dispatcher.UIThread.InvokeAsync<bool>(() =>
        {
          Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object> { { "name", "Type Map" }, { "method", "Missing Types Dialog" } });
          var mappingView = new MissingIncomingTypesDialog();
          return mappingView.ShowDialog<bool>();
        }).ConfigureAwait(false) == false)
        {
          return;
        }
      }

      // show custom mapping dialog if the settings correspond to what is being received
      var vm = new TypeMappingOnReceiveViewModel(currentMapping, hostTypesContainer, numNewTypes == 0);
      FamilyImporter familyImporter = null;

      await Dispatcher.UIThread.InvokeAsync<ITypeMap>(() =>
      {
        var mappingView = new MappingViewDialog
        {
          DataContext = vm
        };
        return mappingView.ShowDialog<ITypeMap>();
      }).ConfigureAwait(false);

      while (vm.DoneMapping == false)
      {
        try
        {
          familyImporter ??= new FamilyImporter(document, revitCategoriesExposer, typeRetriever);
          await familyImporter.ImportFamilyTypes(hostTypesContainer).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
          SpeckleLog.Logger.Error("Error in importing family types", ex);
        }

        vm = new TypeMappingOnReceiveViewModel(currentMapping, hostTypesContainer, numNewTypes == 0);
        await Dispatcher.UIThread.InvokeAsync<ITypeMap>(() =>
        {
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

      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object> { { "name", "Type Map" }, { "method", "Mappings Set" } });
    }

    private static void SetMappedValues(IRevitElementTypeRetriever typeRetriever, TypeMap currentMapping)
    {
      foreach (var (@base, mappingValue) in currentMapping.GetAllBasesWithMappings())
      {
        var mappedHostType = mappingValue.MappedHostType ?? mappingValue.InitialGuess;
        if (mappedHostType == null) continue;

        if (mappedHostType is RevitHostType revitHostType)
        {
          typeRetriever.SetElementFamily(@base, revitHostType.HostFamilyName);
        }
        typeRetriever.SetElementType(@base, mappedHostType.HostTypeName);
      }
    }

    /// <summary>
    /// Since an <see cref="DB.ElementType"/> is a Revit concept, this method just hands the <see cref="Base"/> object to the Converter to figure out the <see cref="DB.ElementType"/>. It also retrieves all possible <see cref="DB.ElementType"/>s for a given category of element and then saves it in a <see cref="HostTypeContainer"/>. This container will be passed to DUI for user mapping.
    /// </summary>
    /// <param name="typeRetriever"></param>
    /// <param name="typeMap"></param>
    /// <param name="numNewTypes"></param>
    /// <returns></returns>
    public HostTypeContainer GetHostTypesAndAddIncomingTypes(IRevitElementTypeRetriever typeRetriever, TypeMap typeMap, out int numNewTypes)
    {
      var incomingTypes = new Dictionary<string, List<ISingleValueToMap>>();
      var hostTypes = new HostTypeContainer();

      numNewTypes = 0;
      foreach (var @base in speckleElements)
      {
        var incomingType = typeRetriever.GetElementType(@base);
        if (incomingType == null)
        {
          SpeckleLog.Logger.Warning("Could not find incoming type on Base of type {baseType} with speckle_type {speckleType}", @base.GetType(), @base.speckle_type);
          continue;
        }

        var incomingFamily = typeRetriever.GetElementFamily(@base);

        var typeInfo = revitCategoriesExposer.AllCategories.GetRevitCategoryInfo(@base);
        if (typeInfo.ElementTypeType == null) continue;

        var elementTypes = typeRetriever.GetOrAddAvailibleTypes(typeInfo);
        var exactTypeMatch = typeRetriever.CacheContainsTypeWithName(typeInfo.CategoryName, incomingType);

        if (exactTypeMatch) continue;

        hostTypes.AddCategoryWithTypesIfCategoryIsNew(typeInfo.CategoryName, elementTypes.Select(type => new RevitHostType(type.FamilyName, type.Name)));
        var initialGuess = DefineInitialGuess(typeMap, incomingFamily, incomingType, typeInfo.CategoryName, elementTypes);

        typeMap.AddIncomingType(@base, incomingType, incomingFamily, typeInfo.CategoryName, initialGuess, out var isNewType);
        if (isNewType) numNewTypes++;
      }

      hostTypes.SetAllTypes(
        typeRetriever
          .GetAllCachedElementTypes()
          .Select(type => new RevitHostType(type.FamilyName, type.Name))
      );

      return hostTypes;
    }

    private static ISingleHostType DefineInitialGuess(TypeMap typeMap, string? incomingFamily, string incomingType, string category, IEnumerable<ElementType> elementTypes)
    {
      var existingMappingValue = typeMap.TryGetMappingValueInCategory(category, incomingFamily, incomingType);

      if (existingMappingValue != null && existingMappingValue.InitialGuess != null)
      {
        return existingMappingValue.MappedHostType ?? existingMappingValue.InitialGuess;
      }

      return GetMappedValue(elementTypes, category, incomingType);
    }

    public Dictionary<string, List<MappingValue>>? DeserializeMappingAsDict(MappingSetting mappingSetting)
    {
      if (mappingSetting.MappingJson != null)
      {
        return JsonConvert.DeserializeObject<Dictionary<string, List<MappingValue>>>(mappingSetting.MappingJson);
      }
      return null;
    }

    public static TypeMap? DeserializeMapping(MappingSetting mappingSetting)
    {
      if (mappingSetting.MappingJson != null)
      {
        var settings = new JsonSerializerSettings
        {
          Converters = { 
            new AbstractConverter<RevitMappingValue, ISingleValueToMap>(),
            new AbstractConverter<RevitHostType, ISingleHostType>(),
          },
        };
        try
        {
          return JsonConvert.DeserializeObject<TypeMap>(mappingSetting.MappingJson, settings);
        }
        catch
        {
          // couldn't deserialize so just return null
        }
      }
      return null;
    }

    /// <summary>
    /// Gets the most similar host type of the same category for a single incoming type
    /// </summary>
    /// <param name="elementTypes"></param>
    /// <param name="category"></param>
    /// <param name="speckleType"></param>
    /// <returns></returns>
    private static ISingleHostType GetMappedValue(IEnumerable<ElementType> elementTypes, string category, string speckleType)
    {
      var shortestDistance = int.MaxValue;
      var closestFamily = string.Empty;
      var closestType = $"No families of the category \"{category}\" are loaded into the project";

      foreach (var elementType in elementTypes)
      {
        var distance = LevenshteinDistance(speckleType, elementType.Name);
        if (distance < shortestDistance)
        {
          shortestDistance = distance;
          closestFamily = elementType.FamilyName;
          closestType = elementType.Name;
        }
      }

      return new RevitHostType(closestFamily, closestType);
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

  public class AbstractConverter<TReal, TAbstract> : JsonConverter where TReal : TAbstract
  {
    public override bool CanConvert(Type objectType)
        => objectType == typeof(TAbstract);

    public override object ReadJson(JsonReader reader, Type type, Object value, JsonSerializer jser)
        => jser.Deserialize<TReal>(reader);

    public override void WriteJson(JsonWriter writer, Object value, JsonSerializer jser)
        => jser.Serialize(writer, value);
  }
}
