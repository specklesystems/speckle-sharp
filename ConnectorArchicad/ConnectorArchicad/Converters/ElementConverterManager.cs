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
using DesktopUI2.ViewModels;
using DesktopUI2.Models.Filters;
using Ceiling = Objects.BuiltElements.Ceiling;
using Floor = Objects.BuiltElements.Floor;
using Wall = Objects.BuiltElements.Wall;
using Beam = Objects.BuiltElements.Beam;
using Room = Objects.BuiltElements.Archicad.ArchicadRoom;
using Door = Objects.BuiltElements.Archicad.ArchicadDoor;
using Window = Objects.BuiltElements.Archicad.ArchicadWindow;

namespace Archicad
{
  public sealed class ElementConverterManager
  {
    #region --- Fields ---

    public static ElementConverterManager Instance { get; } = new();

    private Dictionary<Type, Converters.IConverter> Converters { get; } = new();

    private Converters.IConverter DefaultConverter { get; } = new Converters.DirectShape();
    private Dictionary<Type, IEnumerable<Base>> ReceivedObjects { get; set; }
    private Dictionary<string, IEnumerable<string>> SelectedObjects { get; set; }

    private List<string> CanHaveSubElements = new List<string> { "Wall" }; // Hardcoded until we know whats the shared property that defines wether elements may be have subelements or not.
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
        elementIds = AsyncCommandProcessor.Execute(new Communication.Commands.GetElementIds(Communication.Commands.GetElementIds.ElementFilter.All))?.Result;

      SelectedObjects = await GetElementsType(elementIds, progress.CancellationTokenSource.Token);  // Gets all selected objects
      SelectedObjects = SortSelectedObjects();

      foreach (var (element, guids) in SelectedObjects) // For all kind of selected objects (like window, door, wall, etc.)
      {
        var objects = await ConvertOneTypeToSpeckle(guids, ElementTypeProvider.GetTypeByName(element), progress.CancellationTokenSource.Token);  // Deserialize all objects with hiven type
        if (objects.Count() > 0)
        {
          objectToCommit["@" + element] = objects;  // Save 'em. Assigned objects are parents with subelements

          // itermediate solution for the OneClick Send report
          for (int i = 0; i < objects.Count(); i++)
            progress.Report.ReportObjects.Add(new ApplicationObject("", ""));
        }
      }

      return objectToCommit;
    }

    Dictionary<string, IEnumerable<string>> SortSelectedObjects()
    {
      var retval = new Dictionary<string, IEnumerable<string>>();
      var canHave = SelectedObjects.Where(e => CanHaveSubElements.Contains(e.Key));
      var cannotHave = SelectedObjects.Where(e => !CanHaveSubElements.Contains(e.Key));

      foreach (var (key,value) in canHave)
      {
        retval[key] = value;
      }

      foreach (var (key, value) in cannotHave)
      {
        retval[key] = value;
      }

      return retval;
    }

    public async Task<List<string>> ConvertToNative(Base obj, CancellationToken token)
    {
      var result = new List<string>();

      var falttenedElements = FlattenCommitObject(obj);
      ReceivedObjects = falttenedElements.GroupBy(element => element.GetType())
        .ToDictionary(group => group.Key, group => group.Cast<Base>());

      foreach (var (elementType, elements) in ReceivedObjects)
      {
        var convertedObjects = await ConvertOneTypeToNative(elementType, elements, token);
        result.AddRange(convertedObjects);
      }

      return result;
    }

    private void RegisterConverters()
    {
      IEnumerable<Type> convertes = Assembly.GetExecutingAssembly().GetTypes().Where(t =>
        t.IsClass && !t.IsAbstract && typeof(Converters.IConverter).IsAssignableFrom(t));

      foreach ( Type converterType in convertes )
      {
        var converter = Activator.CreateInstance(converterType) as Converters.IConverter;
        if ( converter?.Type is null )
          continue;

        Converters.Add(converter.Type, converter);
      }
    }

    public Converters.IConverter GetConverterForElement(Type elementType)
    {
      if (Converters.ContainsKey(elementType))
        return Converters[elementType];
      if (elementType.IsSubclassOf(typeof(Wall)))
        return Converters[typeof(Wall)];
      if (elementType.IsSubclassOf(typeof(Beam)))
        return Converters[typeof(Beam)];
      if (elementType.IsSubclassOf(typeof(Door)))
        return Converters[typeof(Door)];
      if (elementType.IsSubclassOf(typeof(Window)))
        return Converters[typeof(Window)];
      if (elementType.IsSubclassOf(typeof(Floor)) || elementType.IsSubclassOf(typeof(Ceiling)))
        return Converters[typeof(Floor)];
      if (elementType.IsSubclassOf(typeof(Objects.BuiltElements.Room)))
        return Converters[typeof(Objects.BuiltElements.Room)];

      return DefaultConverter;
    }

    public static bool CanConvertToNative(Base @object)
    {
      return @object
        switch
        {
          Wall _ => true,
          Beam _ => true,
          Floor _ => true,
          Ceiling _ => true,
          Room _ => true,
          DirectShape _ => true,
          Mesh _ => true,
          Door => true,
          Window => true,
          _ => false
        };
    }

