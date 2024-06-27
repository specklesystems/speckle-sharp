using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

[NameAndRankValue(nameof(DisplayableObject), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class FallbackToHostTopLevelConverter
  : IToHostTopLevelConverter,
    ITypedConverter<DisplayableObject, List<RG.GeometryBase>>
{
  private readonly ITypedConverter<SOG.Line, RG.LineCurve> _lineConverter;
  private readonly ITypedConverter<SOG.Polyline, RG.PolylineCurve> _polylineConverter;
  private readonly ITypedConverter<SOG.Mesh, RG.Mesh> _meshConverter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public FallbackToHostTopLevelConverter(
    ITypedConverter<SOG.Line, RG.LineCurve> lineConverter,
    ITypedConverter<SOG.Polyline, RG.PolylineCurve> polylineConverter,
    ITypedConverter<SOG.Mesh, RG.Mesh> meshConverter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack
  )
  {
    _lineConverter = lineConverter;
    _polylineConverter = polylineConverter;
    _meshConverter = meshConverter;
    _contextStack = contextStack;
  }

  public object Convert(Base target) => Convert((DisplayableObject)target);

  public List<RG.GeometryBase> Convert(DisplayableObject target)
  {
    var result = new List<RG.GeometryBase>();
    foreach (var item in target.displayValue)
    {
      RG.GeometryBase x = item switch
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

  private RG.Transform GetUnitsTransform(Base speckleObject)
  {
    /*
     * POC: CNX-9270 Looking at a simpler, more performant way of doing unit scaling on `ToNative`
     * by fully relying on the transform capabilities of the HostApp, and only transforming top-level stuff.
     * This may not hold when adding more complex conversions, but it works for now!
     */
    if (speckleObject["units"] is string units)
    {
      var scaleFactor = Units.GetConversionFactor(units, _contextStack.Current.SpeckleUnits);
      var scale = RG.Transform.Scale(RG.Point3d.Origin, scaleFactor);
      return scale;
    }

    return RG.Transform.Identity;
  }
}
