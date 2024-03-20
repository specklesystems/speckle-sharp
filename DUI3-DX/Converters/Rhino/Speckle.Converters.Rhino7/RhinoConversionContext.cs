using System.Reactive;
using Rhino;
using Speckle.Converters.Common;

namespace Speckle.Converters.Rhino7;

public class RhinoConversionContext : ConversionContext<RhinoDoc, UnitSystem>
{
  public RhinoConversionContext(IHostToSpeckleUnitConverter<UnitSystem> unitConverter)
    : base(RhinoDoc.ActiveDoc, RhinoDoc.ActiveDoc.ModelUnitSystem, unitConverter) { }
}
