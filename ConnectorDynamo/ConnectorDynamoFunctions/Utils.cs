using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Credentials;

namespace Speckle.ConnectorDynamo.Functions
{
  internal static class Utils
  {

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
