using Rhino;
using Rhino.DocObjects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.ToSpeckle.TopLevel;

public class RhinoCurveObjectToSpeckleConverter : RhinoObjectToSpeckleConversion<CurveObject, Base>
{
  public RhinoCurveObjectToSpeckleConverter(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    IRawConversion<CurveObject, Base> converter
  )
    : base(contextStack, converter) { }
}
