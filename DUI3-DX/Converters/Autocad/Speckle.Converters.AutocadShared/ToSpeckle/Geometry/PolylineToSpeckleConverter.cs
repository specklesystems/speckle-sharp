using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.Geometry;

/// <summary>
/// The <see cref="ADB.Polyline"/> class converter. Converts to <see cref="SOG.Autocad.AutocadPolycurve"/>.
/// </summary>
/// <remarks>
/// <see cref="ADB.Polyline"/> is of type <see cref="SOG.Autocad.AutocadPolyType.Light"/> and will have only <see cref="SOG.Line"/>s and <see cref="SOG.Arc"/>s in <see cref="SOG.Polycurve.segments"/>.
/// </remarks>
[NameAndRankValue(nameof(ADB.Polyline), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PolylineToSpeckleConverter
  : IHostObjectToSpeckleConversion,
    IRawConversion<ADB.Polyline, SOG.Autocad.AutocadPolycurve>
{
  private readonly IRawConversion<AG.LineSegment3d, SOG.Line> _lineConverter;
  private readonly IRawConversion<AG.CircularArc3d, SOG.Arc> _arcConverter;
  private readonly IRawConversion<AG.Vector3d, SOG.Vector> _vectorConverter;
  private readonly IRawConversion<ADB.Extents3d, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public PolylineToSpeckleConverter(
    IRawConversion<AG.LineSegment3d, SOG.Line> lineConverter,
    IRawConversion<AG.CircularArc3d, SOG.Arc> arcConverter,
    IRawConversion<AG.Vector3d, SOG.Vector> vectorConverter,
    IRawConversion<ADB.Extents3d, SOG.Box> boxConverter,
    IConversionContextStack<Document, ADB.UnitsValue> contextStack
  )
  {
    _lineConverter = lineConverter;
    _arcConverter = arcConverter;
    _vectorConverter = vectorConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((ADB.Polyline)target);

  public SOG.Autocad.AutocadPolycurve RawConvert(ADB.Polyline target)
  {
    List<double> value = new(target.NumberOfVertices * 3);
    List<double> bulges = new(target.NumberOfVertices);
    List<Objects.ICurve> segments = new();
    for (int i = 0; i < target.NumberOfVertices; i++)
    {
      // get vertex value in the Object Coordinate System (OCS)
      AG.Point2d vertex = target.GetPoint2dAt(i);
      value.AddRange(vertex.ToArray());

      // get the bulge
      bulges.Add(target.GetBulgeAt(i));

      // get segment in the Global Coordinate System (GCS)
      ADB.SegmentType type = target.GetSegmentType(i);
      switch (type)
      {
        case ADB.SegmentType.Line:
          AG.LineSegment3d line = target.GetLineSegmentAt(i);
          segments.Add(_lineConverter.RawConvert(line));
          break;
        case ADB.SegmentType.Arc:
          AG.CircularArc3d arc = target.GetArcSegmentAt(i);
          segments.Add(_arcConverter.RawConvert(arc));
          break;
        default:
          // we are skipping segments of type Empty, Point, and Coincident
          break;
      }
    }

    SOG.Vector normal = _vectorConverter.RawConvert(target.Normal);
    SOG.Box bbox = _boxConverter.RawConvert(target.GeometricExtents);

    SOG.Autocad.AutocadPolycurve polycurve =
      new()
      {
        segments = segments,
        value = value,
        bulges = bulges,
        normal = normal,
        elevation = target.Elevation,
        polyType = SOG.Autocad.AutocadPolyType.Light,
        closed = target.Closed,
        length = target.Length,
        area = target.Area,
        bbox = bbox,
        units = _contextStack.Current.SpeckleUnits
      };

    return polycurve;
  }
}
