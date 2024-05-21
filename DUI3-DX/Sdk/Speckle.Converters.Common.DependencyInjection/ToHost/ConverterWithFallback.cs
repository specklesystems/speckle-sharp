﻿using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;

namespace Speckle.Converters.Common.DependencyInjection.ToHost;

// POC: CNX-9394 Find a better home for this outside `DependencyInjection` project
/// <summary>
/// <inheritdoc cref="ConverterWithoutFallback"/>
/// <br/>
/// If no suitable converter conversion is found, and the target <see cref="Base"/> object has a displayValue property
/// a converter with strong name of <see cref="DisplayableObject"/> is resolved for.
/// </summary>
/// <seealso cref="ConverterWithoutFallback"/>
public sealed class ConverterWithFallback : IRootToHostConverter
{
  private readonly IConverterResolver<IToHostTopLevelConverter> _toHost;
  private readonly ConverterWithoutFallback _baseConverter;

  public ConverterWithFallback(IConverterResolver<IToHostTopLevelConverter> toHost)
  {
    _toHost = toHost;
    _baseConverter = new ConverterWithoutFallback(toHost);
  }

  /// <summary>
  /// Converts a <see cref="Base"/> instance to a host object.
  /// </summary>
  /// <param name="target">The <see cref="Base"/> instance to convert.</param>
  /// <returns>The converted host object.
  /// Fallbacks to display value if a direct conversion is not possible.</returns>
  /// <remarks>
  /// The conversion is done in the following order of preference:
  /// 1. Direct conversion using the <see cref="ConverterWithoutFallback.TryConvert(Base, out object?)"/> method.
  /// 2. Fallback to display value using the <see cref="Speckle.Core.Models.Extensions.BaseExtensions.TryGetDisplayValue{T}"/> method, if a direct conversion is not possible.
  ///
  /// If the direct conversion is not available and there is no displayValue, a <see cref="System.NotSupportedException"/> is thrown.
  /// </remarks>
  /// <exception cref="System.NotSupportedException">Thrown when no conversion is found for <paramref name="target"/>.</exception>
  public object Convert(Base target)
  {
    var typeName = target.GetType().Name;

    // Direct conversion if a converter is found
    if (_baseConverter.TryConvert(target, out object? result))
    {
      return result;
    }

    // Fallback to display value if it exists.
    var displayValue = target.TryGetDisplayValue<Base>();
    if (displayValue != null)
    {
      return FallbackToDisplayValue(displayValue);
    }

    // Throw instead of null-return!
    throw new NotSupportedException($"No conversion found for {typeName}");
  }

  private object FallbackToDisplayValue(IReadOnlyList<Base> displayValue)
  {
    // Create a temp Displayable object that handles the displayValue.
    var tempDisplayableObject = new DisplayableObject(displayValue);

    var displayableObjectConverter = _toHost.GetConversionForType(typeof(DisplayableObject));

    // It is not guaranteed that a fallback converter has been registered in all connectors
    if (displayableObjectConverter == null)
    {
      throw new InvalidOperationException("No converter for fallback displayable objects was found.");
    }

    // Run the conversion, which will (or could?) return an `IEnumerable`. We don't care at this point, connector will.
    return displayableObjectConverter.Convert(tempDisplayableObject);
  }
}
