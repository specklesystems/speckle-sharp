using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Objects.Other;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;

namespace Speckle.Converters.Common.Converters;

public class DefaultToHostConverter : ISpeckleConverterToHost
{
  private readonly IFactory<string, ISpeckleObjectToHostConversion> _toHost;

  public DefaultToHostConverter(IFactory<string, ISpeckleObjectToHostConversion> toHost)
  {
    _toHost = toHost;
  }

  public object Convert(Base target)
  {
    var typeName = target.GetType().Name;

    // Direct conversion if a converter is found
    var objectConverter = _toHost.ResolveInstance(typeName);
    if (objectConverter != null)
    {
      return objectConverter.Convert(target);
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

  private object FallbackToDisplayValue(IEnumerable<Base> displayValue)
  {
    // Create a temp Displayable object that handles the displayValue.
    var tempDisplayableObject = new DisplayableObject(displayValue.ToList());

    var displayableObjectConverter = _toHost.ResolveInstance(nameof(DisplayableObject));

    // It is not guaranteed that a fallback converter has been registered in all connectors
    if (displayableObjectConverter == null)
    {
      // POC: Do we want this to be a very specific exception so it can be handled separately?
      throw new NotSupportedException($"No converter for fallback displayable objects was found.");
    }

    // Run the conversion, which will (or could?) return an `IEnumerable`. We don't care at this point, connector will.
    return displayableObjectConverter.Convert(tempDisplayableObject);
  }
}
