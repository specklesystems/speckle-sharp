using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

[NameAndRankValue(nameof(DisplayableObject), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class FallbackToHostTopLevelConverter
  : IToHostTopLevelConverter,
    ITypedConverter<DisplayableObject, List<IRhinoGeometryBase>>
{
  private readonly ITypedConverter<SOG.Line, IRhinoLineCurve> _lineConverter;
  private readonly ITypedConverter<SOG.Polyline, IRhinoPolylineCurve> _polylineConverter;
  private readonly ITypedConverter<SOG.Mesh, IRhinoMesh> _meshConverter;
  private readonly IConversionContextStack<IRhinoDoc, RhinoUnitSystem> _contextStack;
  private readonly IRhinoTransformFactory _rhinoTransformFactory;

  public FallbackToHostTopLevelConverter(
    ITypedConverter<SOG.Line, IRhinoLineCurve> lineConverter,
    ITypedConverter<SOG.Polyline, IRhinoPolylineCurve> polylineConverter,
    ITypedConverter<SOG.Mesh, IRhinoMesh> meshConverter,
    IConversionContextStack<IRhinoDoc, RhinoUnitSystem> contextStack, IRhinoTransformFactory rhinoTransformFactory)
  {
    _lineConverter = lineConverter;
    _polylineConverter = polylineConverter;
    _meshConverter = meshConverter;
    _contextStack = contextStack;
    _rhinoTransformFactory = rhinoTransformFactory;
  }

  public object Convert(Base target) => Convert((DisplayableObject)target);

  public List<IRhinoGeometryBase> Convert(DisplayableObject target)
  {
    var result = new List<IRhinoGeometryBase>();
    foreach (var item in target.displayValue)
    {
      IRhinoGeometryBase x = item switch
      {
        SOG.Line line => _lineConverter.Convert(line),
        SOG.Polyline polyline => _polylineConverter.Convert(polyline),
        SOG.Mesh mesh => _meshConverter.Convert(mesh),
        _ => throw new NotSupportedException($"Found unsupported fallback geometry: {item.GetType()}")
      };
      x.Transform(GetUnitsTransform(item));
      result.Add(x);
    }

    return result;
  }

  private IRhinoTransform GetUnitsTransform(Base speckleObject)
  {
    /*
     * POC: CNX-9270 Looking at a simpler, more performant way of doing unit scaling on `ToNative`
     * by fully relying on the transform capabilities of the HostApp, and only transforming top-level stuff.
     * This may not hold when adding more complex conversions, but it works for now!
     */
    if (speckleObject["units"] is string units)
    {
      var scaleFactor = Units.GetConversionFactor(units, _contextStack.Current.SpeckleUnits);
      var scale = _rhinoTransformFactory.Scale(_rhinoTransformFactory.Origin, scaleFactor);
      return scale;
    }

    return _rhinoTransformFactory.Identity;
  }
}
