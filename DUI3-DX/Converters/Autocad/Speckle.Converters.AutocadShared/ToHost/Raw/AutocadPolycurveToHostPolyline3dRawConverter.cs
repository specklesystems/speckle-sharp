using Speckle.Converters.Autocad.Extensions;
using System.Collections.Generic;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Converters.Common;

namespace Speckle.Converters.Autocad2023.ToHost.Raw;

public class AutocadPolycurveToHostPolyline3dRawConverter : IRawConversion<SOG.Autocad.AutocadPolycurve, ADB.Polyline3d>
{
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public AutocadPolycurveToHostPolyline3dRawConverter(IConversionContextStack<Document, ADB.UnitsValue> contextStack)
  {
    _contextStack = contextStack;
  }

  public ADB.Polyline3d RawConvert(SOG.Autocad.AutocadPolycurve target)
  {
    // get vertices
    double f = Units.GetConversionFactor(target.units, _contextStack.Current.SpeckleUnits);
    List<AG.Point3d> points = target.value.ConvertToPoint3d(f);

    // create the polyline3d using the empty constructor
    ADB.Polyline3d polyline = new() { Closed = target.closed };

    // add polyline3d to document
    ADB.Transaction tr = _contextStack.Current.Document.TransactionManager.TopTransaction;
    var btr = (ADB.BlockTableRecord)
      tr.GetObject(_contextStack.Current.Document.Database.CurrentSpaceId, ADB.OpenMode.ForWrite);
    btr.AppendEntity(polyline);
    tr.AddNewlyCreatedDBObject(polyline, true);

    // append vertices
    for (int i = 0; i < points.Count; i++)
    {
      ADB.PolylineVertex3d vertex = new(points[i]);
      polyline.AppendVertex(vertex);
      tr.AddNewlyCreatedDBObject(vertex, true);
    }

    // convert to polytype
    ADB.Poly3dType polyType = ADB.Poly3dType.SimplePoly;
    switch (target.polyType)
    {
      case SOG.Autocad.AutocadPolyType.CubicSpline3d:
        polyType = ADB.Poly3dType.CubicSplinePoly;
        break;
      case SOG.Autocad.AutocadPolyType.QuadSpline3d:
        polyType = ADB.Poly3dType.QuadSplinePoly;
        break;
    }

    if (polyType is not ADB.Poly3dType.SimplePoly)
    {
      polyline.ConvertToPolyType(polyType);
    }

    return polyline;
  }
}
