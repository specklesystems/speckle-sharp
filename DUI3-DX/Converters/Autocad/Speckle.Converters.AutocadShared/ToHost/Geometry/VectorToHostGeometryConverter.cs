using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToHost.Geometry;

public class VectorToHostGeometryConverter : IRawConversion<SOG.Vector, AG.Vector3d>
{
  private readonly IConversionContextStack<Document, UnitsValue> _contextStack;

  public VectorToHostGeometryConverter(IConversionContextStack<Document, UnitsValue> contextStack)
  {
    _contextStack = contextStack;
  }

  public object Convert(Base target) => RawConvert((SOG.Vector)target);

  public AG.Vector3d RawConvert(SOG.Vector target)
  {
    double f = Units.GetConversionFactor(target.units, _contextStack.Current.SpeckleUnits);
    return new(target.x * f, target.y * f, target.z * f);
  }
}
