using Grasshopper.Kernel.Types;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using System.Threading;

namespace ConnectorGrasshopper.Extras
{
  public static class Utilities
  {
    public static List<object> DataTreeToNestedLists(GH_Structure<IGH_Goo> dataInput, ISpeckleConverter converter, Action OnConversionProgress = null)
    {
      return DataTreeToNestedLists(dataInput, converter, CancellationToken.None, OnConversionProgress);
    }

    public static List<object> DataTreeToNestedLists(GH_Structure<IGH_Goo> dataInput, ISpeckleConverter converter, CancellationToken cancellationToken, Action OnConversionProgress = null)
    {
      var output = new List<object>();
      for (var i = 0; i < dataInput.Branches.Count; i++)
      {
        if (cancellationToken.IsCancellationRequested) return output;

        var path = dataInput.Paths[i].Indices.ToList();
        var leaves = new List<object>(); 
        
        foreach(var goo in dataInput.Branches[i])
        {
        if (cancellationToken.IsCancellationRequested) return output;
          OnConversionProgress?.Invoke();
          leaves.Add(TryConvertItemToSpeckle(goo, converter));
        }

        RecurseTreeToList(output, path, 0, leaves);
      }

      return output;
    }

    private static void RecurseTreeToList(List<object> parent, List<int> path, int pathIndex, List<object> objects)
    {
      try
      {
        var listIndex = path[pathIndex]; //there should be a list at this index inside this parent list

        parent = EnsureHasSublistAtIndex(parent, listIndex);
        var sublist = parent[listIndex] as List<object>;
        //it's the last index of the path => the last sublist => add objects
        if (pathIndex == path.Count - 1)
        {
          sublist.AddRange(objects);
          return;
        }

        RecurseTreeToList(sublist, path, pathIndex + 1, objects);
      }
      catch (Exception ex)
      {
        throw ex;
      }
    }

    /// <summary>
    /// For a given parent list it creates enough sublists so that we have a sublist at the specified index
    /// If the parent contains some objects already, insert the sublist the the specified index
    /// If there is a missing branch, insert an empty list {0,0} {0,1} {0,3}
    /// If paths have variable length account for it {0} {0,0} {0,1,0}
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    private static List<object> EnsureHasSublistAtIndex(List<object> parent, int index)
    {
      while (parent.Count <= index || !(parent[index] is List<object>))
      {
        if (parent.Count > index)
          parent.Insert(index, new List<object>());
        else
          parent.Add(new List<object>());
      }

      return parent;
    }


    public static IGH_Goo TryConvertItemToNative(object value, ISpeckleConverter converter)
    {
      if (value == null)
        return null;

      if (value is IGH_Goo)
      {
        value = value.GetType().GetProperty("Value")?.GetValue(value);
      }

      if (value is Base @base && converter.CanConvertToNative(@base))
      {
        var converted = converter.ConvertToNative(@base);
        var geomgoo = GH_Convert.ToGoo(converted);
        if (geomgoo != null) 
          return geomgoo;
        var goo = new GH_ObjectWrapper { Value = converted };
        return goo;
      }

      if (value is Base @base2)
      {
        var goo = new GH_SpeckleBase { Value = @base2 };
        return goo;
      }

      if (value.GetType().IsSimpleType())
      {
        return GH_Convert.ToGoo(value);
      }

      if (value is Enum)
      {
        var i = (Enum) value;
        return new GH_ObjectWrapper {Value = i};
      }
      return null;
    }

    public static object TryConvertItemToSpeckle(object value, ISpeckleConverter converter)
    {
      object result = null;

      if (value is null) throw new Exception("Null values are not allowed, please clean your data tree.");
      
      if (value is IGH_Goo)
      {
        value = value.GetType().GetProperty("Value").GetValue(value);
      }

      if (value is Base || value.GetType().IsSimpleType())
      {
        return value;
      }

      if (converter.CanConvertToSpeckle(value))
      {
        return converter.ConvertToSpeckle(value);
      }
      
      return result;
    }

    public static GH_Structure<IGH_Goo> GetSubTree(GH_Structure<IGH_Goo> valueTree, GH_Path searchPath)
    {
      var subTree = new GH_Structure<IGH_Goo>();
      var gen = 0;
      foreach (var path in valueTree.Paths)
      {
        var branch = valueTree.get_Branch(path) as IEnumerable<IGH_Goo>;
        if (path.IsAncestor(searchPath, ref gen))
        {
          subTree.AppendRange(branch, path);
        }
        else if (path.IsCoincident(searchPath))
        {
          subTree.AppendRange(branch, path);
          break;
        }
      }

      subTree.Simplify(GH_SimplificationMode.CollapseLeadingOverlaps);
      return subTree;
    }

    public static bool IsList(object @object)
    {
      var type = @object.GetType();
      return (typeof(IEnumerable).IsAssignableFrom(type) && !typeof(IDictionary).IsAssignableFrom(type) &&
              type != typeof(string));
    }
  }
}
