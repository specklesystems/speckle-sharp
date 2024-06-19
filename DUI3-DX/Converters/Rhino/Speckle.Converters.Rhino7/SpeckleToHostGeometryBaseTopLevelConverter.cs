using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7;

public abstract class SpeckleToHostGeometryBaseTopLevelConverter<TIn, TOut> : IToHostTopLevelConverter
  where TIn : Base
  where TOut : IRhinoGeometryBase
{
  protected IConversionContextStack<IRhinoDoc, RhinoUnitSystem> ContextStack { get; private set; }
  private readonly ITypedConverter<TIn, TOut> _geometryBaseConverter;
  private readonly IRhinoTransformFactory _transformFactory;

  protected SpeckleToHostGeometryBaseTopLevelConverter(
    IConversionContextStack<IRhinoDoc, RhinoUnitSystem> contextStack,
    ITypedConverter<TIn, TOut> geometryBaseConverter,
    IRhinoTransformFactory transformFactory
  )
  {
    ContextStack = contextStack;
    _geometryBaseConverter = geometryBaseConverter;
    _transformFactory = transformFactory;
  }

  public object Convert(Base target)
  {
    var castedBase = (TIn)target;
    var result = _geometryBaseConverter.Convert(castedBase);

    /*
     * POC: CNX-9270 Looking at a simpler, more performant way of doing unit scaling on `ToNative`
     * by fully relying on the transform capabilities of the HostApp, and only transforming top-level stuff.
     * This may not hold when adding more complex conversions, but it works for now!
     */
    if (castedBase["units"] is string units)
    {
      var scaleFactor = Units.GetConversionFactor(units, ContextStack.Current.SpeckleUnits);
      var scale = _transformFactory.Scale(_transformFactory.Origin, scaleFactor);
      result.Transform(scale);
    }

    return result;
  }
}
