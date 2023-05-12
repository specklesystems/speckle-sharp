using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Archicad.Communication;
using Avalonia.Controls;
using DesktopUI2.Models.Filters;
using DesktopUI2.ViewModels;
using Objects.BuiltElements.Archicad;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;
using Beam = Objects.BuiltElements.Beam;
using Ceiling = Objects.BuiltElements.Ceiling;
using Column = Objects.BuiltElements.Column;
using Door = Objects.BuiltElements.Archicad.ArchicadDoor;
using Floor = Objects.BuiltElements.Floor;
using Roof = Objects.BuiltElements.Roof;
using Room = Objects.BuiltElements.Archicad.ArchicadRoom;
using Wall = Objects.BuiltElements.Wall;
using Window = Objects.BuiltElements.Archicad.ArchicadWindow;
using Skylight = Objects.BuiltElements.Archicad.ArchicadSkylight;

namespace Archicad
{
  public sealed partial class ElementConverterManager
  {
    #region --- Fields ---

    public static ElementConverterManager Instance { get; } = new();

    private Dictionary<Type, Converters.IConverter> Converters { get; } = new();

    private Converters.IConverter DefaultConverterForSend { get; } = new Converters.DirectShape();
    private Converters.IConverter DefaultConverterForReceive { get; } = new Converters.Object();

    private Dictionary<Type, IEnumerable<Base>> ReceivedObjects { get; set; }
    private Dictionary<string, IEnumerable<string>> SelectedObjects { get; set; }

    private List<string> CanHaveSubElements = new List<string> { "Wall", "Roof", "Shell" }; // Hardcoded until we know whats the shared property that defines wether elements may be have subelements or not.
    #endregion

    #region --- Ctor \ Dtor ---

    private ElementConverterManager()
    {
      RegisterConverters();
    }

    #endregion

    #region --- Functions ---

