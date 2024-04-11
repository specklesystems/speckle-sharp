using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToHost.Geometry;

[NameAndRankValue(nameof(SOG.Polyline), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PolylineToHostDBPolylineConverter : ISpeckleObjectToHostConversion, IRawConversion<SOG.Polyline, ADB.Polyline3d>
{
  private readonly IRawConversion<SOG.Point, AG.Point3d> _pointConverter;

  public PolylineToHostDBPolylineConverter(IRawConversion<SOG.Point, AG.Point3d> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public object Convert(Base target) => RawConvert((SOG.Polyline)target);

  public ADB.Polyline3d RawConvert(SOG.Polyline target)
  {
    Point3dCollection vertices = new();
    target.GetPoints().ForEach(o => vertices.Add(_pointConverter.RawConvert(o)));
    return new(Poly3dType.SimplePoly, vertices, target.closed);
  }
}
