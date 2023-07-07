using System;
using System.Runtime.CompilerServices;
using Autodesk.Navisworks.Api;
using Speckle.Core.Kits;

namespace Speckle.ConnectorNavisworks.Other;

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

public static class Utilities
{
#if NAVMAN21
    public readonly static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2024);
#elif NAVMAN20
  public readonly static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2023);
#elif NAVMAN19
    public readonly static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2022);
#elif NAVMAN18
    public readonly static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2021);
#elif NAVMAN17
    public readonly static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2020);
#endif

  internal static void ConsoleLog(string message, ConsoleColor color = ConsoleColor.Blue)
  {
    Console.WriteLine(message, color);
  }

  public static void WarnLog(string warningMessage)
  {
    ConsoleLog(warningMessage, ConsoleColor.DarkYellow);
  }

  public static void ErrorLog(string errorMessage)
  {
    ConsoleLog(errorMessage, ConsoleColor.DarkRed);
  }

  public static string GetUnits(Document doc)
  {
    return doc.Units.ToString();
  }
}
