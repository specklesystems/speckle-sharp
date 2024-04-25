using System.Collections.Generic;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Converters.Autocad.Extensions;
using System.Linq;

namespace Speckle.Converters.Autocad.Geometry;

/// <summary>
/// The <see cref="ADB.Polyline2d"/> class converter. Converts to <see cref="SOG.Autocad.AutocadPolycurve"/>.
/// <see cref="ADB.Polyline2d"/> of type <see cref="ADB.Poly2dType.SimplePoly"/> will be converted as <see cref="ADB.Polyline"/>.
/// </summary>
/// <remarks>
/// <see cref="ADB.Polyline2d"/> of type <see cref="ADB.Poly2dType.SimplePoly"/> will have only <see cref="SOG.Line"/>s and <see cref="SOG.Arc"/>s in <see cref="SOG.Polycurve.segments"/>.
/// <see cref="ADB.Polyline2d"/> of type <see cref="ADB.Poly2dType.FitCurvePoly"/>, <see cref="ADB.Poly2dType.CubicSplinePoly"/> and <see cref="ADB.Poly2dType.QuadSplinePoly"/> will have only one <see cref="SOG.Curve"/> in <see cref="SOG.Polycurve.segments"/>.
/// The IHostObjectToSpeckleConversion inheritance should only expect database-resident <see cref="ADB.Polyline2d"/> objects. IRawConversion inheritance can expect non database-resident objects, when generated from other converters.
/// </remarks>
[NameAndRankValue(nameof(ADB.Polyline2d), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class Polyline2dToSpeckleConverter : IHostObjectToSpeckleConversion
{
  private readonly IRawConversion<ADB.Arc, SOG.Arc> _arcConverter;
  private readonly IRawConversion<ADB.Line, SOG.Line> _lineConverter;
  private readonly IRawConversion<ADB.Spline, SOG.Curve> _splineConverter;
  private readonly IRawConversion<AG.Plane, SOG.Plane> _planeConverter;
  private readonly IRawConversion<ADB.Extents3d, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public Polyline2dToSpeckleConverter(
    IRawConversion<ADB.Arc, SOG.Arc> arcConverter,
    IRawConversion<ADB.Line, SOG.Line> lineConverter,
    IRawConversion<ADB.Spline, SOG.Curve> splineConverter,
    IRawConversion<AG.Plane, SOG.Plane> planeConverter,
    IRawConversion<ADB.Extents3d, SOG.Box> boxConverter,
    IConversionContextStack<Document, ADB.UnitsValue> contextStack
  )
  {
    _arcConverter = arcConverter;
    _lineConverter = lineConverter;
    _splineConverter = splineConverter;
    _planeConverter = planeConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((ADB.Polyline2d)target);

  public SOG.Autocad.AutocadPolycurve RawConvert(ADB.Polyline2d target)
  {
    // POC: Below check complicate things, and it is a destructive process. Why not we send it just as is?
    // if this is a simple polyline2d, convert it is a lightweight polyline
    // if (target.PolyType is Poly2dType.SimplePoly)
    // {
    //   using (ADB.Polyline poly = new ADB.Polyline())
    //   {
    //     poly.ConvertFrom(target, true);
    //     return _polylineConverter.RawConvert(poly);
    //   }
    // }

    // get the poly type
    var polyType = SOG.Autocad.AutocadPolyType.Unknown;
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
        break;
      case ADB.Poly2dType.QuadSplinePoly:
        polyType = SOG.Autocad.AutocadPolyType.QuadSpline2d;
        break;
    }

    // get all vertex data
    List<double> value = new();
    List<double> bulges = new();
    List<double> tangents = new();
    List<ADB.Vertex2d> vertices = target
      .GetSubEntities<ADB.Vertex2d>(
        ADB.OpenMode.ForRead,
        _contextStack.Current.Document.TransactionManager.TopTransaction
      )
      .ToList();

    for (int i = 0; i < vertices.Count; i++)
    {
      ADB.Vertex2d vertex = vertices[i];

      // get vertex value in the Global Coordinate System (GCS).
      value.AddRange(vertex.Position.ToArray());

      // get the bulge and tangent, and 3d point for displayvalue
      bulges.Add(vertex.Bulge);
      tangents.Add(vertex.Tangent);

      // POC: check this data is necessary on receive!
      // construct the spline curve segment
      // switch (vertex.VertexType)
      // {
      //   case Vertex2dType.CurveFitVertex:
      //     break;
      //   case Vertex2dType.SplineFitVertex:
      //     break;
      //   case Vertex2dType.SplineControlVertex:
      //     break;
      // }
    }

    List<Objects.ICurve> segments = new();

    // POC: retrieve spline display value here for database-resident polylines by connecting all vertex points
    if (target.Database is not null)
    {
      var exploded = new ADB.DBObjectCollection();
      target.Explode(exploded);
      AG.Point3d previousPoint = new();
      for (int i = 0; i < exploded.Count; i++)
      {
        var segment = exploded[i] as ADB.Curve;

        if (segment == null)
        {
          continue;
        }

        if (i == 0 && exploded.Count > 1)
        {
          // get the connection point to the next segment - this is necessary since imported polycurves might have segments in different directions
          var nextSegment = exploded[i + 1] as ADB.Curve;
          if (nextSegment == null)
          {
            continue;
          }
          AG.Point3d connectionPoint =
            nextSegment.StartPoint.IsEqualTo(segment.StartPoint) || nextSegment.StartPoint.IsEqualTo(segment.EndPoint)
              ? nextSegment.StartPoint
              : nextSegment.EndPoint;

          previousPoint = connectionPoint;
          segment = GetCorrectSegmentDirection(segment, connectionPoint, true, out AG.Point3d _);
        }
        else
        {
          segment = GetCorrectSegmentDirection(segment, previousPoint, false, out previousPoint);
        }

        switch (segment)
        {
          case ADB.Arc arc:
            segments.Add(_arcConverter.RawConvert(arc));
            break;
          case ADB.Line line:
            segments.Add(_lineConverter.RawConvert(line));
            break;
          case ADB.Spline spl:
            segments.Add(_splineConverter.RawConvert(spl));
            break;
        }
      }
    }

    // get the spline curve segment
    // TODO: need to confirm that this retrieves the correct spline. We may need to construct the spline curve manually from vertex enumeration
    // SOG.Curve spline = _splineConverter.RawConvert(target.Spline);
    // spline.displayValue = value.ConvertToSpecklePolyline(_contextStack);

    SOG.Plane plane = _planeConverter.RawConvert(target.GetPlane());
    SOG.Box bbox = _boxConverter.RawConvert(target.GeometricExtents);

    SOG.Autocad.AutocadPolycurve polycurve =
      new()
      {
        segments = segments,
        value = value,
        bulges = bulges,
        tangents = tangents,
        plane = plane,
        polyType = polyType,
        closed = target.Closed,
        length = target.Length,
        area = target.Area,
        bbox = bbox,
        units = _contextStack.Current.SpeckleUnits
      };

    return polycurve;
  }

  private ADB.Curve GetCorrectSegmentDirection(
    ADB.Curve segment,
    AG.Point3d connectionPoint,
    bool isFirstSegment,
    out AG.Point3d nextPoint
  ) // note sometimes curve3d may not have endpoints
  {
    nextPoint = segment.EndPoint;

    // POC: will be reconsidered when we started back and forth testing on AutocadPolycurve https://spockle.atlassian.net/browse/CNX-9327
    if (connectionPoint == null)
    {
      return segment;
    }

    bool reverseDirection;
    if (isFirstSegment)
    {
      reverseDirection = segment.StartPoint.IsEqualTo(connectionPoint);
      if (reverseDirection)
      {
        nextPoint = segment.StartPoint;
      }
    }
    else
    {
      reverseDirection = !segment.StartPoint.IsEqualTo(connectionPoint);
      if (reverseDirection)
      {
        nextPoint = segment.StartPoint;
      }
    }

    if (reverseDirection)
    {
      segment.ReverseCurve();
    }

    return segment;
  }
}
