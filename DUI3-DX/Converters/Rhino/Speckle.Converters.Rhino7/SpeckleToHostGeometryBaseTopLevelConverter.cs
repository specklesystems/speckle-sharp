﻿using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7;

public abstract class SpeckleToHostGeometryBaseTopLevelConverter<TIn, TOut> : IToHostTopLevelConverter
  where TIn : Base
  where TOut : RG.GeometryBase
{
  protected IConversionContextStack<RhinoDoc, UnitSystem> ContextStack { get; private set; }
  private readonly ITypedConverter<TIn, TOut> _geometryBaseConverter;

  protected SpeckleToHostGeometryBaseTopLevelConverter(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    ITypedConverter<TIn, TOut> geometryBaseConverter
  )
  {
    ContextStack = contextStack;
    _geometryBaseConverter = geometryBaseConverter;
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
      var scale = RG.Transform.Scale(RG.Point3d.Origin, scaleFactor);
      result.Transform(scale);
    }

    return result;
  }
}
