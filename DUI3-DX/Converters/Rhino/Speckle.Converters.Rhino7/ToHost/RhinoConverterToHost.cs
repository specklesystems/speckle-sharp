﻿using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.ToHost;

public class RhinoConverterToHost : ISpeckleConverterToHost
{
  private readonly IFactory<string, ISpeckleObjectToHostConversion> _toHost;

  public RhinoConverterToHost(IFactory<string, ISpeckleObjectToHostConversion> toHost)
  {
    _toHost = toHost;
  }

  public object Convert(Base target)
  {
    var typeName = target.GetType().Name;
    var objectConverter = _toHost.ResolveInstance(typeName);

    if (objectConverter == null)
    {
      throw new NotSupportedException($"No conversion found for {typeName}");
    }

    var convertedObject = objectConverter.Convert(target);

    return convertedObject;
  }
}
