using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Converters.Autocad.Extensions;

namespace Speckle.Converters.Autocad.Geometry;

/// <summary>
/// The <see cref="ADB.Polyline3d"/> class converter. Converts to <see cref="SOG.Autocad.AutocadPolycurve"/>.
/// </summary>
/// <remarks>
/// <see cref="ADB.Polyline3d"/> of type <see cref="ADB.Poly2dType.SimplePoly"/> will have only one <see cref="SOG.Polyline"/> in <see cref="SOG.Polycurve.segments"/>.
/// <see cref="ADB.Polyline3d"/> of type <see cref="ADB.Poly2dType.CubicSplinePoly"/> and <see cref="ADB.Poly2dType.QuadSplinePoly"/> will have only one <see cref="SOG.Curve"/> in <see cref="SOG.Polycurve.segments"/>.
/// The IHostObjectToSpeckleConversion inheritance should only expect database-resident Polyline2d objects. IRawConversion inheritance can expect non database-resident objects, when generated from other converters.
/// </remarks>
[NameAndRankValue(nameof(ADB.Polyline3d), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class Polyline3dToSpeckleConverter : IHostObjectToSpeckleConversion
{
  private readonly IRawConversion<AG.Point3d, SOG.Point> _pointConverter;
  private readonly IRawConversion<ADB.Spline, SOG.Curve> _splineConverter;
  private readonly IRawConversion<ADB.Extents3d, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public Polyline3dToSpeckleConverter(
    IRawConversion<AG.Point3d, SOG.Point> pointConverter,
    IRawConversion<ADB.Spline, SOG.Curve> splineConverter,
    IRawConversion<ADB.Extents3d, SOG.Box> boxConverter,
    IConversionContextStack<Document, ADB.UnitsValue> contextStack
  )
  {
    _pointConverter = pointConverter;
    _splineConverter = splineConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((ADB.Polyline3d)target);

  public SOG.Autocad.AutocadPolycurve RawConvert(ADB.Polyline3d target)
  {
    // get the poly type
    var polyType = SOG.Autocad.AutocadPolyType.Unknown;
    switch (target.PolyType)
    {
      case ADB.Poly3dType.SimplePoly:
        polyType = SOG.Autocad.AutocadPolyType.Simple3d;
        break;
      case ADB.Poly3dType.CubicSplinePoly:
        polyType = SOG.Autocad.AutocadPolyType.CubicSpline3d;
        break;
      case ADB.Poly3dType.QuadSplinePoly:
        polyType = SOG.Autocad.AutocadPolyType.QuadSpline3d;
        break;
    }

    // get all vertex data except control vertices
    List<double> value = new();
    List<ADB.PolylineVertex3d> vertices = target
      .GetSubEntities<ADB.PolylineVertex3d>(
        ADB.OpenMode.ForRead,
        _contextStack.Current.Document.TransactionManager.TopTransaction
      )
      .Where(e => e.VertexType != ADB.Vertex3dType.FitVertex) // Do not collect fit vertex points, they are not used for creation
      .ToList();
    for (int i = 0; i < vertices.Count; i++)
    {
      // vertex value is in the Global Coordinate System (GCS).
      value.AddRange(vertices[i].Position.ToArray());
    }

    List<Objects.ICurve> segments = new();
    // for spline polyline3ds, get the spline curve segment
    // and explode the curve to get the spline displayvalue
    if (target.PolyType is not ADB.Poly3dType.SimplePoly)
    {
      // get the spline segment
      SOG.Curve spline = _splineConverter.RawConvert(target.Spline);

      // get the spline displayvalue by exploding the polyline
      List<double> segmentValues = new();
      ADB.DBObjectCollection exploded = new();
      target.Explode(exploded);
      for (int i = 0; i < exploded.Count; i++)
      {
        if (exploded[i] is ADB.Curve segment)
        {
          segmentValues.AddRange(segment.StartPoint.ToArray());
          if (i == exploded.Count - 1)
          {
            segmentValues.AddRange(segment.EndPoint.ToArray());
          }
        }
      }

      SOG.Polyline displayValue = segmentValues.ConvertToSpecklePolyline(_contextStack.Current.SpeckleUnits);
      if (displayValue != null)
      {
        spline.displayValue = displayValue;
      }

      segments.Add(spline);
    }
    // for simple polyline3ds just get the polyline segment from the value
    else
    {
      SOG.Polyline polyline = value.ConvertToSpecklePolyline(_contextStack.Current.SpeckleUnits);
      if (target.Closed)
      {
        polyline.closed = true;
      }

      segments.Add(polyline);
    }

    SOG.Box bbox = _boxConverter.RawConvert(target.GeometricExtents);

    SOG.Autocad.AutocadPolycurve polycurve =
      new()
      {
        segments = segments,
        value = value,
        polyType = polyType,
        closed = target.Closed,
        length = target.Length,
        bbox = bbox,
        units = _contextStack.Current.SpeckleUnits
      };

    return polycurve;
  }
}
