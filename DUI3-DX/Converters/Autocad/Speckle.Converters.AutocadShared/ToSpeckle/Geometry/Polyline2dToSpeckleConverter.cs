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
  private readonly IRawConversion<ADB.Polyline, SOG.Autocad.AutocadPolycurve> _polylineConverter;
  private readonly IRawConversion<ADB.Spline, SOG.Curve> _splineConverter;
  private readonly IRawConversion<AG.Vector3d, SOG.Vector> _vectorConverter;
  private readonly IRawConversion<ADB.Extents3d, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public Polyline2dToSpeckleConverter(
    IRawConversion<ADB.Arc, SOG.Arc> arcConverter,
    IRawConversion<ADB.Line, SOG.Line> lineConverter,
    IRawConversion<ADB.Polyline, SOG.Autocad.AutocadPolycurve> polylineConverter,
    IRawConversion<ADB.Spline, SOG.Curve> splineConverter,
    IRawConversion<AG.Vector3d, SOG.Vector> vectorConverter,
    IRawConversion<ADB.Extents3d, SOG.Box> boxConverter,
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
    // if this is a simple polyline2d, convert it is a lightweight polyline
    if (target.PolyType is ADB.Poly2dType.SimplePoly)
    {
      using (ADB.Polyline poly = new())
      {
        target.UpgradeOpen();
        poly.ConvertFrom(target, false);
        return _polylineConverter.RawConvert(poly);
      }
    }

    // get the poly type
    var polyType = SOG.Autocad.AutocadPolyType.Unknown;
    switch (target.PolyType)
    {
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
    }

    // get the spline curve segment
    // TODO: need to confirm that this retrieves the correct spline. We may need to construct the spline curve manually from vertex enumeration
    SOG.Curve spline = _splineConverter.RawConvert(target.Spline);
    // POC: retrieve spline display value here for database-resident polylines by exploding and retrieving segment endpoints
    if (target.Database is not null)
    {
      List<double> segmentValues = new();
      ADB.DBObjectCollection exploded = new();
      target.Explode(exploded);
      for (int i = 0; i < exploded.Count; i++)
      {
        if (exploded[i] is ADB.Curve curve)
        {
          segmentValues.AddRange(curve.StartPoint.ToArray());
          if (i == exploded.Count - 1)
          {
            segmentValues.AddRange(curve.EndPoint.ToArray());
          }
        }
      }

      SOG.Polyline displayValue = segmentValues.ConvertToSpecklePolyline(_contextStack.Current.SpeckleUnits);
      if (displayValue != null)
      {
        spline.displayValue = displayValue;
      }
    }

    SOG.Vector normal = _vectorConverter.RawConvert(target.Normal);
    SOG.Box bbox = _boxConverter.RawConvert(target.GeometricExtents);
    SOG.Autocad.AutocadPolycurve polycurve =
      new()
      {
        segments = new List<Objects.ICurve>() { spline },
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
}
