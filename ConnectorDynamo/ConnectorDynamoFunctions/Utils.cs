using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Transports;

namespace Speckle.ConnectorDynamo.Functions;

public static class Utils
{
  public static Dictionary<ITransport, string> TryConvertInputToTransport(object o)
  {
    var defaultBranch = "main";
    var transports = new Dictionary<ITransport, string>();

    switch (o)
    {
      case StreamWrapper s:
        var wrapperTransport = new ServerTransport(s.GetAccount().Result, s.StreamId);
        var branch = s.BranchName ?? defaultBranch;
        transports.Add(wrapperTransport, branch);

        break;
      case string s:
        var streamWrapper = new StreamWrapper(s);
        var transport = new ServerTransport(streamWrapper.GetAccount().Result, streamWrapper.StreamId);
        var b = streamWrapper.BranchName ?? defaultBranch;
        transports.Add(transport, b);
        break;
      case ITransport t:
        transports.Add(t, defaultBranch);
        break;
      case List<object> s:
        transports = s.Select(TryConvertInputToTransport)
          .Aggregate(
            transports,
            (current, t) =>
              new List<Dictionary<ITransport, string>> { current, t }
                .SelectMany(dict => dict)
                .ToDictionary(pair => pair.Key, pair => pair.Value)
          );
        break;
      default:
        //Warning("Input was neither a transport nor a stream.");
        break;
    }

    return transports;
  }

  /// Gets the App name from the injected Doc without requiring a dependency on the Revit dll
  internal static string GetAppName()
  {
    if (Globals.RevitDocument == null)
    {
      return HostApplications.Dynamo.GetVersion(HostAppVersion.vSandbox);
    }
    else
    {
      try
      {
        Type type = Globals.RevitDocument.GetType();
        var app = (object)type.GetProperty("Application").GetValue(Globals.RevitDocument, null);

        Type type2 = app.GetType();
        var version = (string)type2.GetProperty("VersionNumber").GetValue(app, null);

        if (version.Contains("2024"))
        {
          return HostApplications.Dynamo.GetVersion(HostAppVersion.vRevit2024);
        }

        if (version.Contains("2023"))
        {
          return HostApplications.Dynamo.GetVersion(HostAppVersion.vRevit2023);
        }

        if (version.Contains("2022"))
        {
          return HostApplications.Dynamo.GetVersion(HostAppVersion.vRevit2022);
        }

        if (version.Contains("2021"))
        {
          return HostApplications.Dynamo.GetVersion(HostAppVersion.vRevit2021);
        }
        else
        {
          return HostApplications.Dynamo.GetVersion(HostAppVersion.vRevit);
        }
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        return HostApplications.Dynamo.GetVersion(HostAppVersion.vRevit);
      }
    }
  }

  internal static HostAppVersion? GetRevitHostAppVersion()
  {
    if (Globals.RevitDocument == null)
    {
      return null;
    }

    try
    {
      Type type = Globals.RevitDocument.GetType();

      var app = type.GetProperty("Application").GetValue(Globals.RevitDocument, null);

      Type type2 = app.GetType();
      var version = (string)type2.GetProperty("VersionNumber").GetValue(app, null);

      if (version.Contains("2024"))
      {
        return HostAppVersion.vRevit2024;
      }

      if (version.Contains("2023"))
      {
        return HostAppVersion.vRevit2023;
      }

      if (version.Contains("2022"))
      {
        return HostAppVersion.vRevit2022;
      }

      if (version.Contains("2021"))
      {
        return HostAppVersion.vRevit2021;
      }

      return HostAppVersion.vRevit;
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      return HostAppVersion.vRevit;
    }
  }

  /// <summary>
  /// Attempts to parse an input object into a list of stream wrapper instances.
  /// </summary>
  /// <param name="input"></param>
  /// <returns>The list of stream wrappers provided as input, or null if the input could not be parsed</returns>
  internal static List<StreamWrapper> InputToStream(object input)
  {
    return input switch
    {
      ArrayList arrayList => InputArrayListToStreams(arrayList),
      StreamWrapper sw => new List<StreamWrapper> { sw },
      string s when !string.IsNullOrEmpty(s) => new List<StreamWrapper> { new(s) },
      _ => null
    };
  }

  private static List<StreamWrapper> InputArrayListToStreams(ArrayList arrayList)
  {
    if (arrayList == null)
    {
      return null;
    }

    var array = arrayList.ToArray();

    try
    {
      //list of stream wrappers
      return array.Cast<StreamWrapper>().ToList();
    }
    catch (InvalidCastException)
    {
      // List is not comprised of StreamWrapper instances
      // This failure is expected.
    }

    try
    {
      //list of urls
      return array.Cast<string>().Select(x => new StreamWrapper(x)).ToList();
    }
    catch (InvalidCastException)
    {
      // List is not comprised of string instances
      // This failure is expected.
    }

    return null;
  }

  internal static StreamWrapper ParseWrapper(object input)
  {
    if (input is StreamWrapper w)
    {
      return w;
    }

    if (input is string s)
    {
      return new StreamWrapper(s);
    }

    return null;
  }

  internal static void HandleApiExeption(Exception ex)
  {
    if (ex.InnerException != null && ex.InnerException.InnerException != null)
    {
      throw (ex.InnerException.InnerException);
    }

    if (ex.InnerException != null)
    {
      throw (ex.InnerException);
    }
    else
    {
      throw (ex);
    }
  }
}
