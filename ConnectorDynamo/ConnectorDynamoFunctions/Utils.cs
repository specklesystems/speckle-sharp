using Autodesk.DesignScript.Runtime;
using ProtoCore.Lang;
using Objects.Converter.Dynamo;
using Speckle.Core.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Speckle.ConnectorDynamo.Functions
{
  public static class Utils
  {
    /// <summary>
    /// Helper method to convert a tree-like structure (nested lists) to Speckle
    /// </summary>
    /// <param name="object"></param>
    /// <returns></returns>
    [IsVisibleInDynamoLibrary(false)]
    public static Base ConvertRecursivelyToSpeckle(object @object)
    {
      var converted = RecusrseTreeToSpeckle(@object);
      var @base = new Base();

      if (IsList(converted))
      {
        @base["@list"] = converted;
      }
      else if (IsDictionary(converted))
      {
        @base["@dictionary"] = converted;
      }
      else
      {
        @base = (Base)converted;
      }

      return @base;
    }

    /// <summary>
    /// Helper method to convert a tree-like structure (nested lists) to Native
    /// </summary>
    /// <param name="base"></param>
    /// <returns></returns>
    [IsVisibleInDynamoLibrary(false)]
    public static object ConvertRecursivelyToNative(Base @base)
    {
      //TODO: Check all properties!
      if (@base.HasMember("@list"))
      {
        return RecusrseTreeToNative(@base["@list"]);
      }
      if (@base.HasMember("@dictionary"))
      {
        return RecusrseTreeToNative(@base["@dictionary"]);
      }

      return RecusrseTreeToNative(@base);
    }


    private static object RecusrseTreeToSpeckle(object @object)
    {
      if (IsList(@object))
      {
        var list = ((IEnumerable)@object).Cast<object>().ToList();
        return list.Select(x => RecusrseTreeToSpeckle(x));
      }

      if (@object is DesignScript.Builtin.Dictionary)
      {
        var dynamoDic = ((DesignScript.Builtin.Dictionary)@object);
        var dictionary = new Dictionary<string, object>();
        foreach (var key in dynamoDic.Keys)
        {
          dictionary[key] = RecusrseTreeToSpeckle(dynamoDic.ValueAtKey(key));
        }
        return dictionary;
      }

      if (IsDictionary(@object))
      {
        var dictionary = @object as Dictionary<string, object>;
        return dictionary.ToDictionary(key => key, value => RecusrseTreeToSpeckle(value));
      }

      var converter = new ConverterDynamo();
      return converter.ConvertToSpeckle(@object);
    }

    private static object RecusrseTreeToNative(object @object)
    {
      if (IsList(@object))
      {
        var list = @object as List<object>;
        return list.Select(x => RecusrseTreeToNative(x));
      }

      if (IsDictionary(@object))
      {
        var dictionary = @object as Dictionary<string, object>;
        return dictionary.ToDictionary(x => x.Key, x => RecusrseTreeToNative(x.Value));
      }


      var converter = new ConverterDynamo();
      return converter.ConvertToNative((Base)@object);
    }







    private static bool IsList(object @object)
    {
      var type = @object.GetType();
      return (typeof(IEnumerable).IsAssignableFrom(type) && !typeof(IDictionary).IsAssignableFrom(type) && type != typeof(string));
    }

    private static bool IsDictionary(object @object)
    {
      Type type = @object.GetType();
      return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
    }
  }
}
