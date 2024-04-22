using Rhino.Runtime;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.ToSpeckle;

public class RhinoConverterToSpeckle : ISpeckleConverterToSpeckle<CommonObject>
{
  private readonly IFactory<string, IHostObjectToSpeckleConversion<CommonObject>> _toSpeckle;

  public RhinoConverterToSpeckle(IFactory<string, IHostObjectToSpeckleConversion<CommonObject>> toSpeckle)
  {
    _toSpeckle = toSpeckle;
  }

  public Base Convert(CommonObject target)
  {
    var type = target.GetType();
    var objectConverter = _toSpeckle.ResolveInstance(type.Name);

    if (objectConverter == null)
    {
      throw new NotSupportedException($"No conversion found for {type.Name}");
    }

    return objectConverter.Convert(target);
  }
}
