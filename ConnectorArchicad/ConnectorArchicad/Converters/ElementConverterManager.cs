using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Archicad.Communication;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Objects.BuiltElements.Archicad;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Beam = Objects.BuiltElements.Beam;
using Ceiling = Objects.BuiltElements.Ceiling;
using Column = Objects.BuiltElements.Column;
using Door = Objects.BuiltElements.Archicad.ArchicadDoor;
using Fenestration = Objects.BuiltElements.Archicad.ArchicadFenestration;
using Floor = Objects.BuiltElements.Floor;
using GridLine = Objects.BuiltElements.GridLine;
using Opening = Objects.BuiltElements.Opening;
using Roof = Objects.BuiltElements.Roof;
using Skylight = Objects.BuiltElements.Archicad.ArchicadSkylight;
using Wall = Objects.BuiltElements.Wall;
using Window = Objects.BuiltElements.Archicad.ArchicadWindow;

namespace Archicad;

public sealed partial class ElementConverterManager
{
  #region --- Fields ---

  public static ElementConverterManager Instance { get; } = new();

  private Dictionary<Type, Converters.IConverter> Converters { get; } = new();

  private Converters.IConverter DefaultConverterForSend { get; } = new Converters.DirectShape();
  private Converters.IConverter DefaultConverterForReceive { get; } = new Converters.Object();

  private Dictionary<Type, IEnumerable<Base>> ReceivedObjects { get; set; }
  private Dictionary<string, IEnumerable<string>> SelectedObjects { get; set; }

  private List<string> CanHaveSubElements = new() { "Wall", "Roof", "Shell" }; // Hardcoded until we know whats the shared property that defines wether elements may be have subelements or not.
  #endregion

  #region --- Ctor \ Dtor ---

  private ElementConverterManager()
  {
    RegisterConverters();
  }

  #endregion

  #region --- Functions ---

  public async Task<Base?> ConvertToSpeckle(StreamState state, ProgressViewModel progress)
  {
    var objectToCommit = new Collection("Archicad model", "model");

    var conversionOptions = new ConversionOptions(state.Settings);

    IEnumerable<string> elementIds = state.Filter.Selection;
    if (state.Filter.Slug == "all")
    {
      elementIds = AsyncCommandProcessor
        .Execute(new Communication.Commands.GetElementIds(Communication.Commands.GetElementIds.ElementFilter.All))
        ?.Result;
    }
    else if (state.Filter.Slug == "elementType")
    {
      var elementTypes = state.Filter.Summary.Split(",").Select(elementType => elementType.Trim()).ToList();
      elementIds = AsyncCommandProcessor
        .Execute(
          new Communication.Commands.GetElementIds(
            Communication.Commands.GetElementIds.ElementFilter.ElementType,
            elementTypes
          )
        )
        ?.Result;
    }

    SelectedObjects = await GetElementsType(elementIds, progress.CancellationToken); // Gets all selected objects
    SelectedObjects = SortSelectedObjects();

    SpeckleLog.Logger.Debug("Conversion started (element types: {0})", SelectedObjects.Count);

    progress.Max = SelectedObjects.Sum(x => x.Value.Count());
    progress.Value = 0;

    List<Base> allObjects = new();
    foreach (var (element, guids) in SelectedObjects) // For all kind of selected objects (like window, door, wall, etc.)
    {
      SpeckleLog.Logger.Debug("{0}: {1}", element, guids.Count());

      Type elemenType = ElementTypeProvider.GetTypeByName(element);
      var objects = await ConvertOneTypeToSpeckle(guids, elemenType, progress, conversionOptions); // Deserialize all objects with given type
      allObjects.AddRange(objects);

      // subelements translated into "elements" property of the parent
      if (typeof(Fenestration).IsAssignableFrom(elemenType) || typeof(Opening).IsAssignableFrom(elemenType))
      {
        Collection elementCollection = null;

        foreach (Base item in objects)
        {
          string parentApplicationId = null;

          if (item is Fenestration fenestration)
          {
            parentApplicationId = fenestration.parentApplicationId;
          }
          else if (item is ArchicadOpening opening)
          {
            parentApplicationId = opening.parentApplicationId;
          }

          Base parent = allObjects.Find(x => x.applicationId == parentApplicationId);

          if (parent == null)
          {
            // parent skipped, so add to collection
            if (elementCollection == null)
            {
              elementCollection = new Collection(element, "Element Type");
              elementCollection.applicationId = element;
              objectToCommit.elements.Add(elementCollection);
            }

            elementCollection.elements.Add(item);
          }
          else
          {
            if (parent["elements"] == null)
            {
              parent["elements"] = new List<Base>() { item };
            }
            else
            {
              var elements = parent["elements"] as List<Base>;
              elements.Add(item);
            }
          }
        }
      }
      // parents translated as new collections
      else
      {
        Collection elementCollection = new(element, "Element Type");
        elementCollection.applicationId = element;
        elementCollection.elements = objects;
        objectToCommit.elements.Add(elementCollection);
      }
    }

    SpeckleLog.Logger.Debug("Conversion done");

    return objectToCommit;
  }

