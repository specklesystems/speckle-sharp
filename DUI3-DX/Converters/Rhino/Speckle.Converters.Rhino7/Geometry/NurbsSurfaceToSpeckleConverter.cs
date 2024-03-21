using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.Geometry;

[NameAndRankValue(nameof(RG.NurbsSurface), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class NurbsSurfaceToSpeckleConverter
  : IHostObjectToSpeckleConversion,
    IRawConversion<RG.NurbsSurface, SOG.Surface>
{
  private readonly IRawConversion<RG.Box, SOG.Box> _boxConverter;
  private readonly IRawConversion<RG.Interval, SOP.Interval> _intervalConverter;
  private readonly IRawConversion<RG.ControlPoint, SOG.ControlPoint> _controlPointConverter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public NurbsSurfaceToSpeckleConverter(
    IRawConversion<RG.Box, SOG.Box> boxConverter,
    IRawConversion<RG.Interval, SOP.Interval> intervalConverter,
    IRawConversion<RG.ControlPoint, SOG.ControlPoint> controlPointConverter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack
  )
  {
    _boxConverter = boxConverter;
    _intervalConverter = intervalConverter;
    _controlPointConverter = controlPointConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((RG.NurbsSurface)target);

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

  public List<List<SOG.ControlPoint>> ControlPointsToSpeckle(RG.Collections.NurbsSurfacePointList controlPoints)
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
