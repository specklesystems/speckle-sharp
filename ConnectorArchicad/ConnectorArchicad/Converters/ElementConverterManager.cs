using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Archicad.Communication;
using Objects.BuiltElements.Archicad;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Ceiling = Objects.BuiltElements.Ceiling;
using Wall = Objects.BuiltElements.Wall;

namespace Archicad
{
  public sealed class ElementConverterManager
  {
    #region --- Fields ---

    public static ElementConverterManager Instance { get; } = new ElementConverterManager();

    private Dictionary<Type, Converters.IConverter> Converters { get; } = new Dictionary<Type, Converters.IConverter>();

    private Converters.IConverter DefaultConverter { get; } = new Converters.DirectShape();

    #endregion

    #region --- Ctor \ Dtor ---

    private ElementConverterManager()
    {
      RegisterConverters();
    }

    #endregion

    #region --- Functions ---

    public async Task<Base?> ConvertToSpeckle(IEnumerable<string> elementIds, CancellationToken token)
    {
      IEnumerable<Model.ElementModelData> rawModels =
        await AsyncCommandProcessor.Execute(new Communication.Commands.GetModelForElements(elementIds), token);
      if (rawModels is null)
        return null;

      Dictionary<Type, IEnumerable<string>> elementTypeTable =
        await AsyncCommandProcessor.Execute(new Communication.Commands.GetElementsType(elementIds), token);
      if (elementTypeTable is null)
        return null;

      var elements = new List<Base>();
      foreach (var(key, value)in elementTypeTable)
      {
        var converter = GetConverterForElement(key);
        List<Base> convertedElements =
          await converter.ConvertToSpeckle(rawModels.Where(model => value.Contains(model.elementId)), token);
        elements.AddRange(convertedElements);
      }

      if (elements.Count == 0)
        return null;

      var commitObject = new Base
      {
        ["@Elements"] = elements
      };

      return commitObject;
    }

    public async Task<List<string>> ConvertToNative(Base obj, CancellationToken token)
    {
      List<string> result = new List<string>();

      var elements = FlattenCommitObject(obj);
      Console.WriteLine(String.Join(", ", elements.Select(el => $"{el.speckle_type}: {el.id}")));

      var elementTypeTable = elements.GroupBy(element => element.GetType())
        .ToDictionary(group => group.Key, group => group.Cast<Base>());
      foreach (var(key, value)in elementTypeTable)
      {
        var converter = GetConverterForElement(key);
        List<string> convertedElementIds = await converter.ConvertToArchicad(value, token);
        result.AddRange(convertedElementIds);
      }

      return result;
    }

    private void RegisterConverters()
    {
      IEnumerable<Type> convertes = Assembly.GetExecutingAssembly().GetTypes().Where(t =>
        t.IsClass && !t.IsAbstract && typeof(Converters.IConverter).IsAssignableFrom(t));

      foreach (Type converterType in convertes)
      {
        var converter = Activator.CreateInstance(converterType)as Converters.IConverter;
        if (converter?.Type is null)
          continue;

        Converters.Add(converter.Type, converter);
      }
    }

    private Converters.IConverter GetConverterForElement(Type elementType)
    {
      return Converters.ContainsKey(elementType) ? Converters[elementType] : DefaultConverter;
    }

    public bool CanConvertToNative(Base @object)
    {
      return @object
      switch
      {
        Wall _ => true,
          Ceiling _ => true,
          DirectShape _ => true,
          Mesh _ => true,
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
    private List<Base> FlattenCommitObject(object obj)
    {
      List<Base> objects = new List<Base>();

      switch (obj)
      {
        case Base @base when CanConvertToNative(@base):
          objects.Add(@base);

          return objects;
        case Base @base:
          {
            foreach (var prop in @base.GetDynamicMembers())
              objects.AddRange(FlattenCommitObject(@base[prop]));

            return objects;
          }
        case IReadOnlyList<object> list:
          {
            foreach (var listObj in list)
              objects.AddRange(FlattenCommitObject(listObj));

            return objects;
          }
        case IDictionary dict:
          {
            foreach (DictionaryEntry kvp in dict)
              objects.AddRange(FlattenCommitObject(kvp.Value));

            return objects;
          }
        default:
          return objects;
      }
    }

    #endregion
  }
}
