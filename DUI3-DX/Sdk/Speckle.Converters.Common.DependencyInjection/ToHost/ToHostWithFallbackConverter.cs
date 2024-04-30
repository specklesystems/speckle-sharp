using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;

namespace Speckle.Converters.Common.DependencyInjection.ToHost;

public sealed class ToHostConverterWithoutFallback : ISpeckleConverterToHost
{
  private readonly IFactory<string, ISpeckleObjectToHostConversion> _toHost;

  public ToHostConverterWithoutFallback(IFactory<string, ISpeckleObjectToHostConversion> toHost)
  {
    _toHost = toHost;
  }

  public object Convert(Base target)
  {
    if (TryConvert(target, out object? result))
    {
      return result!;
    }
    throw new NotSupportedException($"No conversion found for {target.GetType()}");
  }

  internal bool TryConvert(Base target, out object? result)
  {
    var typeName = target.GetType().Name;

    // Direct conversion if a converter is found
    var objectConverter = _toHost.ResolveInstance(typeName);
    if (objectConverter != null)
    {
      result = objectConverter.Convert(target);
      return true;
    }

    result = null;
    return false;
  }
}

//TODO: xml docs
public sealed class ToHostConverterWithFallback : ISpeckleConverterToHost
{
  private readonly IFactory<string, ISpeckleObjectToHostConversion> _toHost;
  private readonly ToHostConverterWithoutFallback _baseConverter;

  public ToHostConverterWithFallback(IFactory<string, ISpeckleObjectToHostConversion> toHost)
  {
    _toHost = toHost;
    _baseConverter = new ToHostConverterWithoutFallback(toHost);
  }

  public object Convert(Base target)
  {
    var typeName = target.GetType().Name;

    // Direct conversion if a converter is found
    if (_baseConverter.TryConvert(target, out object? result))
    {
      return result!;
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

    var displayableObjectConverter = _toHost.ResolveInstance(nameof(DisplayableObject));

    // It is not guaranteed that a fallback converter has been registered in all connectors
    if (displayableObjectConverter == null)
    {
      throw new InvalidOperationException("No converter for fallback displayable objects was found.");
    }

    // Run the conversion, which will (or could?) return an `IEnumerable`. We don't care at this point, connector will.
    return displayableObjectConverter.Convert(tempDisplayableObject);
  }
}
