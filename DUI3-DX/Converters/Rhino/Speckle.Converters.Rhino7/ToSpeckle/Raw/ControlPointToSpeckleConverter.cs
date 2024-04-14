using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class ControlPointToSpeckleConverter : IRawConversion<RG.ControlPoint, SOG.ControlPoint>
{
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public ControlPointToSpeckleConverter(IConversionContextStack<RhinoDoc, UnitSystem> contextStack)
  {
    _contextStack = contextStack;
  }

  public SOG.ControlPoint RawConvert(RG.ControlPoint target) =>
    new(target.Location.X, target.Location.Y, target.Location.Z, target.Weight, _contextStack.Current.SpeckleUnits);

  public Base Convert(object target) => RawConvert((RG.ControlPoint)target);
}
