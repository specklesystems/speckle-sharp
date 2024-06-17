using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Common;

public class LegacyRootToSpeckleConverter : IRootToSpeckleConverter
{
  private readonly IFactory<IToSpeckleTopLevelConverter> _toSpeckle;

  public LegacyRootToSpeckleConverter(IFactory<IToSpeckleTopLevelConverter> toSpeckle)
  {
    _toSpeckle = toSpeckle;
  }

  public Base Convert(object target)
  {
    Type type = target.GetType();
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
