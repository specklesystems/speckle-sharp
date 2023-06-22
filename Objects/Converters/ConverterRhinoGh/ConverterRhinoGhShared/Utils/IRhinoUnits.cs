using Objects.Structural.Analysis;
using Rhino;
using Rhino.DocObjects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Converter.RhinoGh.Utils;

public interface IRhinoUnits
{
  string UnitToSpeckle(UnitSystem us);

  double ScaleToNative(double value, string units, string modelUnits);
}
