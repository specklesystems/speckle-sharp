using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class ControlPointToSpeckleConverter : ITypedConverter<RG.ControlPoint, SOG.ControlPoint>
{
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public ControlPointToSpeckleConverter(IConversionContextStack<RhinoDoc, UnitSystem> contextStack)
  {
    _contextStack = contextStack;
  }

  /// <summary>
  /// Converts a ControlPoint object to a Speckle ControlPoint object.
  /// </summary>
  /// <param name="target">The ControlPoint object to convert.</param>
  /// <returns>The converted Speckle ControlPoint object.</returns>
  public SOG.ControlPoint RawConvert(RG.ControlPoint target) =>
    new(target.Location.X, target.Location.Y, target.Location.Z, target.Weight, _contextStack.Current.SpeckleUnits);

  public Base Convert(object target) => RawConvert((RG.ControlPoint)target);
}
