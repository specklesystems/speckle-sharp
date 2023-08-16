using System;
using System.Collections.Generic;
using Speckle.Core.Models;

namespace Objects.Converter.Navisworks;

// ReSharper disable once UnusedType.Global
public partial class ConverterNavisworks
{
  /// Methods included to satisfy the ISpeckleConverter requirements
  /// No actual receiving exists
  ///
  /// <inheritdoc />
  public object ConvertToNative(Base @object)
  {
    throw new NotImplementedException();
  }

  public object ConvertToNativeDisplayable(Base @object)
  {
    throw new NotImplementedException();
  }

  /// <inheritdoc />
  public List<object> ConvertToNative(List<Base> objects)
  {
    throw new NotImplementedException();
  }

  /// <inheritdoc />
  public bool CanConvertToNative(Base @object)
  {
    throw new NotImplementedException();
  }

  public bool CanConvertToNativeDisplayable(Base @object)
  {
    throw new NotImplementedException();
  }
}
