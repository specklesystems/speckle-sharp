using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;
using Speckle.Core.Models;
using Autodesk.AutoCAD.DatabaseServices;

namespace Speckle.Converters.Autocad.ToSpeckle.Raw;

public class PointToSpeckleRawConverter : IRawConversion<AG.Point3d, SOG.Point>
{
  private readonly IConversionContextStack<Document, UnitsValue> _contextStack;

  public PointToSpeckleRawConverter(IConversionContextStack<Document, UnitsValue> contextStack)
  {
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((AG.Point3d)target);

  public SOG.Point RawConvert(AG.Point3d target) =>
    new(target.X, target.Y, target.Z, _contextStack.Current.SpeckleUnits);
}
