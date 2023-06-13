using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Autodesk.Navisworks.Api;

namespace Objects.Converter.Navisworks;

internal static class ArrayExtension
{
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static T[] ToArray<T>(this Array arr)
    where T : struct
  {
    var result = new T[arr.Length];
    Array.Copy(arr, result, result.Length);
    return result;
  }
}

// ReSharper disable once UnusedType.Global
public partial class ConverterNavisworks
{
  private const string RootNodePseudoId = "___";

  /// <summary>
  ///   Checks is the Element is hidden or if any of its ancestors is hidden
  /// </summary>
  /// <param name="element"></param>
  /// <returns></returns>
  private static bool IsElementHidden(ModelItem element)
  {
    // Hidden status is stored at the earliest node in the hierarchy
    // Any of the the tree path nodes Hidden then the element is hidden
    return element.AncestorsAndSelf.Any(x => x.IsHidden);
  }
}
