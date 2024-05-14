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
using RevitSharedResources.Extensions.SpeckleExtensions;
using RevitSharedResources.Interfaces;
using Speckle.ConnectorRevit.UI;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;
using Speckle.Newtonsoft.Json;
using DB = Autodesk.Revit.DB;

namespace ConnectorRevit.TypeMapping;

/// <summary>
/// Responsible for parsing data received from Speckle for necessary <see cref="DB.ElementType"/>s, and then querying the current Revit document for <see cref="DB.ElementType"/>s that exist in the Project. This class then stores all of the data that it finds in an <see cref="ITypeMap"/> object that can be passed to DUI for user mapping.
/// </summary>
internal sealed class ElementTypeMapper
{
  private readonly IAllRevitCategoriesExposer revitCategoriesExposer;
  private readonly IRevitElementTypeRetriever typeRetriever;
  private readonly IRevitDocumentAggregateCache revitDocumentAggregateCache;
  private List<Base> speckleElements = new();
  private readonly Document document;

  /// <summary>
  /// Initialize ElementTypeMapper. Will throw if the provided <see cref="ISpeckleConverter"/> does not also implement <see cref="IRevitElementTypeRetriever"/> and <see cref="IElementTypeInfoExposer"/>
  /// </summary>
  /// <param name="converter"></param>
  /// <param name="flattenedCommit"></param>
  /// <param name="storedObjects"></param>
  /// <param name="doc"></param>
  /// <exception cref="SpeckleException"></exception>
  public ElementTypeMapper(
    ISpeckleConverter converter,
    IRevitDocumentAggregateCache revitDocumentAggregateCache,
    List<ApplicationObject> flattenedCommit,
    Dictionary<string, Base> storedObjects,
    Document doc
  )
  {
    document = doc;

    if (converter is not IRevitElementTypeRetriever typeRetriever)
    {
      throw new SpeckleException($"Converter does not implement interface {nameof(IRevitElementTypeRetriever)}");
    }
    else
    {
      this.typeRetriever = typeRetriever;
    }

    if (converter is not IAllRevitCategoriesExposer typeInfoExposer)
    {
      throw new SpeckleException($"Converter does not implement interface {nameof(IRevitElementTypeRetriever)}");
    }
    else
    {
      revitCategoriesExposer = typeInfoExposer;
    }

    this.revitDocumentAggregateCache =
      revitDocumentAggregateCache ?? throw new SpeckleException($"RevitDocumentAggregateCache cannot be null");

    var traversalFunc = DefaultTraversal.CreateTraverseFunc(converter);
    foreach (var appObj in flattenedCommit)
    {
      // add base and traverse nested elements
      speckleElements.AddRange(
        traversalFunc
          .Traverse(storedObjects[appObj.OriginalId])
          .Select(c => c.current)
          .Where(converter.CanConvertToNative)
          .OfType<Base>()
      );
    }
  }

  public async Task Map(ISetting mapOnReceiveSetting, ISetting directShapeStrategySetting)
  {
    // Get Settings for recieve on mapping
    if (
      mapOnReceiveSetting is not MappingSetting mappingSetting
      || mappingSetting.Selection == ConnectorBindingsRevit.noMapping
      //skip mappings dialog always when DS fallback is set to always
      || directShapeStrategySetting.Selection == ConnectorBindingsRevit.DsFallbackAways
    )
    {
      return;
    }

    var masterTypeMap = DeserializeMapping(mappingSetting, out var previousMappingExists) ?? new TypeMap();
    var currentOperationTypeMap = new TypeMap();

    var hostTypesContainer = GetHostTypesAndAddIncomingTypes(
      currentOperationTypeMap,
      masterTypeMap,
      out var numNewTypes
    );

    if (await ShouldShowCustomMappingDialog(mappingSetting.Selection, numNewTypes).ConfigureAwait(false))
    {
      // show custom mapping dialog if the settings correspond to what is being received
      await ShowCustomMappingDialog(currentOperationTypeMap, hostTypesContainer, numNewTypes).ConfigureAwait(false);

      // close the dialog
      MainViewModel.CloseDialog();

      masterTypeMap.AddTypeMap(currentOperationTypeMap);
      mappingSetting.MappingJson = JsonConvert.SerializeObject(masterTypeMap);
    }
    else if (!previousMappingExists)
    {
      // if we're not showing the user the custom mapping dialog and the user doesn't have an existing mapping
      // then we're done here
      return;
    }

    // update the mapping object for the user mapped types
    SetMappedValues(typeRetriever, currentOperationTypeMap);

    Analytics.TrackEvent(
      Analytics.Events.DUIAction,
      new Dictionary<string, object> { { "name", "Type Map" }, { "method", "Mappings Set" } }
    );
  }

