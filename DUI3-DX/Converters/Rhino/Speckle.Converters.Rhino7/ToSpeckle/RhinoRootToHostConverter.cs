using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.ToSpeckle;

public class RhinoRootToHostConverter : IRootToSpeckleConverter
{
  private readonly IFactory<string, IToSpeckleTopLevelConverter> _toSpeckle;

  public RhinoRootToHostConverter(IFactory<string, IToSpeckleTopLevelConverter> toSpeckle)
  {
    _toSpeckle = toSpeckle;
  }

  public Base Convert(object target)
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
