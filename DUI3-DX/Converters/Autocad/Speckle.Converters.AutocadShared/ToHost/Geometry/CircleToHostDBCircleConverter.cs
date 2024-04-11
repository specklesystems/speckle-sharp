using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Objects.Geometry;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToHost.Geometry;

[NameAndRankValue(nameof(SOG.Circle), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class CircleToHostDBCircleConverter : ISpeckleObjectToHostConversion, IRawConversion<SOG.Circle, ADB.Circle>
{
  private readonly IRawConversion<SOG.Point, AG.Point3d> _pointConverter;
  private readonly IRawConversion<SOG.Vector, AG.Vector3d> _vectorConverter;
  private readonly IConversionContextStack<Document, UnitsValue> _contextStack;

  public CircleToHostDBCircleConverter(
    IRawConversion<SOG.Point, AG.Point3d> pointConverter,
    IRawConversion<Vector, Vector3d> vectorConverter,
    IConversionContextStack<Document, UnitsValue> contextStack
  )
  {
    _pointConverter = pointConverter;
    _vectorConverter = vectorConverter;
    _contextStack = contextStack;
  }

  public object Convert(Base target) => RawConvert((SOG.Circle)target);

  public ADB.Circle RawConvert(SOG.Circle target)
  {
    AG.Vector3d normal = _vectorConverter.RawConvert(target.plane.normal);
    AG.Point3d origin = _pointConverter.RawConvert(target.plane.origin);

    double f = Units.GetConversionFactor(target.units, _contextStack.Current.SpeckleUnits);
    double radius;
    if (target.radius is double targetRadius)
    {
      radius = f * targetRadius;
    }
    else
    {
      radius = f * Math.Sqrt(target.area / Math.PI);
    }

    return new(origin, normal, radius);
  }
}