  Dictionary<string, IEnumerable<string>> SortSelectedObjects()
  {
    var retval = new Dictionary<string, IEnumerable<string>>();
    var canHave = SelectedObjects.Where(e => CanHaveSubElements.Contains(e.Key));
    var cannotHave = SelectedObjects.Where(e => !CanHaveSubElements.Contains(e.Key));

    foreach (var (key, value) in canHave)
    {
      retval[key] = value;
    }

    foreach (var (key, value) in cannotHave)
    {
      retval[key] = value;
    }

    return retval;
  }

  private void RegisterConverters()
  {
    IEnumerable<Type> convertes = Assembly
      .GetExecutingAssembly()
      .GetTypes()
      .Where(t => t.IsClass && !t.IsAbstract && typeof(Converters.IConverter).IsAssignableFrom(t));

    foreach (Type converterType in convertes)
    {
      var converter = Activator.CreateInstance(converterType) as Converters.IConverter;
      if (converter?.Type is null)
      {
        continue;
      }

      Converters.Add(converter.Type, converter);
    }
  }

  public Converters.IConverter GetConverterForElement(
    Type elementType,
    ConversionOptions conversionOptions,
    bool forReceive
  )
  {
    if (forReceive)
    {
      // always convert to Archicad GridElement
      if (elementType.IsAssignableFrom(typeof(GridLine)))
      {
        return Converters[typeof(Archicad.GridElement)];
      }

      if (conversionOptions != null && !conversionOptions.ReceiveParametric)
      {
        return DefaultConverterForReceive;
      }
    }

    if (Converters.ContainsKey(elementType))
    {
      return Converters[elementType];
    }

    if (elementType.IsSubclassOf(typeof(Wall)))
    {
      return Converters[typeof(Wall)];
    }

    if (elementType.IsSubclassOf(typeof(Beam)))
    {
      return Converters[typeof(Beam)];
    }

    if (elementType.IsSubclassOf(typeof(Column)))
    {
      return Converters[typeof(Column)];
    }

    if (elementType.IsSubclassOf(typeof(Door)))
    {
      return Converters[typeof(Door)];
    }

    if (elementType.IsSubclassOf(typeof(Window)))
    {
      return Converters[typeof(Window)];
    }

    if (elementType.IsSubclassOf(typeof(Skylight)))
    {
      return Converters[typeof(Skylight)];
    }

    if (elementType.IsSubclassOf(typeof(Floor)) || elementType.IsSubclassOf(typeof(Ceiling)))
    {
      return Converters[typeof(Floor)];
    }

    if (elementType.IsSubclassOf(typeof(Roof)))
    {
      return Converters[typeof(Roof)];
    }

    if (elementType.IsSubclassOf(typeof(Opening)))
    {
      return Converters[typeof(Opening)];
    }

    if (elementType.IsAssignableFrom(typeof(Objects.BuiltElements.Room)))
    {
      return Converters[typeof(Archicad.Room)];
    }

    if (elementType.IsAssignableFrom(typeof(Archicad.GridElement)))
    {
      return Converters[typeof(Archicad.GridElement)];
    }

    return forReceive ? DefaultConverterForReceive : DefaultConverterForSend;
  }

  #endregion

  private async Task<Dictionary<string, IEnumerable<string>>?> GetElementsType(
    IEnumerable<string> applicationIds,
    CancellationToken token
  )
  {
    var retval = await AsyncCommandProcessor.Execute(new Communication.Commands.GetElementsType(applicationIds), token);
    return retval;
  }

  public async Task<List<Base>?> ConvertOneTypeToSpeckle(
    IEnumerable<string> applicationIds,
    Type elementType,
    ProgressViewModel progress,
    ConversionOptions conversionOptions
  )
  {
    var rawModels = await GetModelForElements(applicationIds, progress.CancellationToken); // Model data, like meshes
    var elementConverter = ElementConverterManager.Instance.GetConverterForElement(elementType, null, false); // Object converter
    var convertedObjects = await elementConverter.ConvertToSpeckle(
      rawModels,
      progress.CancellationToken,
      conversionOptions
    ); // Deserialization

    foreach (Base convertedObject in convertedObjects)
    {
      ApplicationObject applicationObject = new(convertedObject.applicationId, elementType.Name);
      applicationObject.Update(status: ApplicationObject.State.Created);

      progress.Report.Log(applicationObject);
    }

    progress.Value = progress.Value + convertedObjects.Count();

    return convertedObjects;
  }

  private async Task<IEnumerable<Model.ElementModelData>> GetModelForElements(
    IEnumerable<string> applicationIds,
    CancellationToken token
  )
  {
    var retval = await AsyncCommandProcessor.Execute(
      new Communication.Commands.GetModelForElements(applicationIds),
      token
    );
    return retval;
  }
}
