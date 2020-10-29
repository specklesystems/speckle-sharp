using Grasshopper.Kernel.Types;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel.Data;

namespace ConnectorGrasshopper.Extras
{
  public static class Utilities
  {

    public static object TryConvertItemToNative(object value, ISpeckleConverter converter)
    {
      if (value is IGH_Goo)
      {
        value = value.GetType().GetProperty("Value")?.GetValue(value);
      }
      if (value is Base @base && converter.CanConvertToNative(@base))
      {
        return converter.ConvertToNative(@base);
      }
      if (value.GetType().IsSimpleType())
      {
        return value;
      }
      return null;
    }

    public static object TryConvertItemToSpeckle(object value, ISpeckleConverter converter)
    {
      object result = null;

      if (value is IGH_Goo)
      {
        value = value.GetType().GetProperty("Value").GetValue(value);
      }

      if (value is Base || Speckle.Core.Models.Utilities.IsSimpleType(value.GetType()))
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
  }
}