  private async Task<bool> ShouldShowCustomMappingDialog(string listBoxSelection, int numNewTypes)
  {
    if (listBoxSelection == ConnectorBindingsRevit.everyReceive)
    {
      return true;
    }
    else if (
      listBoxSelection == ConnectorBindingsRevit.forNewTypes
      && numNewTypes > 0
      && await ShowMissingIncomingTypesDialog().ConfigureAwait(false)
    )
    {
      return true;
    }
    return false;
  }

  private static async Task<bool> ShowMissingIncomingTypesDialog()
  {
    var response = await Dispatcher.UIThread
      .InvokeAsync<bool>(() =>
      {
        Analytics.TrackEvent(
          Analytics.Events.DUIAction,
          new Dictionary<string, object> { { "name", "Type Map" }, { "method", "Missing Types Dialog" } }
        );
        var mappingView = new MissingIncomingTypesDialog();
        return mappingView.ShowDialog<bool>();
        ;
      })
      .ConfigureAwait(false);

    if (response)
    {
      Analytics.TrackEvent(
        Analytics.Events.DUIAction,
        new Dictionary<string, object> { { "name", "Type Map" }, { "method", "Dialog Accept" } }
      );
    }
    else
    {
      Analytics.TrackEvent(
        Analytics.Events.DUIAction,
        new Dictionary<string, object> { { "name", "Type Map" }, { "method", "Dialog Ignore" } }
      );
    }

    return response;
  }

  private async Task ShowCustomMappingDialog(
    TypeMap? currentMapping,
    HostTypeContainer hostTypesContainer,
    int numNewTypes
  )
  {
    var vm = new TypeMappingOnReceiveViewModel(currentMapping, hostTypesContainer, numNewTypes == 0);
    FamilyImporter familyImporter = null;

    await Dispatcher.UIThread
      .InvokeAsync<ITypeMap>(() =>
      {
        var mappingView = new MappingViewDialog { DataContext = vm };
        return mappingView.ShowDialog<ITypeMap>();
      })
      .ConfigureAwait(false);

    while (vm.DoneMapping == false)
    {
      try
      {
        familyImporter ??= new FamilyImporter(
          document,
          revitCategoriesExposer,
          typeRetriever,
          revitDocumentAggregateCache
        );
        await familyImporter.ImportFamilyTypes(hostTypesContainer).ConfigureAwait(false);
      }
      catch (SpeckleException ex)
      {
        StreamViewModel.HandleCommandException(ex, false, "ImportTypesCommand");
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        SpeckleLog.Logger.LogDefaultError(ex);
        var speckleEx = new SpeckleException(ex.Message, ex);
        StreamViewModel.HandleCommandException(speckleEx, false, "ImportTypesCommand");
      }

      vm = new TypeMappingOnReceiveViewModel(currentMapping, hostTypesContainer, numNewTypes == 0);
      await Dispatcher.UIThread
        .InvokeAsync<ITypeMap>(() =>
        {
          var mappingView = new MappingViewDialog { DataContext = vm };
          return mappingView.ShowDialog<ITypeMap>();
        })
        .ConfigureAwait(false);
    }
  }

