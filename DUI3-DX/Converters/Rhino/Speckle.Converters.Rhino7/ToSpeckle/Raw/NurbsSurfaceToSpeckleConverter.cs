using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class NurbsSurfaceToSpeckleConverter : ITypedConverter<IRhinoNurbsSurface, SOG.Surface>
{
  private readonly ITypedConverter<IRhinoBox, SOG.Box> _boxConverter;
  private readonly ITypedConverter<IRhinoInterval, SOP.Interval> _intervalConverter;
  private readonly ITypedConverter<IRhinoControlPoint, SOG.ControlPoint> _controlPointConverter;
  private readonly IConversionContextStack<IRhinoDoc, RhinoUnitSystem> _contextStack;
  private readonly IRhinoBoxFactory _rhinoBoxFactory;

  public NurbsSurfaceToSpeckleConverter(
    ITypedConverter<IRhinoBox, SOG.Box> boxConverter,
    ITypedConverter<IRhinoInterval, SOP.Interval> intervalConverter,
    ITypedConverter<IRhinoControlPoint, SOG.ControlPoint> controlPointConverter,
    IConversionContextStack<IRhinoDoc, RhinoUnitSystem> contextStack,
    IRhinoBoxFactory rhinoBoxFactory
  )
  {
    _boxConverter = boxConverter;
    _intervalConverter = intervalConverter;
    _controlPointConverter = controlPointConverter;
    _contextStack = contextStack;
    _rhinoBoxFactory = rhinoBoxFactory;
  }

  /// <summary>
  /// Converts a NurbsSurface object to a Surface object.
  /// </summary>
  /// <param name="target">The NurbsSurface object to convert.</param>
  /// <returns>A Surface object representing the converted NurbsSurface.</returns>
  public SOG.Surface Convert(IRhinoNurbsSurface target)
  {
    var result = new SOG.Surface
    {
      degreeU = target.OrderU - 1,
      degreeV = target.OrderV - 1,
      rational = target.IsRational,
      closedU = target.IsClosed(0),
      closedV = target.IsClosed(1),
      domainU = _intervalConverter.Convert(target.Domain(0)),
      domainV = _intervalConverter.Convert(target.Domain(1)),
      knotsU = target.KnotsU.ToList(),
      knotsV = target.KnotsV.ToList(),
      units = _contextStack.Current.SpeckleUnits,
      bbox = _boxConverter.Convert(_rhinoBoxFactory.CreateBox(target.GetBoundingBox(true)))
    };

    result.SetControlPoints(ControlPointsToSpeckle(target.Points));

    return result;
  }

  private List<List<SOG.ControlPoint>> ControlPointsToSpeckle(IRhinoNurbsSurfacePointList controlPoints)
  {
    var points = new List<List<SOG.ControlPoint>>();
    for (var i = 0; i < controlPoints.CountU; i++)
    {
      var row = new List<SOG.ControlPoint>();
      for (var j = 0; j < controlPoints.CountV; j++)
      {
        var pt = controlPoints.GetControlPoint(i, j);
        row.Add(_controlPointConverter.Convert(pt));
      }

      points.Add(row);
    }

    return points;
  }
}
