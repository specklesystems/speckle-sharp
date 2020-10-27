using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Credentials;

namespace Speckle.ConnectorDynamo.Functions
{
  internal static class Utils
  {
    internal static List<T> MultiDimensionalInputToList<T>(object input)
    {
      var items = new List<T>();
      var isList = true;

      //it's a flat list?
      try
      {
        items = (input as ArrayList).ToArray().Cast<T>().ToList();
      }
      catch
      {
        isList = false;
      }

      //it's a single item?
      if (!isList)
      {
        try
        {
          var s = (T)input;
          items.Add(s);
        }
        catch
        {
        }
      }

      return items;
    }
  }
}
