using System.Collections.Generic;
using Speckle.Converters.Autocad.Extensions;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;

namespace Speckle.Converters.Autocad2023.ToHost.Raw;

public class AutocadPolycurveToHostPolyline2dRawConverter : IRawConversion<SOG.Autocad.AutocadPolycurve, ADB.Polyline2d>
{
  private readonly IRawConversion<SOG.Vector, AG.Vector3d> _vectorConverter;
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public AutocadPolycurveToHostPolyline2dRawConverter(
    IRawConversion<SOG.Vector, AG.Vector3d> vectorConverter,
    IConversionContextStack<Document, ADB.UnitsValue> contextStack
  )
  {
    _vectorConverter = vectorConverter;
    _contextStack = contextStack;
  }

  public ADB.Polyline2d RawConvert(SOG.Autocad.AutocadPolycurve target)
  {
    // check for normal
    if (target.normal is not SOG.Vector normal)
    {
      throw new System.ArgumentException($"Autocad polycurve of type {target.polyType} did not have a normal");
    }

    // check for elevation
    if (target.elevation is not double elevation)
    {
      throw new System.ArgumentException($"Autocad polycurve of type {target.polyType} did not have an elevation");
    }

    // get vertices
    double f = Units.GetConversionFactor(target.units, _contextStack.Current.SpeckleUnits);
    List<AG.Point3d> points = target.value.ConvertToPoint3d(f);

    // check for invalid bulges
    if (target.bulges is null || target.bulges.Count < points.Count)
    {
      throw new System.ArgumentException($"Autocad polycurve of type {target.polyType} had null or malformed bulges");
    }

    // check for invalid tangents
    if (target.tangents is null || target.tangents.Count < points.Count)
    {
      throw new System.ArgumentException($"Autocad polycurve of type {target.polyType} had null or malformed tangents");
    }

    // create the polyline2d using the empty constructor
    AG.Vector3d convertedNormal = _vectorConverter.RawConvert(normal);
    double convertedElevation = elevation * f;
    ADB.Polyline2d polyline =
      new()
      {
        Elevation = convertedElevation,
        Normal = convertedNormal,
        Closed = target.closed
      };

    // add polyline2d to document
    ADB.Transaction tr = _contextStack.Current.Document.TransactionManager.TopTransaction;
    var btr = (ADB.BlockTableRecord)
      tr.GetObject(_contextStack.Current.Document.Database.CurrentSpaceId, ADB.OpenMode.ForWrite);
    btr.AppendEntity(polyline);
    tr.AddNewlyCreatedDBObject(polyline, true);

    // append vertices
    for (int i = 0; i < points.Count; i++)
    {
      double tangent = target.tangents[i];
      ADB.Vertex2d vertex = new(points[i], target.bulges[i], 0, 0, tangent);
      if (tangent != 0)
      {
        vertex.TangentUsed = true;
      }

      polyline.AppendVertex(vertex);
      tr.AddNewlyCreatedDBObject(vertex, true);
    }

    // convert to polytype
    ADB.Poly2dType polyType = ADB.Poly2dType.SimplePoly;
    switch (target.polyType)
    {
      case SOG.Autocad.AutocadPolyType.FitCurve2d:
        polyType = ADB.Poly2dType.FitCurvePoly;
        break;
      case SOG.Autocad.AutocadPolyType.CubicSpline2d:
        polyType = ADB.Poly2dType.CubicSplinePoly;
        break;
      case SOG.Autocad.AutocadPolyType.QuadSpline2d:
        polyType = ADB.Poly2dType.QuadSplinePoly;
        break;
    }

    if (polyType is not ADB.Poly2dType.SimplePoly)
    {
      polyline.ConvertToPolyType(polyType);
    }

    return polyline;
  }
}
