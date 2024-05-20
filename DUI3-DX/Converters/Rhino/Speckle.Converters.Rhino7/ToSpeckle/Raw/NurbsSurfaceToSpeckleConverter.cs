using Rhino;
using Rhino.Geometry.Collections;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class NurbsSurfaceToSpeckleConverter : ITypedConverter<RG.NurbsSurface, SOG.Surface>
{
  private readonly ITypedConverter<RG.Box, SOG.Box> _boxConverter;
  private readonly ITypedConverter<RG.Interval, SOP.Interval> _intervalConverter;
  private readonly ITypedConverter<RG.ControlPoint, SOG.ControlPoint> _controlPointConverter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public NurbsSurfaceToSpeckleConverter(
    ITypedConverter<RG.Box, SOG.Box> boxConverter,
    ITypedConverter<RG.Interval, SOP.Interval> intervalConverter,
    ITypedConverter<RG.ControlPoint, SOG.ControlPoint> controlPointConverter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack
  )
  {
    _boxConverter = boxConverter;
    _intervalConverter = intervalConverter;
    _controlPointConverter = controlPointConverter;
    _contextStack = contextStack;
  }

  /// <summary>
  /// Converts a NurbsSurface object to a Surface object.
  /// </summary>
  /// <param name="target">The NurbsSurface object to convert.</param>
  /// <returns>A Surface object representing the converted NurbsSurface.</returns>
  public SOG.Surface RawConvert(RG.NurbsSurface target)
  {
    var result = new SOG.Surface
    {
      degreeU = target.OrderU - 1,
      degreeV = target.OrderV - 1,
      rational = target.IsRational,
      closedU = target.IsClosed(0),
      closedV = target.IsClosed(1),
      domainU = _intervalConverter.RawConvert(target.Domain(0)),
      domainV = _intervalConverter.RawConvert(target.Domain(1)),
      knotsU = target.KnotsU.ToList(),
      knotsV = target.KnotsV.ToList(),
      units = _contextStack.Current.SpeckleUnits,
      bbox = _boxConverter.RawConvert(new RG.Box(target.GetBoundingBox(true)))
    };

    result.SetControlPoints(ControlPointsToSpeckle(target.Points));

    return result;
  }

  private List<List<SOG.ControlPoint>> ControlPointsToSpeckle(NurbsSurfacePointList controlPoints)
  {
    var points = new List<List<SOG.ControlPoint>>();
    for (var i = 0; i < controlPoints.CountU; i++)
    {
      var row = new List<SOG.ControlPoint>();
      for (var j = 0; j < controlPoints.CountV; j++)
      {
        var pt = controlPoints.GetControlPoint(i, j);
        row.Add(_controlPointConverter.RawConvert(pt));
      }

      points.Add(row);
    }

    return points;
  }
}
