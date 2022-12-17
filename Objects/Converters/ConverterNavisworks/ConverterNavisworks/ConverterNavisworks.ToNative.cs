using Speckle.Core.Models;
using System;
using System.Collections.Generic;

namespace Objects.Converter.Navisworks
{
  public partial class ConverterNavisworks
  {
    /// Methods included to satisfy the ISpeckleConverter requirements
    /// No actual receiving exists
    public object ConvertToNative(Base @object) => throw new NotImplementedException();
    public List<object> ConvertToNative(List<Base> objects) => throw new NotImplementedException();
    public bool CanConvertToNative(Base @object) => throw new NotImplementedException();
  }
}