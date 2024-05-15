using Speckle.Converters.Autocad.Extensions;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;

namespace Speckle.Converters.Autocad2023.ToHost.Raw;

public class AutocadPolycurveToHostPolylineRawConverter : IRawConversion<SOG.Autocad.AutocadPolycurve, ADB.Polyline>
{
  private readonly IRawConversion<SOG.Vector, AG.Vector3d> _vectorConverter;
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public AutocadPolycurveToHostPolylineRawConverter(
    IRawConversion<SOG.Vector, AG.Vector3d> vectorConverter,
    IConversionContextStack<Document, ADB.UnitsValue> contextStack
  )
  {
    _vectorConverter = vectorConverter;
    _contextStack = contextStack;
  }

  public ADB.Polyline RawConvert(SOG.Autocad.AutocadPolycurve target)
  {
    if (target.normal is null || target.elevation is null)
    {
      throw new System.ArgumentException(
        "Autocad polycurve of type light did not have a valid normal and/or elevation"
      );
    }

    double f = Units.GetConversionFactor(target.units, _contextStack.Current.SpeckleUnits);
    List<AG.Point2d> points2d = target.value.ConvertToPoint2d(f);

    ADB.Polyline polyline =
      new()
      {
        Normal = _vectorConverter.RawConvert(target.normal),
        Elevation = (double)target.elevation * f,
        Closed = target.closed
      };

    for (int i = 0; i < points2d.Count; i++)
    {
      var bulge = target.bulges is null ? 0 : target.bulges[i];
      polyline.AddVertexAt(i, points2d[i], bulge, 0, 0);
    }

    return polyline;
  }
}
