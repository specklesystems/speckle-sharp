using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.ComApi;
using Autodesk.Navisworks.Api.Interop.ComApi;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Objects.Converter.Navisworks
{
  internal static class ArrayExtension
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] ToArray<T>(this Array arr) where T : struct
    {
      T[] result = new T[arr.Length];
      Array.Copy(arr, result, result.Length);
      return result;
    }
  }

  public partial class ConverterNavisworks
  {
    private static string PseudoIdFromModelItem(ModelItem element)
    {
      // The path for ModelItems is their node position at each level of the Models tree.
      // This is the de facto UID for that element within the file at that time.
      InwOaPath path = ComApiBridge.ToInwOaPath(element);

      // Acknowledging that if a collection contains >=10000 children then this indexing will be inadequate
      string pointer = ((Array)path.ArrayData).ToArray<int>().Aggregate("",
        (current, value) => current + (value.ToString().PadLeft(4, '0') + "-")).TrimEnd('-');

      return pointer;
    }
  }
}