  private static void SetMappedValues(IRevitElementTypeRetriever typeRetriever, TypeMap currentMapping)
  {
    foreach (var (@base, mappingValue) in currentMapping.GetAllBasesWithMappings())
    {
      var mappedHostType = mappingValue.MappedHostType ?? mappingValue.InitialGuess;
      if (mappedHostType == null)
      {
        continue;
      }

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
  /// <param name="currentTypeMap"></param>
  /// <param name="numNewTypes"></param>
  /// <returns></returns>
  public HostTypeContainer GetHostTypesAndAddIncomingTypes(
    TypeMap currentTypeMap,
    TypeMap masterTypeMap,
    out int numNewTypes
  )
  {
    var hostTypes = new HostTypeContainer();

    numNewTypes = 0;
    var groupedElementTypeCache = revitDocumentAggregateCache.GetOrInitializeWithDefaultFactory<List<ElementType>>();
    var elementTypeCache = revitDocumentAggregateCache.GetOrInitializeWithDefaultFactory<ElementType>();

    // add all element types from the document to the cache
    groupedElementTypeCache.GetOrAddGroupOfTypes(RevitSharedResources.Helpers.Categories.Undefined);

    foreach (var @base in speckleElements)
    {
      var incomingType = typeRetriever.GetElementType(@base);
      if (incomingType == null)
      {
        SpeckleLog.Logger.Warning(
          "Could not find incoming type on Base of type {baseType} with speckle_type {speckleType}",
          @base.GetType(),
          @base.speckle_type
        );
        continue;
      }

      var incomingFamily = typeRetriever.GetElementFamily(@base);

      var typeInfo = revitCategoriesExposer.AllCategories.GetRevitCategoryInfo(@base);
      //if (typeInfo.ElementTypeType == null) continue;

      var elementTypes = groupedElementTypeCache.GetOrAddGroupOfTypes(typeInfo);
      var exactTypeMatch = elementTypeCache.ContainsKey(typeInfo.GetCategorySpecificTypeName(incomingType));

      hostTypes.AddCategoryWithTypesIfCategoryIsNew(
        typeInfo.CategoryName,
        elementTypes.Select(type => new RevitHostType(type.FamilyName, type.Name))
      );

      var mappedValue = GetExistingMappedValue(masterTypeMap, incomingFamily, incomingType, typeInfo.CategoryName);

      if (mappedValue == null)
      {
        mappedValue = GetMappedValueGuess(elementTypes, typeInfo.CategoryName, incomingType);

        // if the neither the document nor the masterTypeMap contain a matching type, then it is new
        if (!exactTypeMatch)
        {
          numNewTypes++;
        }
      }

      currentTypeMap.AddIncomingType(@base, incomingType, incomingFamily, typeInfo.CategoryName, mappedValue);
    }

    hostTypes.SetAllTypes(
      elementTypeCache.GetAllObjects().Select(type => new RevitHostType(type.FamilyName, type.Name))
    );

    return hostTypes;
  }

  private static ISingleHostType? GetExistingMappedValue(
    TypeMap typeMap,
    string? incomingFamily,
    string incomingType,
    string category
  )
  {
    var existingMappingValue = typeMap.TryGetMappingValueInCategory(category, incomingFamily, incomingType);

    if (existingMappingValue != null && existingMappingValue.InitialGuess != null)
    {
      return existingMappingValue.MappedHostType ?? existingMappingValue.InitialGuess;
    }
    return null;
  }

  public static TypeMap? DeserializeMapping(MappingSetting mappingSetting, out bool previousMappingExists)
  {
    if (mappingSetting.MappingJson != null)
    {
      var settings = new JsonSerializerSettings
      {
        Converters =
        {
          new AbstractConverter<RevitMappingValue, ISingleValueToMap>(),
          new AbstractConverter<RevitHostType, ISingleHostType>(),
        },
      };
      try
      {
        previousMappingExists = true;
        return JsonConvert.DeserializeObject<TypeMap>(mappingSetting.MappingJson, settings);
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        SpeckleLog.Logger.LogDefaultError(ex);
      }
    }
    previousMappingExists = false;
    return null;
  }

  /// <summary>
  /// Gets the most similar host type of the same category for a single incoming type
  /// </summary>
  /// <param name="elementTypes"></param>
  /// <param name="category"></param>
  /// <param name="incomingType"></param>
  /// <returns></returns>
  private static ISingleHostType GetMappedValueGuess(
    IEnumerable<ElementType> elementTypes,
    string category,
    string incomingType
  )
  {
    var shortestDistance = int.MaxValue;
    var closestFamily = string.Empty;
    var closestType = $"No families of the category \"{category}\" are loaded into the project";

    foreach (var elementType in elementTypes)
    {
      var distance = LevenshteinDistance(incomingType, elementType.Name);
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
    {
      ;
    }

    for (int j = 0; j <= m; d[0, j] = j++)
    {
      ;
    }

    for (int i = 1; i <= n; i++)
    {
      for (int j = 1; j <= m; j++)
      {
        int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
        d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
      }
    }
    return d[n, m];
  }
}

public class AbstractConverter<TReal, TAbstract> : JsonConverter
  where TReal : TAbstract
{
  public override bool CanConvert(Type objectType) => objectType == typeof(TAbstract);

  public override object ReadJson(JsonReader reader, Type type, Object value, JsonSerializer jser) =>
    jser.Deserialize<TReal>(reader);

  public override void WriteJson(JsonWriter writer, Object value, JsonSerializer jser) => jser.Serialize(writer, value);
}
