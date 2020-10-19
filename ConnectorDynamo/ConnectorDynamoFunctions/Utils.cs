using Autodesk.DesignScript.Runtime;
using ProtoCore.Lang;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Speckle.ConnectorDynamo.Functions
{
  [IsVisibleInDynamoLibrary(false)]
  public static class Utils
  {
    /// <summary>
    /// Helper method to convert a tree-like structure (nested lists) to Speckle
    /// </summary>
    /// <param name="object"></param>
    /// <returns></returns>
    public static RecursiveConversionResult ConvertRecursivelyToSpeckle(object @object)
    {
      var recursiveConverter = new RecursiveTreeToSpeckleConverter(@object);
      return recursiveConverter.result;
    }

    /// <summary>
    /// Helper method to convert a tree-like structure (nested lists) to Native
    /// </summary>
    /// <param name="base"></param>
    /// <returns></returns>
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

      var Kit = KitManager.GetDefaultKit();
      try
      {
        var converter = Kit.LoadConverter(Applications.Dynamo);
        return converter.ConvertToNative((Base)@object);
      }
      catch
      {
        //TODO: use Capture and Throw method in Core
        throw new Exception("No default kit found on this machine.");
      }
    }



    public static bool IsList(object @object)
    {
      var type = @object.GetType();
      return (typeof(IEnumerable).IsAssignableFrom(type) && !typeof(IDictionary).IsAssignableFrom(type) && type != typeof(string));
    }

    public static bool IsDictionary(object @object)
    {
      Type type = @object.GetType();
      return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
    }




  }

  /// <summary>
  /// This class helps implement a local counter to get the total number of objects converted
  /// </summary>
  public class RecursiveTreeToSpeckleConverter
  {
    public int Counter { get; internal set; } = 0;
    public RecursiveConversionResult result { get; internal set; }
    private object @object { get; set; }

    public RecursiveTreeToSpeckleConverter(object @object)
    {
      this.@object = RecurseTreeToSpeckle(@object);
      var @base = new Base();

      if (Utils.IsList(this.@object))
      {
        @base["@list"] = this.@object;
      }
      else if (Utils.IsDictionary(this.@object))
      {
        @base["@dictionary"] = this.@object;
      }
      else
      {
        @base = (Base)this.@object;
      }


      result = new RecursiveConversionResult(Counter, @base);
    }


    private object RecurseTreeToSpeckle(object @object)
    {
      if (Utils.IsList(@object))
      {
        var list = ((IEnumerable)@object).Cast<object>().ToList();
        return list.Select(x => RecurseTreeToSpeckle(x)).ToList();
      }

      if (@object is DesignScript.Builtin.Dictionary)
      {
        var dynamoDic = ((DesignScript.Builtin.Dictionary)@object);
        var dictionary = new Dictionary<string, object>();
        foreach (var key in dynamoDic.Keys)
        {
          dictionary[key] = RecurseTreeToSpeckle(dynamoDic.ValueAtKey(key));
        }
        return dictionary;
      }

      if (Utils.IsDictionary(@object))
      {
        var dictionary = @object as Dictionary<string, object>;
        return dictionary.ToDictionary(key => key, value => RecurseTreeToSpeckle(value));
      }

      var Kit = KitManager.GetDefaultKit();
      try
      {
        Counter++;
        var converter = Kit.LoadConverter(Applications.Dynamo);
        return converter.ConvertToSpeckle(@object);
      }
      catch (Exception ex)
      {
        //TODO: use Capture and Throw method in Core
        throw new Exception("Conversion failed: " + ex.Message);
      }

    }
  }
  /// <summary>
  /// Data structure to return Count and Base object at once
  /// </summary>
  public class RecursiveConversionResult
  {
    public int TotalObjects { get; set; }
    public Base Object { get; set; }

    public RecursiveConversionResult(int totalObjects, Base @object)
    {
      TotalObjects = totalObjects;
      Object = @object;
    }

  }
}
