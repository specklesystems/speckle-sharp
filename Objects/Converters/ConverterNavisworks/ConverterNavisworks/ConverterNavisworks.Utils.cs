using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.ComApi;

namespace Objects.Converter.Navisworks
{
  internal static class ArrayExtension
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] ToArray<T>(this Array arr) where T : struct
    {
      var result = new T[arr.Length];
      Array.Copy(arr, result, result.Length);
      return result;
    }
  }


  public partial class ConverterNavisworks
  {
    public static string
      RootNodePseudoId = "___";

    private static string PseudoIdFromModelItem(ModelItem element)
    {
      // The path for ModelItems is their node position at each level of the Models tree.
      // This is the de facto UID for that element within the file at that time.
      if (element == null) return null;

      var path = ComApiBridge.ToInwOaPath(element);

      // Acknowledging that if a collection contains >=10000 children then this indexing will be inadequate
      var pointer = ((Array)path.ArrayData).ToArray<int>().Aggregate("",
        (current, value) => current + value.ToString().PadLeft(4, '0') + "-").TrimEnd('-');

      return pointer;
    }


    /// <summary>
    ///   Checks is the Element is hidden or if any of its ancestors is hidden
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    private static bool IsElementVisible(ModelItem element)
    {
      // Hidden status is stored at the earliest node in the hierarchy
      // All of the the tree path nodes need to not be Hidden
      return element.AncestorsAndSelf.All(x => x.IsHidden != true);
    }

    /// <summary>
    ///   Checks is the Element is hidden or if any of its ancestors is hidden
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    private static bool IsElementHidden(ModelItem element)
    {
      // Hidden status is stored at the earliest node in the hierarchy
      // Any of the the tree path nodes Hidden then the element is hidden
      return element.AncestorsAndSelf.Any(x => x.IsHidden == true);
    }
  }
}