    public async Task<Base?> ConvertToSpeckle(ISelectionFilter filter, ProgressViewModel progress)
    {
      var objectToCommit = new Base();

      IEnumerable<string> elementIds = filter.Selection;
      if (filter.Slug == "all")
        elementIds = AsyncCommandProcessor
          .Execute(new Communication.Commands.GetElementIds(Communication.Commands.GetElementIds.ElementFilter.All))
          ?.Result;

      SelectedObjects = await GetElementsType(elementIds, progress.CancellationToken); // Gets all selected objects
      SelectedObjects = SortSelectedObjects();

      SpeckleLog.Logger.Debug("Conversion started (element types: {0})", SelectedObjects.Count);

      foreach (var (element, guids) in SelectedObjects) // For all kind of selected objects (like window, door, wall, etc.)
      {
        SpeckleLog.Logger.Debug("{0}: {1}", element, guids.Count());

        var objects = await ConvertOneTypeToSpeckle(
          guids,
          ElementTypeProvider.GetTypeByName(element),
          progress.CancellationToken
        ); // Deserialize all objects with hiven type
        if (objects.Count() > 0)
        {
          objectToCommit["@" + element] = objects; // Save 'em. Assigned objects are parents with subelements

          // itermediate solution for the OneClick Send report
          for (int i = 0; i < objects.Count(); i++)
            if (!progress.Report.ReportObjects.ContainsKey(objects[i].applicationId))
              progress.Report.ReportObjects.Add(objects[i].applicationId, new ApplicationObject("", ""));
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
          continue;

        Converters.Add(converter.Type, converter);
      }
    }

    public Converters.IConverter GetConverterForElement(
      Type elementType,
      ConversionOptions conversionOptions,
      bool forReceive
    )
    {
      if (forReceive && conversionOptions != null && !conversionOptions.ReceiveParametric)
        return DefaultConverterForReceive;

      if (Converters.ContainsKey(elementType))
        return Converters[elementType];
      if (elementType.IsSubclassOf(typeof(Wall)))
        return Converters[typeof(Wall)];
      if (elementType.IsSubclassOf(typeof(Beam)))
        return Converters[typeof(Beam)];
      if (elementType.IsSubclassOf(typeof(Column)))
        return Converters[typeof(Column)];
      if (elementType.IsSubclassOf(typeof(Door)))
        return Converters[typeof(Door)];
      if (elementType.IsSubclassOf(typeof(Window)))
        return Converters[typeof(Window)];
      if (elementType.IsSubclassOf(typeof(Skylight)))
        return Converters[typeof(Skylight)];
      if (elementType.IsSubclassOf(typeof(Floor)) || elementType.IsSubclassOf(typeof(Ceiling)))
        return Converters[typeof(Floor)];
      if (elementType.IsSubclassOf(typeof(Roof)))
        return Converters[typeof(Roof)];
      if (elementType.IsSubclassOf(typeof(Objects.BuiltElements.Room)))
        return Converters[typeof(Objects.BuiltElements.Room)];

      return forReceive ? DefaultConverterForReceive : DefaultConverterForSend;
    }

    #endregion

    private async Task<Dictionary<string, IEnumerable<string>>?> GetElementsType(
      IEnumerable<string> applicationIds,
      CancellationToken token
    )
    {
      var retval = await AsyncCommandProcessor.Execute(
        new Communication.Commands.GetElementsType(applicationIds),
        token
      );
      return retval;
    }

    public async Task<List<Base>?> ConvertOneTypeToSpeckle(
      IEnumerable<string> applicationIds,
      Type elementType,
      CancellationToken token
    )
    {
      var rawModels = await GetModelForElements(applicationIds, token); // Model data, like meshes
      var elementConverter = ElementConverterManager.Instance.GetConverterForElement(elementType, null, false); // Object converter
      var convertedObjects = await elementConverter.ConvertToSpeckle(rawModels, token); // Deserialization

      foreach (var convertedObject in convertedObjects)
      {
        var subElementsAsBases = await ConvertSubElementsToSpeckle(convertedObject, token);
        if (subElementsAsBases.Count() > 0)
        {
          convertedObject["elements"] = subElementsAsBases;
        }
      }

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

    public async Task<List<Base>?> ConvertSubElementsToSpeckle(Base convertedObject, CancellationToken token)
    {
      var subElementsAsBases = new List<Base>();

      if (convertedObject is not (Objects.BuiltElements.Archicad.ArchicadWall or Objects.BuiltElements.Archicad.ArchicadRoof or Objects.BuiltElements.Archicad.ArchicadShell))
        return subElementsAsBases;

      var subElements = await GetAllSubElements(convertedObject.applicationId);
      if (subElements.Count() == 0)
        return subElementsAsBases;

      var subElementsByGuid = await GetElementsType(subElements.Select(e => e.applicationId), token);
      var mutualSubElements = GetAllMutualSubElements(subElementsByGuid);

      foreach (var (element, guids) in mutualSubElements)
      {
        if (guids.Count() == 0)
          continue;
        var convertedSubElements = await ConvertOneTypeToSpeckle(
          guids,
          ElementTypeProvider.GetTypeByName(element),
          token
        );
        subElementsAsBases = subElementsAsBases.Concat(convertedSubElements).ToList(); // Update list with new values
      }
      RemoveSubElements(mutualSubElements); // Remove subelements from SelectedObjects (where we stored all selected objects)

      return subElementsAsBases;
    }

    private async Task<IEnumerable<SubElementData>?> GetAllSubElements(string apllicationId)
    {
      IEnumerable<SubElementData>? currentSubElements = await AsyncCommandProcessor.Execute(
        new Communication.Commands.GetSubElementInfo(apllicationId),
        CancellationToken.None
      );

      return currentSubElements;
    }

    private Dictionary<string, IEnumerable<string>> GetAllMutualSubElements(
      Dictionary<string, IEnumerable<string>> allSubElementsByGuid
    )
    {
      Dictionary<string, IEnumerable<string>> mutualSubElements = new Dictionary<string, IEnumerable<string>>();

      foreach (var (element, guids) in allSubElementsByGuid)
      {
        mutualSubElements[element] = GetMutualSubElementsByType(element, guids);
      }

      return mutualSubElements;
    }

    private IEnumerable<string> GetMutualSubElementsByType(string elementType, IEnumerable<string> applicationIds)
    {
      if (!SelectedObjects.ContainsKey(elementType))
        return new List<string>();

      return SelectedObjects[elementType].Where(guid => applicationIds.Contains(guid));
    }

    public void RemoveSubElements(Dictionary<string, IEnumerable<string>> mutualSubElements)
    {
      foreach (var (element, guids) in mutualSubElements)
      {
        if (guids.Count() == 0)
          continue;
        var guidsToKeep = SelectedObjects[element].Where(guid => !guids.Contains(guid));
        SelectedObjects[element] = guidsToKeep.ToList();
      }
    }
  }
}