    /// <summary>
    /// Recurses through the commit object and flattens it.
    /// TODO extract somewhere generic for other connectors to use?
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="converter"></param>
    /// <returns></returns>
    public List<Base> FlattenCommitObject(object obj)
    {
      List<Base> objects = new List<Base>();

      switch ( obj )
      {
        case Base @base when CanConvertToNative(@base):
          objects.Add(@base);

          return objects;
        case Base @base:
        {
          foreach ( var prop in @base.GetDynamicMembers() )
            objects.AddRange(FlattenCommitObject(@base[ prop ]));

          var specialKeys = @base.GetMembers();
          if ( specialKeys.ContainsKey("displayValue") )
            objects.AddRange(FlattenCommitObject(specialKeys[ "displayValue" ]));
          if ( specialKeys.ContainsKey("elements") ) // for built elements like roofs, walls, and floors.
            objects.AddRange(FlattenCommitObject(specialKeys[ "elements" ]));

          return objects;
        }
        case IReadOnlyList<object> list:
        {
          foreach ( var listObj in list )
            objects.AddRange(FlattenCommitObject(listObj));

          return objects;
        }
        case IDictionary dict:
        {
          foreach ( DictionaryEntry kvp in dict )
            objects.AddRange(FlattenCommitObject(kvp.Value));

          return objects;
        }
        default:
          return objects;
      }
    }
    #endregion

   
    public async Task<List<string>?> ConvertSubElementsToNative(IEnumerable<Base> subElements, CancellationToken token)
    {
      //Should add a flag to each object to sign if has subelements or not.This way
      // we could prevent unneccessary calls. (?)
      var convertedObjects = new List<string>();

      foreach (var subElement in subElements)
      {
        if (subElement == null)
          continue;
        var tmp = new List<Base>();
        tmp.Add(subElement);
        var convertedSubElements = await ConvertOneTypeToNative(subElement.GetType(), tmp, token);
        convertedObjects.AddRange(convertedSubElements);
      }

      return convertedObjects;
    }

    public async Task<List<string>?> ConvertOneTypeToNative(Type elementType, IEnumerable<Base> elements, CancellationToken token)
    {
      List<string> result = new List<string>();
      var elementConverter = GetConverterForElement(elementType);
      List<string> convertedObjects = await elementConverter.ConvertToArchicad(elements, token);

      result.AddRange(convertedObjects);

      foreach (var parentObjectId in convertedObjects)
      {

        if (!ReceivedObjects.ContainsKey(elementType)) // Check if type exist at all
          continue;
        if (ReceivedObjects[elementType].Count() == 0)
          continue;

        Base currentObj = ReceivedObjects[elementType].Where(e => e.applicationId == parentObjectId).FirstOrDefault();
        if (currentObj == null) // If no match found
          continue;
        if (!currentObj.GetMembers().ContainsKey("elements")) // If match found but doesnt have subelements
          continue;

        var subElements = currentObj["elements"] as List<Base>;

        if (subElements != null && subElements.Count() > 0)
        {
          var convertedSubElements = await ConvertSubElementsToNative(subElements, token);
          result.AddRange(convertedSubElements);
        }

      }

      return result;
    }

    private async Task<Dictionary<string, IEnumerable<string>>?> GetElementsType(IEnumerable<string> applicationIds, CancellationToken token)
    {
      var retval = await AsyncCommandProcessor.Execute(new Communication.Commands.GetElementsType(applicationIds), token);
      return retval;
    }

    public async Task<List<Base>?> ConvertOneTypeToSpeckle(IEnumerable<string> applicationIds, Type elementType, CancellationToken token)
    {
      var rawModels = await GetModelForElements(applicationIds, token); // Model data, like meshes
      var elementConverter = ElementConverterManager.Instance.GetConverterForElement(elementType);  // Object converter
      var convertedObjects = await elementConverter.ConvertToSpeckle(rawModels, token); // Deserialization

      foreach (var convertedObject in convertedObjects)
      {
        var subElementsAsBases = await ConvertSubElementsToSpeckle(convertedObject.applicationId, token);
        if (subElementsAsBases.Count() > 0)
        {
          convertedObject["elements"] = subElementsAsBases;
        }

      }

      return convertedObjects;
    }

    private async Task<IEnumerable<Model.ElementModelData>> GetModelForElements(IEnumerable<string> applicationIds, CancellationToken token)
    {
      var retval = await AsyncCommandProcessor.Execute(new Communication.Commands.GetModelForElements(applicationIds), token);
      return retval;
    }

    public async Task<List<Base>?> ConvertSubElementsToSpeckle(string applicationId, CancellationToken token)
    {
      var subElementsAsBases = new List<Base>();

      var subElements = await GetAllSubElements(applicationId);
      if (subElements.Count() == 0)
        return subElementsAsBases;

      var subElementsByGuid = await GetElementsType(subElements.Select(e => e.applicationId), token);
      var mutualSubElements = GetAllMutualSubElements(subElementsByGuid);

      foreach (var (element, guids) in mutualSubElements)
      {
        if (guids.Count() == 0)
          continue;
        var convertedSubElements = await ConvertOneTypeToSpeckle(guids, ElementTypeProvider.GetTypeByName(element), token);
        subElementsAsBases = subElementsAsBases.Concat(convertedSubElements).ToList();  // Update list with new values
      }
      RemoveSubElements(mutualSubElements); // Remove subelements from SelectedObjects (where we stored all selected objects) 

      return subElementsAsBases;
    }

    private async Task<IEnumerable<SubElementData>?> GetAllSubElements(string apllicationId)
    {
      IEnumerable<SubElementData>? currentSubElements =
        await AsyncCommandProcessor.Execute(new Communication.Commands.GetSubElementInfo(apllicationId),
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
