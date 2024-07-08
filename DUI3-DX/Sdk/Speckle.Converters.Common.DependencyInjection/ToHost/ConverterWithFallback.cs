using System.Collections;
using Speckle.Converters.Common.Objects;
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
  private readonly ConverterWithoutFallback _baseConverter;

  public ConverterWithFallback(IConverterResolver<IToHostTopLevelConverter> toHost)
  {
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
  /// 1. Direct conversion using the <see cref="ConverterWithoutFallback"/>.
  /// 2. Fallback to display value using the <see cref="Speckle.Core.Models.Extensions.BaseExtensions.TryGetDisplayValue{T}"/> method, if a direct conversion is not possible.
  ///
  /// If the direct conversion is not available and there is no displayValue, a <see cref="System.NotSupportedException"/> is thrown.
  /// </remarks>
  /// <exception cref="System.NotSupportedException">Thrown when no conversion is found for <paramref name="target"/>.</exception>
  public object Convert(Base target)
  {
    Type type = target.GetType();

    // Direct conversion if a converter is found
    if (_baseConverter.TryGetConverter(type, out IToHostTopLevelConverter? result))
    {
      return result.Convert(target);
    }

    // Fallback to display value if it exists.
    var displayValue = target.TryGetDisplayValue<Base>();
    if (displayValue != null)
    {
      if (displayValue is IList && !displayValue.Any())
      {
        throw new NotSupportedException($"No display value found for {type}");
      }
      return FallbackToDisplayValue(displayValue);
    }

    throw new NotSupportedException($"No conversion found for {type}");
  }

  private object FallbackToDisplayValue(IReadOnlyList<Base> displayValue)
  {
    var tempDisplayableObject = new DisplayableObject(displayValue);

    return _baseConverter.Convert(tempDisplayableObject);
  }
}
