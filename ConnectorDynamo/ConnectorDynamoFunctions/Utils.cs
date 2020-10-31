using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Credentials;

namespace Speckle.ConnectorDynamo.Functions
{
  internal static class Utils
  {
    internal static List<StreamWrapper> InputToStream(object input)
    {
      try
      {
        //it's a list
        var array = (input as ArrayList).ToArray();

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
        return new List<StreamWrapper>{ input as StreamWrapper};
      }
      catch
      {
        //ignored
      }

      try
      {
        //single url
        return new List<StreamWrapper>{ new StreamWrapper(input as string)};
      }
      catch
      {
        //ignored
      }

      return null;
    }
  }
}
