using Autodesk.DesignScript.Runtime;
using Newtonsoft.Json;
using ProtoCore.Lang;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Speckle.ConnectorDynamo.Functions
{
  [IsVisibleInDynamoLibrary(false)]
  public class BatchConverter
  {
    private ISpeckleConverter _converter { get; set; }
    public BatchConverter()
    {
      var kit = KitManager.GetDefaultKit();
      _converter = kit.LoadConverter(Applications.Dynamo);
    }

    /// <summary>
    /// Helper method to convert a tree-like structure (nested lists) to Speckle
    /// </summary>
    /// <param name="object"></param>
    /// <returns></returns>
    public Base ConvertRecursivelyToSpeckle(object @object)
    {
      var converted = RecurseTreeToSpeckle(@object);
      var @base = new Base();

      if (IsList(converted) || IsDictionary(converted) || Utilities.IsSimpleType(converted.GetType()))
      {
        @base["@data"] = converted;
      }
      else
      {
        @base = (Base)converted;
      }


      return @base;
    }

    private object RecurseTreeToSpeckle(object @object)
    {
      if (IsList(@object))
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

      if (IsDictionary(@object))
      {
        var dictionary = @object as Dictionary<string, object>;
        return dictionary.ToDictionary(key => key, value => RecurseTreeToSpeckle(value));
      }

      //is item
      return TryConvertItemToSpeckle(@object);


    }

    private object TryConvertItemToSpeckle(object value)
    {
      object result = null;

      if (value is Base || Utilities.IsSimpleType(value.GetType()))
      {
        return value;
      }

      try
      {
        return _converter.ConvertToSpeckle(value);
      }
      catch (Exception ex)
      {
        Core.Logging.Log.CaptureAndThrow(ex);
      }

      return result;
    }

    /// <summary>
    /// Helper method to convert a tree-like structure (nested lists) to Native
    /// </summary>
    /// <param name="base"></param>
    /// <returns></returns>
    public object ConvertRecursivelyToNative(Base @base)
    {
      // first check if it's a single item, eg a point
      if (_converter.CanConvertToNative(@base))
        return RecusrseTreeToNative(@base);

      // otherwise it's probably just a wrapper Base
      // - if there's only one member unpack it
      // - otherwise return dictionary of unpacked members
      var members = @base.GetDynamicMembers();

      if (members.Count() == 1)
      {
        return RecusrseTreeToNative(@base[members.ElementAt(0)]);
      }
      else
      {
        return members.ToDictionary(x => x, x => RecusrseTreeToNative(@base[x]));
      }
    }




    private object RecusrseTreeToNative(object @object)
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

      return TryConvertItemToNative(@object);

    }

    private object TryConvertItemToNative(object value)
    {
      object result = null;

      if (Utilities.IsSimpleType(value.GetType()) || !(value is Base) || !_converter.CanConvertToNative((Base)value))
      {
        return value;
      }

      try
      {
        return _converter.ConvertToNative((Base)value);
      }
      catch (Exception ex)
      {
        Core.Logging.Log.CaptureAndThrow(ex);
      }

      return result;
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





}
