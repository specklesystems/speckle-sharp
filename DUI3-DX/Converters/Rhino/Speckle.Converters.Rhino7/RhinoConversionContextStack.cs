using Rhino;
using Speckle.Converters.Common;

namespace Speckle.Converters.Rhino7;

public class RhinoConversionContextStack : ConversionContextStack<RhinoDoc, UnitSystem>
{
  public RhinoConversionContextStack(IHostToSpeckleUnitConverter<UnitSystem> unitConverter)
    : base(RhinoDoc.ActiveDoc, RhinoDoc.ActiveDoc.ModelUnitSystem, unitConverter) { }
}
