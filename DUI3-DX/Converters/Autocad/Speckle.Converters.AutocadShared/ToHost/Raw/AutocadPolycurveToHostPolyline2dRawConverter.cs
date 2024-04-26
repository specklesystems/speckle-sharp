using System.Collections.Generic;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Autocad2023.ToHost.Raw;

public class AutocadPolycurveToHostPolyline2dRawConverter : IRawConversion<SOG.Autocad.AutocadPolycurve, ADB.Polyline2d>
{
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public AutocadPolycurveToHostPolyline2dRawConverter(IConversionContextStack<Document, ADB.UnitsValue> contextStack)
  {
    _contextStack = contextStack;
  }

  public ADB.Polyline2d RawConvert(SOG.Autocad.AutocadPolycurve target)
  {
    ADB.Poly2dType polyType = ADB.Poly2dType.SimplePoly;
    switch (target.polyType)
    {
      case SOG.Autocad.AutocadPolyType.Simple2d:
        polyType = ADB.Poly2dType.SimplePoly;
        break;
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

    // AG.Point3dCollection pointsCollection = new();
    var points = new List<AG.Point3d>();

    for (int i = 2; i < target.value.Count; i += 3)
    {
      // pointsCollection.Add(new AG.Point3d(target.value[i - 2], target.value[i - 1], target.value[i]));
      points.Add(new AG.Point3d(target.value[i - 2], target.value[i - 1], target.value[i]));
    }

    // var bulges = target.bulges is null ? new AG.DoubleCollection() : new AG.DoubleCollection(target.bulges.ToArray());

    // ADB.Polyline2d pl = new(polyType, points, 0, target.closed, 0, 0, bulges);
    ADB.Polyline2d pl = new();

    ADB.Transaction tr = _contextStack.Current.Document.TransactionManager.TopTransaction;
    var btr = (ADB.BlockTableRecord)
      tr.GetObject(_contextStack.Current.Document.Database.CurrentSpaceId, ADB.OpenMode.ForWrite);
    btr.AppendEntity(pl);
    tr.AddNewlyCreatedDBObject(pl, true);
    for (int i = 0; i < points.Count; i++)
    {
      ADB.Vertex2d vertex = new(points[i], target.bulges[i], 0, 0, target.tangents[i]);
      pl.AppendVertex(vertex);
      tr.AddNewlyCreatedDBObject(vertex, true);
    }

    pl.Closed = target.closed;
    pl.PolyType = polyType;

    return pl;
  }
}
