using Objects.Structural.Geometry;
using RH = Rhino.Geometry;

namespace Objects.Converter.RhinoGh;

public partial class ConverterRhinoGh
{
  private RH.Curve element1DToNative(Element1D element1d)
  {
    return CurveToNative(element1d.baseLine);
  }
}
