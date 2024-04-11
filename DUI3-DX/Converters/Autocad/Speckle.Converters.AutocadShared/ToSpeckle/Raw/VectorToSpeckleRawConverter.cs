using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToSpeckle.Raw;

public class VectorToSpeckleRawConverter : IRawConversion<AG.Vector3d, SOG.Vector>
{
  private readonly IConversionContextStack<Document, UnitsValue> _contextStack;

  public VectorToSpeckleRawConverter(IConversionContextStack<Document, UnitsValue> contextStack)
  {
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((AG.Vector3d)target);

  public SOG.Vector RawConvert(AG.Vector3d target) =>
    new(target.X, target.Y, target.Z, _contextStack.Current.SpeckleUnits);
}
