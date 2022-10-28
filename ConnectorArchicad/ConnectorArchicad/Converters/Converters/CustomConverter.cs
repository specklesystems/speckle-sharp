using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Archicad.Communication;
using Archicad.Model;
using Objects.BuiltElements;
using Objects.BuiltElements.Archicad;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Ceiling = Objects.BuiltElements.Ceiling;
using Floor = Objects.BuiltElements.Floor;
using Room = Objects.BuiltElements.Archicad.Room;
using Wall = Objects.BuiltElements.Wall;
using Beam = Objects.BuiltElements.Beam;
using Door = Objects.BuiltElements.Archicad.ArchicadDoor;
using Window = Objects.BuiltElements.Archicad.ArchicadWindow;
using Archicad.Converters;

namespace Archicad
{
  class CustomConverter
  {
    #region AC <- Speckle (Receive) TODO
    #endregion

    #region AC -> Speckle (Send)
    private Dictionary<Type, Converters.IConverter> Converters { get; } = new();

    public Dictionary<string, IEnumerable<string>> GuidsByElementType { get; set; }

    public async Task<Base?> ConvertAllToSpeckle(IEnumerable<string> applicationIds, CancellationToken token)
    {
      // todo @ Register once only
      RegisterConverters();
      var objectToCommit = new Base();

      GuidsByElementType = await GetElementsType(applicationIds, token);  // Gets all selected objects
      foreach (var (element, guids) in GuidsByElementType )               // For all kind of selected objects (like window, door, wall, etc.)
      {
        var objects = await ConvertOneTypeToSpeckle(guids, ElementTypeProvider.GetTypeByName(element), token);  // Deserialize all objects
        objectToCommit[element] = objects;  // Save 'em
      }

      return objectToCommit;
    }

    private void RegisterConverters()
    {
      IEnumerable<Type> convertes = Assembly.GetExecutingAssembly().GetTypes().Where(t =>
        t.IsClass && !t.IsAbstract && typeof(Converters.IConverter).IsAssignableFrom(t));

      foreach (Type converterType in convertes)
      {
        var converter = Activator.CreateInstance(converterType) as Converters.IConverter;
        if (converter?.Type is null)
          continue;

        Converters.Add(converter.Type, converter);
      }
    }

    private async Task<Dictionary<string, IEnumerable<string>>?> GetElementsType(IEnumerable<string> applicationIds, CancellationToken token)
    {
      var retval = await AsyncCommandProcessor.Execute(new Communication.Commands.GetElementsType(applicationIds), token);
      return retval;
    }

    public async Task<List<Base>?> ConvertOneTypeToSpeckle(IEnumerable<string> applicationIds,Type elementType, CancellationToken token)
    {
      var rawModels = await GetModelForElements(applicationIds, token); // Model data, like meshes
      var elementConverter = GetConverterByElement(elementType);        // Object converter
      var convertedObjects = await elementConverter.ConvertToSpeckle(rawModels, token); // Deserialization

      foreach (var convertedObject in convertedObjects)
      {
        // Should add a flag to each object to sign if has subelements or not. This way
        // we could prevent unneccessary calls. (?)
        var subElementsAsBases = await ConvertSubElementsToSpeckle(convertedObject.applicationId, token);
        if(subElementsAsBases.Count() > 0)
        {
          convertedObject["elements"] = subElementsAsBases;
        }
 
      }

      return convertedObjects;
    }
    private Converters.IConverter GetConverterByElement(Type elementType)
    {
      if (Converters.ContainsKey(elementType))
        return Converters[elementType];
      if (elementType.IsSubclassOf(typeof(Wall)))
        return Converters[typeof(Wall)];
      if (elementType.IsSubclassOf(typeof(Beam)))
        return Converters[typeof(Beam)];
      if (elementType.IsSubclassOf(typeof(Door)))
        return Converters[typeof(Door)];
      if (elementType.IsSubclassOf(typeof(Floor)) || elementType.IsSubclassOf(typeof(Ceiling)))
        return Converters[typeof(Floor)];
      if (elementType.IsSubclassOf(typeof(Objects.BuiltElements.Room)))
        return Converters[typeof(Objects.BuiltElements.Room)];

      // as default
      return new Converters.DirectShape();
    }
    private async Task<IEnumerable<Model.ElementModelData>> GetModelForElements(IEnumerable<string> applicationIds, CancellationToken token)
    {
      var retval = await AsyncCommandProcessor.Execute(new Communication.Commands.GetModelForElements(applicationIds), token);
      return retval;
    }

    public async Task<List<Base>?> ConvertSubElementsToSpeckle(string applicationId, CancellationToken token)
    {
      // Should add a flag to each object to sign if has subelements or not. This way
      // we could prevent unneccessary calls. (?)
      var subElementsAsBases = new List<Base>();

      var subElements = await GetAllSubElements(applicationId);
      // Clear
      if (subElements.Count() == 0)
        return subElementsAsBases;

      var subElementGuids = subElements.Select(e => e.applicationId);
      var allSubElementsByGuid = await GetElementsType(subElementGuids, token);
      var mutualSubElements = GetAllMutualSubElements(allSubElementsByGuid);

      foreach (var (element, guids) in mutualSubElements)
      {
        if (guids.Count() == 0)
          continue;
        var convertedSubElements = await ConvertOneTypeToSpeckle(guids, ElementTypeProvider.GetTypeByName(element), token);
        subElementsAsBases = subElementsAsBases.Concat(convertedSubElements).ToList();  // Update list with new values
        RemoveSubElements(mutualSubElements); // Remove subelements from GuidsByElementType (where we stored all selected objects) 
      }

      return subElementsAsBases;
    }

    private async Task<IEnumerable<SubelementModelData>?> GetAllSubElements(string apllicationId)
    {
      IEnumerable<SubelementModelData>? currentSubElements =
        await AsyncCommandProcessor.Execute(new Communication.Commands.GetSubelementInfo(apllicationId),
        CancellationToken.None);

      return currentSubElements;
    }

    private Dictionary<string, IEnumerable<string>> GetAllMutualSubElements(Dictionary<string, IEnumerable<string>> allSubElementsByGuid)
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
      if (!GuidsByElementType.ContainsKey(elementType))
        return new List<string>();

      var toBeDeleted = GuidsByElementType[elementType].Where(guid => applicationIds.Contains(guid));
      return GuidsByElementType[elementType].Intersect(toBeDeleted);

    }

    public void RemoveSubElements(Dictionary<string, IEnumerable<string>> mutualSubElements)
    {
      foreach (var (element, guids) in mutualSubElements)
      {
        if (guids.Count() == 0)
          continue;
        var toBeDeleted = GuidsByElementType[element].Where(guid => !guids.Contains(guid));
        GuidsByElementType[element] = GuidsByElementType[element].Intersect(toBeDeleted).ToList();
      }
    }
    #endregion

  }
}
