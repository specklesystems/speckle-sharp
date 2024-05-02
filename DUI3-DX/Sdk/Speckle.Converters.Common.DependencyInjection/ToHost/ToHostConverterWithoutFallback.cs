using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Common.DependencyInjection.ToHost;

// POC: CNX-9394 Find a better home for this outside `DependencyInjection` project
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
