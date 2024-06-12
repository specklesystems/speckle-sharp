using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.InterfaceGenerator;

namespace Speckle.Converters.Common;

[GenerateAutoInterface]
public class RootToSpeckleConverter : IRootToSpeckleConverter
{
  private readonly IFactory<IToSpeckleTopLevelConverter> _toSpeckle;
  private readonly IProxyMap _proxyMap;

  public RootToSpeckleConverter(IFactory<IToSpeckleTopLevelConverter> toSpeckle, IProxyMap proxyMap)
  {
    _toSpeckle = toSpeckle;
    _proxyMap = proxyMap;
  }

  public Base Convert(object target)
  {
    Type type = target.GetType();
    var wrapper = _proxyMap.WrapIfExists(type, target);
    if (wrapper is not null)
    {
      (type, target) = wrapper.Value;
    }
    try
    {
      var objectConverter = _toSpeckle.ResolveInstance(type.Name); //poc: would be nice to have supertypes resolve

      if (objectConverter == null)
      {
        throw new NotSupportedException($"No conversion found for {type.Name}");
      }
      var convertedObject = objectConverter.Convert(target);

      return convertedObject;
    }
    catch (SpeckleConversionException e)
    {
      Console.WriteLine(e);
      throw; // Just rethrowing for now, Logs may be needed here.
    }
  }
}
