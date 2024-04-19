using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.Geometry;

/// <summary>
/// The <see cref="Polyline"/> class converter. Converts to <see cref="SOG.Autocad.AutocadPolycurve"/>.
/// </summary>
/// <remarks>
/// <see cref="Polyline"/> is of type <see cref="SOG.Autocad.AutocadPolyType.Light"/> and will have only <see cref="SOG.Line"/>s and <see cref="SOG.Arc"/>s in <see cref="SOG.Polycurve.segments"/>.
/// </remarks>
[NameAndRankValue(nameof(ADB.Polyline), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PolylineToSpeckleConverter
  : IHostObjectToSpeckleConversion,
    IRawConversion<ADB.Polyline, SOG.Autocad.AutocadPolycurve>
{
  private readonly IRawConversion<AG.Point3d, SOG.Point> _pointConverter;
  private readonly IRawConversion<AG.LineSegment3d, SOG.Line> _lineConverter;
  private readonly IRawConversion<AG.CircularArc3d, SOG.Arc> _arcConverter;
  private readonly IRawConversion<AG.Plane, SOG.Plane> _planeConverter;
  private readonly IRawConversion<Extents3d, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<Document, UnitsValue> _contextStack;

  public PolylineToSpeckleConverter(
    IRawConversion<AG.Point3d, SOG.Point> pointConverter,
    IRawConversion<AG.LineSegment3d, SOG.Line> lineConverter,
    IRawConversion<AG.CircularArc3d, SOG.Arc> arcConverter,
    IRawConversion<AG.Plane, SOG.Plane> planeConverter,
    IRawConversion<Extents3d, SOG.Box> boxConverter,
    IConversionContextStack<Document, UnitsValue> contextStack
  )
  {
    _pointConverter = pointConverter;
    _lineConverter = lineConverter;
    _arcConverter = arcConverter;
    _planeConverter = planeConverter;
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
      SegmentType type = target.GetSegmentType(i);
      switch (type)
      {
        case SegmentType.Line:
          segments.Add(_lineConverter.RawConvert(target.GetLineSegmentAt(i)));
          break;
        case SegmentType.Arc:
          segments.Add(_arcConverter.RawConvert(target.GetArcSegmentAt(i)));
          break;
        // POC: commented out claire's exception here because it breaks the conversion seems unnecessarily.. TBD
        // default:
        //   throw new InvalidOperationException("Polyline had an invalid segment of type Empty, Point, or Coincident.");
      }
    }

    SOG.Plane plane = _planeConverter.RawConvert(target.GetPlane());
    SOG.Box bbox = _boxConverter.RawConvert(target.GeometricExtents);

    SOG.Autocad.AutocadPolycurve polycurve =
      new()
      {
        segments = segments,
        value = value,
        bulges = bulges,
        plane = plane,
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
