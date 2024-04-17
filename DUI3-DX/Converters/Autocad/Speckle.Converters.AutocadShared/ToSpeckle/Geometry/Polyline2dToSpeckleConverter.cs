using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Converters.Autocad.Extensions;
using System.Linq;
using Autodesk.AutoCAD.Geometry;

namespace Speckle.Converters.Autocad.Geometry;

/// <summary>
/// The <see cref="Polyline2d"/> class converter. Converts to <see cref="SOG.Autocad.AutocadPolycurve"/>.
/// <see cref="Polyline2d"/> of type <see cref="Poly2dType.SimplePoly"/> will be converted as <see cref="Polyline"/>.
/// </summary>
/// <remarks>
/// <see cref="Polyline2d"/> of type <see cref="Poly2dType.SimplePoly"/> will have only <see cref="SOG.Line"/>s and <see cref="SOG.Arc"/>s in <see cref="SOG.Polycurve.segments"/>.
/// <see cref="Polyline2d"/> of type <see cref="Poly2dType.FitCurvePoly"/>, <see cref="Poly2dType.CubicSplinePoly"/> and <see cref="Poly2dType.QuadSplinePoly"/> will have only one <see cref="SOG.Curve"/> in <see cref="SOG.Polycurve.segments"/>.
/// The IHostObjectToSpeckleConversion inheritance should only expect database-resident <see cref="Polyline2d"/> objects. IRawConversion inheritance can expect non database-resident objects, when generated from other converters.
/// </remarks>
[NameAndRankValue(nameof(ADB.Polyline2d), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class Polyline2dToSpeckleConverter : IHostObjectToSpeckleConversion
{
  private readonly IRawConversion<AG.Point3d, SOG.Point> _pointConverter;
  private readonly IRawConversion<ADB.Polyline, SOG.Autocad.AutocadPolycurve> _polylineConverter;
  private readonly IRawConversion<ADB.Spline, SOG.Curve> _splineConverter;
  private readonly IRawConversion<AG.Plane, SOG.Plane> _planeConverter;
  private readonly IRawConversion<Extents3d, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<Document, UnitsValue> _contextStack;

  public Polyline2dToSpeckleConverter(
    IRawConversion<AG.Point3d, SOG.Point> pointConverter,
    IRawConversion<ADB.Polyline, SOG.Autocad.AutocadPolycurve> polylineConverter,
    IRawConversion<ADB.Spline, SOG.Curve> splineConverter,
    IRawConversion<AG.Plane, SOG.Plane> planeConverter,
    IRawConversion<Extents3d, SOG.Box> boxConverter,
    IConversionContextStack<Document, UnitsValue> contextStack
  )
  {
    _pointConverter = pointConverter;
    _polylineConverter = polylineConverter;
    _splineConverter = splineConverter;
    _planeConverter = planeConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((ADB.Polyline2d)target);

  public SOG.Autocad.AutocadPolycurve RawConvert(ADB.Polyline2d target)
  {
    // if this is a simple polyline2d, convert it is a lightweight polyline
    if (target.PolyType is Poly2dType.SimplePoly)
    {
      using (ADB.Polyline poly = new ADB.Polyline())
      {
        poly.ConvertFrom(target, true);
        return _polylineConverter.RawConvert(poly);
      }
    }

    // get the poly type
    var polyType = SOG.Autocad.AutocadPolyType.Unknown;
    switch (target.PolyType)
    {
      case Poly2dType.FitCurvePoly:
        polyType = SOG.Autocad.AutocadPolyType.FitCurve2d;
        break;
      case Poly2dType.CubicSplinePoly:
        polyType = SOG.Autocad.AutocadPolyType.CubicSpline2d;
        break;
      case Poly2dType.QuadSplinePoly:
        polyType = SOG.Autocad.AutocadPolyType.QuadSpline2d;
        break;
    }

    // get all vertex data
    List<double> value = new();
    List<double> bulges = new();
    List<double> tangents = new();
    Point3dCollection vertices3D = new();
    List<Vertex2d> vertices = target
      .GetSubEntities<Vertex2d>(OpenMode.ForRead, _contextStack.Current.Document.TransactionManager.TopTransaction)
      .ToList();

    for (int i = 0; i < vertices.Count; i++)
    {
      Vertex2d vertex = vertices[i];

      // get vertex value in the Global Coordinate System (GCS).
      value.AddRange(vertex.Position.ToArray());

      // get the bulge and tangent, and 3d point for displayvalue
      bulges.Add(vertex.Bulge);
      tangents.Add(vertex.Tangent);

      // construct the spline curve segment
      switch (vertex.VertexType)
      {
        case Vertex2dType.CurveFitVertex:
          break;
        case Vertex2dType.SplineFitVertex:
          break;
        case Vertex2dType.SplineControlVertex:
          break;
      }
    }

    // POC: retrieve spline display value here for database-resident polylines by connecting all vertex points
    SOG.Polyline displayvalue;
    if (target.Database is not null)
    {
      target.Explode()
    }

    // get the spline curve segment
    // TODO: need to confirm that this retrieves the correct spline. We may need to construct the spline curve manually from vertex enumeration
    SOG.Curve spline = _splineConverter.RawConvert(target.Spline);

    SOG.Plane plane = _planeConverter.RawConvert(target.GetPlane());
    SOG.Box bbox = _boxConverter.RawConvert(target.GeometricExtents);

    SOG.Autocad.AutocadPolycurve polycurve =
      new()
      {
        segments = new List<Objects.ICurve>() { spline },
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
}
