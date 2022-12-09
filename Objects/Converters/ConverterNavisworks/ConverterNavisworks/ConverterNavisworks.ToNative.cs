using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Objects.Converter.Navisworks
{
  public partial class ConverterNavisworks
  {
    public object ConvertToNative(Base @object)
    {
      throw new NotImplementedException();
    }

    public List<object> ConvertToNative(List<Base> objects)
    {
      throw new NotImplementedException();
    }

    public bool CanConvertToNative(Base @object)
    {
      switch (@object)
      {
        default: return false;
      }
    }
  }
}