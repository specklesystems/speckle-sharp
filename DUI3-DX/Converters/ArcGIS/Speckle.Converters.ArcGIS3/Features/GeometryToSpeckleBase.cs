using ArcGIS.Core.Data;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Features;

[NameAndRankValue(nameof(ArcGIS.Core.Geometry.Geometry), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class GeometryToSpeckleBaseList
  : IHostObjectToSpeckleConversion,
    IRawConversion<ArcGIS.Core.Geometry.Geometry, Base>
{
  private readonly IFactory<string, IHostObjectToSpeckleConversion> _toSpeckle;

  public GeometryToSpeckleBaseList(IFactory<string, IHostObjectToSpeckleConversion> toSpeckle)
  {
    _toSpeckle = toSpeckle;
  }

  public Base Convert(object target) => RawConvert((ArcGIS.Core.Geometry.Geometry)target);

  public Base RawConvert(ArcGIS.Core.Geometry.Geometry target)
  {
    List<Base> convertedList = new();

    Type type = target.GetType();
    try
    {
      var objectConverter = _toSpeckle.ResolveInstance(type.Name);

      if (objectConverter == null)
      {
        throw new NotSupportedException($"No conversion found for {type.Name}");
      }
      Base newGeometry = objectConverter.Convert(target);
      convertedList.Add(newGeometry);
      return convertedList[0];
    }
    catch (SpeckleConversionException e)
    {
      Console.WriteLine(e);
      throw; // Just rethrowing for now, Logs may be needed here.
    }
  }
}
