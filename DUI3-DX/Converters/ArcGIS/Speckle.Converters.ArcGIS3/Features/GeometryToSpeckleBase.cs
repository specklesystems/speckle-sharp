using Objects.BuiltElements.TeklaStructures;
using Objects.GIS;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Features;

public class GeometryToSpeckleBaseList : IRawConversion<ArcGIS.Core.Geometry.Geometry, List<Base>>
{
  private readonly IFactory<string, IHostObjectToSpeckleConversion> _toSpeckle;

  public GeometryToSpeckleBaseList(IFactory<string, IHostObjectToSpeckleConversion> toSpeckle)
  {
    _toSpeckle = toSpeckle;
  }

  public List<Base> RawConvert(ArcGIS.Core.Geometry.Geometry target)
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
      return convertedList;
    }
    catch (SpeckleConversionException e)
    {
      Console.WriteLine(e);
      throw; // Just rethrowing for now, Logs may be needed here.
    }
  }
}
