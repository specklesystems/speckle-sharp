using Rhino;
using Rhino.Runtime;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7;

public abstract class SpeckleToHostGeometryBaseConversion<TIn, TOut> : ISpeckleObjectToHostConversion<CommonObject>
  where TIn : Base
  where TOut : RG.GeometryBase
{
  protected IConversionContextStack<RhinoDoc, UnitSystem> ContextStack { get; }
  private readonly IRawConversion<TIn, TOut> _geometryBaseConverter;

  protected SpeckleToHostGeometryBaseConversion(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    IRawConversion<TIn, TOut> geometryBaseConverter
  )
  {
    ContextStack = contextStack;
    _geometryBaseConverter = geometryBaseConverter;
  }

  public CommonObject Convert(Base target)
  {
    var castedBase = (TIn)target;
    var result = _geometryBaseConverter.RawConvert(castedBase);

    /*
     * POC: CNX-9270 Looking at a simpler, more performant way of doing unit scaling on `ToNative`
     * by fully relying on the transform capabilities of the HostApp, and only transforming top-level stuff.
     * This may not hold when adding more complex conversions, but it works for now!
     */
    if (castedBase["units"] is string units)
    {
      var scaleFactor = Units.GetConversionFactor(units, ContextStack.Current.SpeckleUnits);
      var scale = RG.Transform.Scale(RG.Point3d.Origin, scaleFactor);
      result.Transform(scale);
    }

    return result;
  }
}
