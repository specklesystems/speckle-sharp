using Autodesk.DesignScript.Runtime;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Speckle.Core.Logging;

namespace Speckle.ConnectorDynamo.Functions
{
  [IsVisibleInDynamoLibrary(false)]
  public class BatchConverter
  {
    private ISpeckleConverter _converter { get; set; }

    public BatchConverter()
    {
      var kit = KitManager.GetDefaultKit();

      if (kit == null)
        throw new SpeckleException("Cannot find the Objects Kit. Has it been copied to the Kits folder?");

      _converter = kit.LoadConverter(Utils.GetAppName());

      if (_converter == null)
        throw new SpeckleException("Cannot find the Dynamo converter. Has it been copied to the Kits folder?");

      // if in Revit, we have a doc, injected by the Extension
      if (Globals.RevitDocument != null)
        _converter.SetContextDocument(Globals.RevitDocument);

    }



    /// <summary>
    /// Helper method to convert a tree-like structure (nested lists) to Speckle
    /// </summary>
    /// <param name="object"></param>
    /// <returns></returns>
    public Base ConvertRecursivelyToSpeckle(object @object)
    {
      if (@object is ProtoCore.DSASM.StackValue)
        throw new SpeckleException("Invalid input");

      var converted = RecurseTreeToSpeckle(@object);

      if (converted is null) return null;
      var @base = new Base();

      //case 1: lists and basic types => add them to a wrapper Base object in a `data` prop
      //case 2: Base => just use it as it is
      if (IsList(converted) || converted.GetType().IsSimpleType())
      {
        @base["@data"] = converted;
      }
      else if (converted is Base convertedBase)
      {
        @base = convertedBase;
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

      if (@object is DesignScript.Builtin.Dictionary dsDic)
      {
        return DictionaryToBase(dsDic);
      }

      if (@object is Dictionary<string, object> dic)
      {
        return DictionaryToBase(dic);
      }

      //is item
      return TryConvertItemToSpeckle(@object);
    }

    private Base DictionaryToBase(DesignScript.Builtin.Dictionary dsDic)
    {
      var @base = new Base();
      foreach (var key in dsDic.Keys)
      {
        @base[key] = RecurseTreeToSpeckle(dsDic.ValueAtKey(key));
      }

      return @base;
    }

    private Base DictionaryToBase(Dictionary<string, object> dic)
    {
      var @base = new Base();
      foreach (var key in dic.Keys)
      {
        @base[key] = RecurseTreeToSpeckle(dic[key]);
      }

      return @base;
    }

    private object TryConvertItemToSpeckle(object value)
    {
      object result = null;
      if (_converter.CanConvertToSpeckle(value))
      {
        try
        {
          return _converter.ConvertToSpeckle(value);
        }
        catch (Exception ex)
        {
          throw new SpeckleException("Could not convert " + value.GetType() + " to Speckle:" + ex.Message, ex);
        }
      }


      if (value is Base || value is null || value.GetType().IsSimpleType())
      {
        return value;
      }



      return result;
    }

    private static Regex dataTreePathRegex => new Regex(@"^(@(\(\d+\))?)?(?<path>\{\d+(;\d+)*\})$");
    
    public static bool IsDataTree(Base @base)
    {
      var regex = dataTreePathRegex;
      var members = @base.GetDynamicMembers().ToList();
      if (members.Count == 0) return false;
      var isDataTree = members.All(el => regex.Match(el).Success);
      return members.Count > 0 && isDataTree;
    }

    public object ConvertDataTreeToNative(Base @base)
    {
      var names = @base.GetDynamicMembers();
      var list = new List<object>();
      foreach (var name in names)
      {
        if (!dataTreePathRegex.Match(name).Success) continue; // Ignore non matching elements, done for extra safety.
        
        var parts =
          name.Split('{')[1] // Get everything after open curly brace
            .Split('}')[0] // Get everything before close curly brace
            .Split(';') // Split by ;
            .Select(text =>
            {
              int.TryParse(text, out var num);
              return num;
            }).ToList(); 

        var currentList = list;
        foreach (var p in parts)
        {
          while (currentList.Count < p + 1)
          {
            var newList = new List<object>();
            currentList.Add(newList);
          }

          currentList = currentList[p] as List<object>;
        }

        var value = @base[name];
        var converted = RecurseTreeToNative(value) as List<object>;
        currentList.AddRange(converted);
        Console.WriteLine(parts);
      }
      return list;
    }

    /// <summary>
    /// Helper method to convert a tree-like structure (nested lists) to Native
    /// </summary>
    /// <param name="base"></param>
    /// <returns></returns>
    public object ConvertRecursivelyToNative(Base @base)
    {
      if (@base == null)
        return null;

      // case 1: it's an item that has a direct conversion method, eg a point
      if (_converter.CanConvertToNative(@base))
        return TryConvertItemToNative(@base);
      // if (IsDataTree(@base))
      // {
      //   return ConvertDataTreeToNative(@base);
      // }
      // case 2: it's a wrapper Base
      //       2a: if there's only one member unpack it
      //       2b: otherwise return dictionary of unpacked members
      var members = @base.GetMemberNames();

      if (members.Count() == 1)
      {
        var converted = RecurseTreeToNative(@base[members.ElementAt(0)]);
        return converted;
      }

      return members.ToDictionary(x => x, x => RecurseTreeToNative(@base[x]));
    }


    private object RecurseTreeToNative(object @object)
    {
      if (IsList(@object))
      {
        var list = ((IEnumerable)@object).Cast<object>();
        return list.Select(x => RecurseTreeToNative(x)).ToList();
      }
      if (@object is Base @base && IsDataTree(@base))
      {
        return ConvertDataTreeToNative(@base);
      }
      
      return TryConvertItemToNative(@object);
    }

    private object TryConvertItemToNative(object value)
    {
      if (value == null)
        return value;

      //it's a simple type or not a Base
      if (value.GetType().IsSimpleType() || !(value is Base))
      {
        return value;
      }

      var @base = (Base)value;

      //it's an unsupported Base, return a dictionary
      if (!_converter.CanConvertToNative(@base))
      {
        //get both dynamic and instance members
        var instanceMembers = @base.GetInstanceMembersNames();
        var dynamicMembers = @base.GetDynamicMembers();
        var dicI = instanceMembers.ToDictionary(x => x, x => RecurseTreeToNative(@base[x]));
        var dicD = dynamicMembers.ToDictionary(x => x, x => RecurseTreeToNative(@base[x]));

        var dic = dicI.Concat(dicD).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);


        return dic;
      }

      try
      {
        return _converter.ConvertToNative(@base);
      }
      catch (Exception ex)
      {
        throw new SpeckleException(ex.Message, ex);
      }

      return null;
    }


    public static bool IsList(object @object)
    {
      if (@object == null)
        return false;

      var type = @object.GetType();
      return (typeof(IEnumerable).IsAssignableFrom(type) && !typeof(IDictionary).IsAssignableFrom(type) &&
              type != typeof(string));
    }

    public static bool IsDictionary(object @object)
    {
      if (@object == null)
        return false;

      Type type = @object.GetType();
      return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
    }
  }
}

