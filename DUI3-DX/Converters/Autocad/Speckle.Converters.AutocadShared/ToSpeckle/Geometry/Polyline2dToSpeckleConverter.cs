using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Converters.Autocad.Extensions;

namespace Speckle.Converters.Autocad.Geometry;

/// <summary>
/// The <see cref="ADB.Polyline2d"/> class converter. Converts to <see cref="SOG.Autocad.AutocadPolycurve"/>.
/// </summary>
/// <remarks>
/// <see cref="ADB.Polyline2d"/> of type <see cref="ADB.Poly2dType.SimplePoly"/> will have only <see cref="SOG.Line"/>s and <see cref="SOG.Arc"/>s in <see cref="SOG.Polycurve.segments"/>.
/// <see cref="ADB.Polyline2d"/> of type <see cref="ADB.Poly2dType.FitCurvePoly"/> will have only <see cref="SOG.Arc"/>s in <see cref="SOG.Polycurve.segments"/>.
/// <see cref="ADB.Polyline2d"/> of type <see cref="ADB.Poly2dType.CubicSplinePoly"/> and <see cref="ADB.Poly2dType.QuadSplinePoly"/> will have only one <see cref="SOG.Curve"/> in <see cref="SOG.Polycurve.segments"/>.
/// The IToSpeckleTopLevelConverter inheritance should only expect database-resident <see cref="ADB.Polyline2d"/> objects. IRawConversion inheritance can expect non database-resident objects, when generated from other converters.
/// </remarks>
[NameAndRankValue(nameof(ADB.Polyline2d), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class Polyline2dToSpeckleConverter : IToSpeckleTopLevelConverter
{
  private readonly ITypedConverter<ADB.Arc, SOG.Arc> _arcConverter;
  private readonly ITypedConverter<ADB.Line, SOG.Line> _lineConverter;
  private readonly ITypedConverter<ADB.Polyline, SOG.Autocad.AutocadPolycurve> _polylineConverter;
  private readonly ITypedConverter<ADB.Spline, SOG.Curve> _splineConverter;
  private readonly ITypedConverter<AG.Vector3d, SOG.Vector> _vectorConverter;
  private readonly ITypedConverter<ADB.Extents3d, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public Polyline2dToSpeckleConverter(
    ITypedConverter<ADB.Arc, SOG.Arc> arcConverter,
    ITypedConverter<ADB.Line, SOG.Line> lineConverter,
    ITypedConverter<ADB.Polyline, SOG.Autocad.AutocadPolycurve> polylineConverter,
    ITypedConverter<ADB.Spline, SOG.Curve> splineConverter,
    ITypedConverter<AG.Vector3d, SOG.Vector> vectorConverter,
    ITypedConverter<ADB.Extents3d, SOG.Box> boxConverter,
    IConversionContextStack<Document, ADB.UnitsValue> contextStack
  )
  {
    _arcConverter = arcConverter;
    _lineConverter = lineConverter;
    _polylineConverter = polylineConverter;
    _splineConverter = splineConverter;
    _vectorConverter = vectorConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((ADB.Polyline2d)target);

  public SOG.Autocad.AutocadPolycurve RawConvert(ADB.Polyline2d target)
  {
    // get the poly type
    var polyType = SOG.Autocad.AutocadPolyType.Unknown;
    bool isSpline = false;
    switch (target.PolyType)
    {
      case ADB.Poly2dType.SimplePoly:
        polyType = SOG.Autocad.AutocadPolyType.Simple2d;
        break;
      case ADB.Poly2dType.FitCurvePoly:
        polyType = SOG.Autocad.AutocadPolyType.FitCurve2d;
        break;
      case ADB.Poly2dType.CubicSplinePoly:
        polyType = SOG.Autocad.AutocadPolyType.CubicSpline2d;
        isSpline = true;
        break;
      case ADB.Poly2dType.QuadSplinePoly:
        polyType = SOG.Autocad.AutocadPolyType.QuadSpline2d;
        isSpline = true;
        break;
    }

    // get all vertex data
    List<double> value = new();
    List<double> bulges = new();
    List<double?> tangents = new();
    List<ADB.Vertex2d> vertices = target
      .GetSubEntities<ADB.Vertex2d>(
        ADB.OpenMode.ForRead,
        _contextStack.Current.Document.TransactionManager.TopTransaction
      )
      .Where(e => e.VertexType != ADB.Vertex2dType.CurveFitVertex && e.VertexType != ADB.Vertex2dType.SplineFitVertex) // Do not collect fit vertex points, they are not used for creation
      .ToList();

    for (int i = 0; i < vertices.Count; i++)
    {
      ADB.Vertex2d vertex = vertices[i];

      // get vertex value in the Global Coordinate System (GCS).
      // NOTE: for some reason, the z value of the position for rotated polyline2ds doesn't seem to match the exploded segment endpoint values
      value.AddRange(vertex.Position.ToArray());

      // get the bulge and tangent
      bulges.Add(vertex.Bulge);
      tangents.Add(vertex.TangentUsed ? vertex.Tangent : null);
    }

    // explode the polyline
    // exploded segments will be the polyecurve segments for non-spline poly2ds, and the displayvalue for spline poly2ds
    // NOTE: exploded segments may not be in order or in the correct direction
    List<Objects.ICurve> segments = new();
    List<double> segmentValues = new();
    ADB.DBObjectCollection exploded = new();
    target.Explode(exploded);
    AG.Point3d previousPoint = new();

    for (int i = 0; i < exploded.Count; i++)
    {
      if (exploded[i] is ADB.Curve segment)
      {
        // for splines, just store point values for display value creation
        if (isSpline)
        {
          segmentValues.AddRange(segment.StartPoint.ToArray());
          if (i == exploded.Count - 1)
          {
            segmentValues.AddRange(segment.EndPoint.ToArray());
          }
        }
        // for non-splines, convert the curve and add to segments list
        else
        {
          // for the first segment, the only way we can correctly determine its orientation is to find the connection point to the next segment
          // this is because the z value of rotated polyline2d vertices is unreliable, so we can't use the first vertex
          if (i == 0 && exploded.Count > 1 && exploded[1] is ADB.Curve nextSegment)
          {
            previousPoint =
              segment.StartPoint.IsEqualTo(nextSegment.StartPoint) || segment.StartPoint.IsEqualTo(nextSegment.EndPoint)
                ? segment.EndPoint
                : segment.StartPoint;
          }

          switch (segment)
          {
            case ADB.Arc arc:
              if (ShouldReverseCurve(arc, previousPoint))
              {
                arc.ReverseCurve();
              }

              segments.Add(_arcConverter.Convert(arc));
              previousPoint = arc.EndPoint;
              break;
            case ADB.Line line:
              if (ShouldReverseCurve(line, previousPoint))
              {
                line.ReverseCurve();
              }

              segments.Add(_lineConverter.Convert(line));
              previousPoint = line.EndPoint;
              break;
          }
        }
      }
    }

    // for splines, convert the spline curve and display value and add to the segments list
    if (isSpline)
    {
      SOG.Curve spline = _splineConverter.Convert(target.Spline);
      SOG.Polyline displayValue = segmentValues.ConvertToSpecklePolyline(_contextStack.Current.SpeckleUnits);
      if (displayValue != null)
      {
        spline.displayValue = displayValue;
      }

      segments.Add(spline);
    }

    SOG.Vector normal = _vectorConverter.Convert(target.Normal);
    SOG.Box bbox = _boxConverter.Convert(target.GeometricExtents);
    SOG.Autocad.AutocadPolycurve polycurve =
      new()
      {
        segments = segments,
        value = value,
        bulges = bulges,
        tangents = tangents,
        normal = normal,
        elevation = target.Elevation,
        polyType = polyType,
        closed = target.Closed,
        length = target.Length,
        area = target.Area,
        bbox = bbox,
        units = _contextStack.Current.SpeckleUnits
      };

    return polycurve;
  }

  /// <summary>
  /// Determines if the input curve is reversed according to the input start point
  /// </summary>
  /// <param name="curve"></param>
  /// <param name="startPoint">Should match either the start or the end point of the curve.</param>
  /// <returns>True if the endpoint of the curve matches the startpoint, or false if it doesn't.</returns>
  private bool ShouldReverseCurve(ADB.Curve curve, AG.Point3d startPoint) => curve.EndPoint.IsEqualTo(startPoint);
}
