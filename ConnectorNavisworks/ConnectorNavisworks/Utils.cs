using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using Autodesk.Navisworks.Api;
using Speckle.Core.Kits;
using System.Drawing;
using Console = Colorful.Console;
using System.Runtime.CompilerServices;

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


    public static void ConsoleLog(string message, ConsoleColor color = ConsoleColor.Blue, string hex = "#3b82f6")
    {
      Console.WriteLine(message, color: ColorTranslator.FromHtml(hex));
    }

    public static void WarnLog(string warningMessage)
    {
      ConsoleLog(warningMessage, hex: "#f59e0b");
    }

    public static void ErrorLog(Exception err)
    {
      ErrorLog(err.Message);
      throw err;
    }

    public static void ErrorLog(string errorMessage) => ConsoleLog(errorMessage, hex: "#ef4444");


    public static string GetUnits(Document doc)
    {
      return doc.Units.ToString();
    }
  }
}