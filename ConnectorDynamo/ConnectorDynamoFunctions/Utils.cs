using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Transports;

namespace Speckle.ConnectorDynamo.Functions
{
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
          transports = s
            .Select(TryConvertInputToTransport)
            .Aggregate(transports, (current, t) => new List<Dictionary<ITransport, string>> { current, t }
              .SelectMany(dict => dict)
              .ToDictionary(pair => pair.Key, pair => pair.Value));
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
        return HostApplications.Dynamo.GetVersion(HostAppVersion.vSandbox);
      else
      {
        try
        {
          System.Type type = Globals.RevitDocument.GetType();
          var app = (object)type.GetProperty("Application").GetValue(Globals.RevitDocument, null);

          System.Type type2 = app.GetType();
          var version = (string)type2.GetProperty("VersionNumber").GetValue(app, null);

          if (version.Contains("2024"))
            return HostApplications.Dynamo.GetVersion(HostAppVersion.vRevit2024);
          if (version.Contains("2023"))
            return HostApplications.Dynamo.GetVersion(HostAppVersion.vRevit2023);
          if (version.Contains("2022"))
            return HostApplications.Dynamo.GetVersion(HostAppVersion.vRevit2022);
          if (version.Contains("2021"))
            return HostApplications.Dynamo.GetVersion(HostAppVersion.vRevit2021);
          else
            return HostApplications.Dynamo.GetVersion(HostAppVersion.vRevit);

        }
        catch (Exception e)
        {
          return HostApplications.Dynamo.GetVersion(HostAppVersion.vRevit);
        }
      }
    }

    //My god this function sucks. It took me 20 mins to understand. Why not one that simply deals with one stream wrapper, and then use linq to cast things around? 
    internal static List<StreamWrapper> InputToStream(object input)
    {
      try
      {
        //it's a list
        var array = (input as ArrayList)?.ToArray();

        try
        {
          //list of stream wrappers
          return array.Cast<StreamWrapper>().ToList();
        }
        catch
        {
          //ignored
        }

        try
        {
          //list of urls
          return array.Cast<string>().Select(x => new StreamWrapper(x)).ToList();
        }
        catch
        {
          //ignored
        }
      }
      catch
      {
        // ignored
      }

      try
      {
        //single stream wrapper
        var sw = input as StreamWrapper;
        if (sw != null)
        {
          return new List<StreamWrapper> { sw };
        }
      }
      catch
      {
        //ignored
      }

      try
      {
        //single url
        var s = input as string;
        if (!string.IsNullOrEmpty(s))
        {
          return new List<StreamWrapper> { new StreamWrapper(s) };
        }
      }
      catch
      {
        //ignored
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
}
