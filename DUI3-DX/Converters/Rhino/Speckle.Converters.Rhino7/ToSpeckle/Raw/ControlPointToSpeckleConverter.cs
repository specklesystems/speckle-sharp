using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class ControlPointToSpeckleConverter : ITypedConverter<IRhinoControlPoint, SOG.ControlPoint>
{
  private readonly IConversionContextStack<IRhinoDoc, RhinoUnitSystem> _contextStack;

  public ControlPointToSpeckleConverter(IConversionContextStack<IRhinoDoc, RhinoUnitSystem> contextStack)
  {
    _contextStack = contextStack;
  }

  /// <summary>
  /// Converts a ControlPoint object to a Speckle ControlPoint object.
  /// </summary>
  /// <param name="target">The ControlPoint object to convert.</param>
  /// <returns>The converted Speckle ControlPoint object.</returns>
  public SOG.ControlPoint Convert(IRhinoControlPoint target) =>
    new(target.Location.X, target.Location.Y, target.Location.Z, target.Weight, _contextStack.Current.SpeckleUnits);

  public Base Convert(object target) => Convert((IRhinoControlPoint)target);
}
