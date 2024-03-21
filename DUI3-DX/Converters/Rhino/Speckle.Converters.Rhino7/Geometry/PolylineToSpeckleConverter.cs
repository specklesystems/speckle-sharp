using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.Geometry;

[NameAndRankValue(nameof(RG.Polyline), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PolylineToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<RG.Polyline, SOG.Polyline>
{
  private readonly IRawConversion<RG.Point3d, SOG.Point> _pointConverter;
  private readonly IRawConversion<RG.Box, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public PolylineToSpeckleConverter(
    IRawConversion<RG.Point3d, SOG.Point> pointConverter,
    IRawConversion<RG.Box, SOG.Box> boxConverter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack
  )
  {
    _pointConverter = pointConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((RG.Polyline)target);

  public SOG.Polyline RawConvert(RG.Polyline target)
  {
    // POC: Original polyline conversion had a domain as input, as well as the side-effect of returning a `Line` if the polyline had 2 points only.

    var box = _boxConverter.RawConvert(new RG.Box(target.BoundingBox));
    var points = target.Select(pt => _pointConverter.RawConvert(pt)).ToList();

    if (target.IsClosed)
    {
      points.RemoveAt(points.Count - 1);
    }

    return new SOG.Polyline(
      points.SelectMany(pt => new[] { pt.x, pt.y, pt.z }).ToList(),
      _contextStack.Current.SpeckleUnits
    )
    {
      bbox = box,
      length = target.Length
    };
  }
}
