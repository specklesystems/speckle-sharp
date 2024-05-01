using System.Collections.Generic;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Converters.Autocad.Extensions;
using System.Linq;
using Autodesk.AutoCAD.Geometry;

namespace Speckle.Converters.Autocad.Geometry;

/// <summary>
/// The <see cref="ADB.Polyline3d"/> class converter. Converts to <see cref="SOG.Autocad.AutocadPolycurve"/>.
/// </summary>
/// <remarks>
/// <see cref="ADB.Polyline3d"/> of type <see cref="ADB.Poly2dType.SimplePoly"/> will have only <see cref="SOG.Line"/>s in <see cref="SOG.Polycurve.segments"/>.
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

    List<Objects.ICurve> segments = new();
    for (int i = 0; i < vertices.Count; i++)
    {
      Point3d vertex = vertices[i].Position;

      // get vertex value in the Global Coordinate System (GCS).
      value.AddRange(vertex.ToArray());

      // construct the segment lines if this is a simple poly
      if (i < vertices.Count - 1)
      {
        if (polyType is SOG.Autocad.AutocadPolyType.Simple3d)
        {
          var nextVertex = vertices[i + 1].Position;
          SOG.Point start = _pointConverter.RawConvert(vertex);
          SOG.Point end = _pointConverter.RawConvert(nextVertex);

          SOG.Line segment = new(start, end, _contextStack.Current.SpeckleUnits);
          segments.Add(segment);
        }
      }
    }

    // get the spline curve segment if this is a spline polyline3d
    if (polyType is not SOG.Autocad.AutocadPolyType.Simple3d)
    {
      // add first 3 coordinate to last for display value polyline for spline
      if (target.Closed)
      {
        var firstPoint = value.Take(3).ToList();
        value.AddRange(firstPoint);
      }

      SOG.Curve spline = _splineConverter.RawConvert(target.Spline);
      spline.displayValue = value.ConvertToSpecklePolyline(_contextStack.Current.SpeckleUnits);

      segments.Add(spline);
    }
    else
    {
      if (target.Closed)
      {
        SOG.Point start = _pointConverter.RawConvert(vertices.First().Position);
        SOG.Point end = _pointConverter.RawConvert(vertices.Last().Position);
        segments.Add(new SOG.Line(start, end, _contextStack.Current.SpeckleUnits));
      }
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
