using Grasshopper.Kernel.Types;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConnectorGrashopper.Extras
{
  public static class Utilities
  {

    public static object TryConvertItemToNative(object value, ISpeckleConverter converter)
    {
      if (value is IGH_Goo)
      {
        value = value.GetType().GetProperty("Value")?.GetValue(value);
      }
      if (value is Base @base && converter.CanConvertToNative(@base))
      {
        return converter.ConvertToNative(@base);
      }
      if (value.GetType().IsSimpleType())
      {
        return value;
      }
      return null;
    }

    public static object TryConvertItemToSpeckle(object value, ISpeckleConverter converter)
    {
      object result = null;

      if (value is IGH_Goo)
      {
        value = value.GetType().GetProperty("Value").GetValue(value);
      }

      if (value is Base || Speckle.Core.Models.Utilities.IsSimpleType(value.GetType()))
      {
        return value;
      }

      if (converter.CanConvertToSpeckle(value))
      {
        return converter.ConvertToSpeckle(value);
      }

      return result;
    }
  }
}
