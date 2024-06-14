using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.InterfaceGenerator;
using Speckle.Revit.Interfaces;

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
    Type revitType = target.GetType();
    var wrapper = _proxyMap.WrapIfExists(revitType, target);
    if (wrapper == null)
    {
      throw new NotSupportedException($"No wrapper found for Revit type: {revitType.Name}");
    }
    var (wrappedType, wrappedObject) = wrapper.Value;
    try
    {
      var objectConverter = _toSpeckle.ResolveInstance(wrappedType.Name); //poc: would be nice to have supertypes resolve

      if (objectConverter == null)
      {
        throw new NotSupportedException($"No conversion found for {wrappedType.Name}");
      }
      var convertedObject = objectConverter.Convert(wrappedObject);

      return convertedObject;
    }
    catch (SpeckleConversionException e)
    {
      Console.WriteLine(e);
      throw; // Just rethrowing for now, Logs may be needed here.
    }
  }
}
