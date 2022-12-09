using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Objects.Converter.Navisworks
{
  public partial class ConverterNavisworks
  {
    public Base ConvertToSpeckle(object @object)
    {
      Base @base = null;

      return @base;
    }


    public List<Base> ConvertToSpeckle(List<object> objects)
    {
      return objects.Select(ConvertToSpeckle).ToList();
    }

    public bool CanConvertToSpeckle(object @object)
    {
      switch (@object)
      {
        default:
          return false;
      }
    }
  }
}