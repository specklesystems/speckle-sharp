using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7;

public abstract class HostToSpeckleGeometryBaseConversion<TIn, TOut> : IHostObjectToSpeckleConversion
  where TIn : RG.GeometryBase
  where TOut : Base
{
  protected IConversionContextStack<RhinoDoc, UnitSystem> ContextStack { get; private set; }
  private readonly IRawConversion<TIn, TOut> _geometryBaseConverter;

  protected HostToSpeckleGeometryBaseConversion(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    IRawConversion<TIn, TOut> geometryBaseConverter
  )
  {
    ContextStack = contextStack;
    _geometryBaseConverter = geometryBaseConverter;
  }

  public Base Convert(object target)
  {
    var castedBase = (TIn)target;
    var result = _geometryBaseConverter.RawConvert(castedBase);
    return result;
  }
}
