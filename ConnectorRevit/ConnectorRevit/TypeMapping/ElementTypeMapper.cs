#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
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
using Avalonia.Threading;
using Speckle.Core.Models.GraphTraversal;

namespace ConnectorRevit.TypeMapping
{
  internal sealed class ElementTypeMapper
  {
    private readonly IElementTypeInfoExposer<BuiltInCategory> elementTypeInfoExposer;
    private readonly IRevitElementTypeRetriever<ElementType, BuiltInCategory> typeRetriever;
    private List<Base> speckleElements = new();
    private readonly Document document;
    public ElementTypeMapper(ISpeckleConverter converter, List<ApplicationObject> flattenedCommit, Dictionary<string, Base> storedObjects, Document doc)
    {
      document = doc;

      if (converter is not IRevitElementTypeRetriever<ElementType, BuiltInCategory> typeRetriever)
      {
        throw new ArgumentException($"Converter does not implement interface {nameof(IRevitElementTypeRetriever<ElementType, BuiltInCategory>)}");
      }
      else this.typeRetriever = typeRetriever;
      
      if (converter is not IElementTypeInfoExposer<BuiltInCategory> typeInfoExposer)
      {
        throw new ArgumentException($"Converter does not implement interface {nameof(IRevitElementTypeRetriever<ElementType, BuiltInCategory>)}");
      }
      else this.elementTypeInfoExposer = typeInfoExposer;

      var traversalFunc = DefaultTraversal.CreateTraverseFunc(converter);
      foreach (var appObj in flattenedCommit)
      {
        // add base and traverse nested elements
        speckleElements.AddRange(traversalFunc.Traverse(storedObjects[appObj.OriginalId])
          .Select(c => c.current)
          .OfType<Base>()
        );
      }
    }
    public async Task Map(ISetting mapOnReceiveSetting)
    {
      // Get Settings for recieve on mapping 
      if (mapOnReceiveSetting is not MappingSeting mappingSetting
        || mappingSetting.Selection == ConnectorBindingsRevit.noMapping)
      {
        return;
      }

      var currentMapping = DeserializeMapping(mappingSetting);
      currentMapping ??= new TypeMap();

      var hostTypesContainer = GetHostTypesAndAddIncomingTypes(typeRetriever, currentMapping, out var numNewTypes);
      if (numNewTypes == 0 && mappingSetting.Selection == ConnectorBindingsRevit.forNewTypes) { return; }

      if (mappingSetting.Selection == null)
      {
        if (await Dispatcher.UIThread.InvokeAsync<bool>(() => {
          var mappingView = new MissingIncomingTypesDialog();
          return mappingView.ShowDialog<bool>();
        }).ConfigureAwait(false) == false)
        {
          return;
        }
      }

      // show custom mapping dialog if the settings corrospond to what is being received
      var vm = new TypeMappingOnReceiveViewModel(currentMapping, hostTypesContainer, numNewTypes == 0);
      FamilyImporter familyImporter = null;

      currentMapping = await Dispatcher.UIThread.InvokeAsync<ITypeMap>(() => {
        var mappingView = new MappingViewDialog
        {
          DataContext = vm
        };
        return mappingView.ShowDialog<ITypeMap>();
      }).ConfigureAwait(false);

      while (vm.DoneMapping == false)
      {
        familyImporter ??= new FamilyImporter(document, elementTypeInfoExposer, typeRetriever);
        await familyImporter.ImportFamilyTypes(hostTypesContainer).ConfigureAwait(false);

        vm = new TypeMappingOnReceiveViewModel(currentMapping, hostTypesContainer, numNewTypes == 0);
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

    private static void SetMappedValues(IRevitElementTypeRetriever<ElementType, BuiltInCategory> typeRetriever, ITypeMap currentMapping)
    {
      foreach (var (@base, mappingValue) in currentMapping.GetAllBasesWithMappings())
      {
        typeRetriever.SetElementType(@base, mappingValue.OutgoingType ?? mappingValue.InitialGuess);
      }
    }

    public HostTypeAsStringContainer GetHostTypesAndAddIncomingTypes(IRevitElementTypeRetriever<ElementType, BuiltInCategory> typeRetriever, ITypeMap typeMap, out int numNewTypes)
    {
      var incomingTypes = new Dictionary<string, List<ISingleValueToMap>>();
      var hostTypes = new HostTypeAsStringContainer();

      numNewTypes = 0;
      foreach (var @base in speckleElements)
      {
        var incomingType = typeRetriever.GetElementType(@base);
        if (incomingType == null) continue; // TODO: do we want to throw an error (or at least log it)

        var typeInfo = elementTypeInfoExposer.GetRevitTypeInfo(@base);
        if (typeInfo.ElementTypeType == null) continue;

        var elementTypes = typeRetriever.GetOrAddAvailibleTypes(typeInfo);
        var exactTypeMatch = typeRetriever.CacheContainsTypeWithName(typeInfo.CategoryName, incomingType);

        if (exactTypeMatch) continue;

        hostTypes.AddCategoryWithTypesIfCategoryIsNew(typeInfo.CategoryName, elementTypes.Select(type => type.Name));
        string initialGuess = DefineInitialGuess(typeMap, incomingType, typeInfo.CategoryName, elementTypes);

        typeMap.AddIncomingType(@base, incomingType, typeInfo.CategoryName, initialGuess, out var isNewType);
        if (isNewType) numNewTypes++;
      }

      hostTypes.SetAllTypes(
        typeRetriever
          .GetAllCachedElementTypes()
          .Select(type => type.Name)
      );

      return hostTypes;
    }

    private static string DefineInitialGuess(ITypeMap typeMap, string incomingType, string category, IEnumerable<ElementType> elementTypes)
    {
      var existingMappingValue = typeMap.TryGetMappingValueInCategory(category, incomingType);
      string initialGuess;

      if (existingMappingValue != null &&
        (existingMappingValue.InitialGuess != null ||
        existingMappingValue.OutgoingType != null))
      {
        initialGuess = existingMappingValue.OutgoingType ?? existingMappingValue.InitialGuess;
        existingMappingValue.InitialGuess = initialGuess;
        existingMappingValue.OutgoingType = null;
      }
      else
      {
        initialGuess = GetMappedValue(elementTypes, category, incomingType);
      }

      return initialGuess;
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
        var settings = new JsonSerializerSettings
        {
          Converters = { new AbstractConverter<MappingValue, ISingleValueToMap>() },
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
