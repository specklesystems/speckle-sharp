using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Geometry;

[NameAndRankValue(nameof(DisplayableObject), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class FallbackToHostConverter : IToHostTopLevelConverter, ITypedConverter<DisplayableObject, ACG.Geometry>
{
  private readonly ITypedConverter<List<SOG.Mesh>, ACG.Multipatch> _meshListConverter;
  private readonly ITypedConverter<List<SOG.Polyline>, ACG.Polyline> _polylineListConverter;
  private readonly ITypedConverter<List<SOG.Point>, ACG.Multipoint> _pointListConverter;

  public FallbackToHostConverter(
    ITypedConverter<List<SOG.Mesh>, ACG.Multipatch> meshListConverter,
    ITypedConverter<List<SOG.Polyline>, ACG.Polyline> polylineListConverter,
    ITypedConverter<List<SOG.Point>, ACG.Multipoint> pointListConverter
  )
  {
    _meshListConverter = meshListConverter;
    _polylineListConverter = polylineListConverter;
    _pointListConverter = pointListConverter;
  }

  public object Convert(Base target) => Convert((DisplayableObject)target);

  public ACG.Geometry Convert(DisplayableObject target)
  {
    if (!target.displayValue.Any())
    {
      throw new NotSupportedException($"Zero fallback values specified");
    }

    var first = target.displayValue[0];

    return first switch
    {
      SOG.Polyline => _polylineListConverter.Convert(target.displayValue.Cast<SOG.Polyline>().ToList()),
      SOG.Mesh => _meshListConverter.Convert(target.displayValue.Cast<SOG.Mesh>().ToList()),
      SOG.Point => _pointListConverter.Convert(target.displayValue.Cast<SOG.Point>().ToList()),
      _ => throw new NotSupportedException($"Found unsupported fallback geometry: {first.GetType()}")
    };
  }
}
