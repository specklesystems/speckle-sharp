using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.InterfaceGenerator;

namespace Speckle.Converters.Common;

[GenerateAutoInterface]
public class RootConvertManager : IRootConvertManager
{
  private readonly IFactory<IToSpeckleTopLevelConverter> _toSpeckle;

  public RootConvertManager(IFactory<IToSpeckleTopLevelConverter> toSpeckle)
  {
    _toSpeckle = toSpeckle;
  }

  public Type GetTargetType(object target) => target.GetType();
  public bool IsSubClass(Type baseType, Type childType) => baseType.IsAssignableFrom(childType);

  public Base Convert(Type type, object obj)
  {
    try
    {
      var objectConverter = _toSpeckle.ResolveInstance(type.Name); //poc: would be nice to have supertypes resolve

      if (objectConverter == null)
      {
        throw new NotSupportedException($"No conversion found for {type.Name}");
      }
      var convertedObject = objectConverter.Convert(obj);

      return convertedObject;
    }
    catch (SpeckleConversionException e)
    {
      Console.WriteLine(e);
      throw; // Just rethrowing for now, Logs may be needed here.
    }
  }
}
