﻿using System.Diagnostics.CodeAnalysis;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Common.DependencyInjection.ToHost;

// POC: CNX-9394 Find a better home for this outside `DependencyInjection` project
/// <summary>
/// Provides an implementation for <see cref="IRootToHostConverter"/>
/// that resolves a <see cref="IToHostTopLevelConverter"/> via the injected <see cref="IConverterResolver{TConverter}"/>
/// </summary>
/// <seealso cref="ConverterWithFallback"/>
public sealed class ConverterWithoutFallback : IRootToHostConverter
{
  private readonly IConverterResolver<IToHostTopLevelConverter> _toHost;

  public ConverterWithoutFallback(IConverterResolver<IToHostTopLevelConverter> converterResolver)
  {
    _toHost = converterResolver;
  }

  public object Convert(Base target)
  {
    if (TryConvert(target, out object? result))
    {
      return result;
    }
    throw new NotSupportedException($"No conversion found for {target.GetType()}");
  }

  internal bool TryConvert(Base target, [NotNullWhen(true)] out object? result)
  {
    // Direct conversion if a converter is found
    var objectConverter = _toHost.GetConversionForType(target.GetType());
    if (objectConverter != null)
    {
      result = objectConverter.Convert(target);
      return true;
    }

    result = null;
    return false;
  }
}
