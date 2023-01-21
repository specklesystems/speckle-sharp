using System;
using System.Collections.Generic;
using Autodesk.Navisworks.Api;
using Speckle.Core.Kits;
using System.Runtime.CompilerServices;
using Autodesk.Navisworks.Api.Interop.ComApi;
using Autodesk.Navisworks.Api.ComApi;
using System.Linq;

namespace Speckle.ConnectorNavisworks
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


  internal class PseudoIdComparer : IComparer<string>
  {
    public int Compare(string x, string y) =>
      x != null && y != null
        ? x.Length == y.Length ? string.Compare(x, y, StringComparison.Ordinal) : x.Length.CompareTo(y.Length)
        : 0;
  }

  public static class Utils
  {
#if NAVMAN20
    public static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2023);
#elif NAVMAN19
    public static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2022);
#elif NAVMAN18
    public static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2021);
#elif NAVMAN17
    public static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2020);
#endif
    public static string InvalidChars = @"<>/\:;""?*|=,‘";
    public static string ApplicationIdKey = "applicationId";
    public static string RootNodePseudoId = "___"; // This should be shorter than the padding on indexes and not contain '-'


    public static void ConsoleLog(string message, ConsoleColor color = ConsoleColor.Blue)
    {
      Console.WriteLine(message, color);
    }

    public static void WarnLog(string warningMessage)
    {
      ConsoleLog(warningMessage, color: ConsoleColor.DarkYellow);
    }

    public static void ErrorLog(Exception err)
    {
      ErrorLog(err.Message);
      throw err;
    }

    public static void ErrorLog(string errorMessage) => ConsoleLog(errorMessage, ConsoleColor.DarkRed);


    public static string GetUnits(Document doc)
    {
      return doc.Units.ToString();
    }

    internal static string ObjectDescriptor(string pseudoId)
    {
      ModelItem element = PointerToModelItem(pseudoId);
      string simpleType = element.GetType().ToString().Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries)
        .LastOrDefault();
      return string.IsNullOrEmpty(element.ClassDisplayName)
        ? $"{simpleType}"
        : $"{simpleType} {element.ClassDisplayName}";
    }

    internal static ModelItem PointerToModelItem(object @string)
    {
      int[] pathArray;

      if (@string.ToString() == RootNodePseudoId)
      {
        var rootItems = Application.ActiveDocument.Models.RootItems;

        return rootItems.First;
      }

      try
      {
        pathArray = @string.ToString().Split('-').Select(x =>
        {
          if (int.TryParse(x, out var value))
          {
            return value;
          }

          throw (new Exception("malformed path pseudoId"));
        }).ToArray();
      }
      catch
      {
        return null;
      }

      InwOpState10 oState = ComApiBridge.State;
      InwOaPath protoPath = (InwOaPath)oState.ObjectFactory(nwEObjectType.eObjectType_nwOaPath);

      Array oneBasedArray = Array.CreateInstance(
        typeof(int),
        // ReSharper disable once RedundantExplicitArraySize
        new int[1] { pathArray.Length },
        // ReSharper disable once RedundantExplicitArraySize
        new int[1] { 1 });

      Array.Copy(pathArray, 0, oneBasedArray, 1, pathArray.Length);

      protoPath.ArrayData = oneBasedArray;

      ModelItem m = ComApiBridge.ToModelItem(protoPath);

      return m;
    }

    public static string GetPseudoId(ModelItem modelItem)
    {
      // The path for ModelItems is their node position at each level of the Models tree.
      // This is the de facto UID for that element within the file at that time.
      InwOaPath path = ComApiBridge.ToInwOaPath(modelItem);


      var arrayData = ((Array)path.ArrayData).ToArray<int>();

      // Neglect the Root Node
      if (arrayData.Length == 0) return RootNodePseudoId;

      // Acknowledging that if a collection contains >=10000 children then this indexing will be inadequate
      string pseudoId = arrayData.Aggregate("",
        (current, value) => current + (value.ToString().PadLeft(4, '0') + "-")).TrimEnd('-');

      return pseudoId;
    }

   
  }
}