using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.Geometry;

[Common.NameAndRankValue(nameof(AG.Vector3d), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class VectorToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<AG.Vector3d, SOG.Vector>
{
  private readonly IConversionContextStack<Document, UnitsValue> _contextStack;

  public VectorToSpeckleConverter(IConversionContextStack<Document, UnitsValue> contextStack)
  {
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((AG.Vector3d)target);

  public SOG.Vector RawConvert(AG.Vector3d target) =>
    new(target.X, target.Y, target.Z, _contextStack.Current.SpeckleUnits);
}
