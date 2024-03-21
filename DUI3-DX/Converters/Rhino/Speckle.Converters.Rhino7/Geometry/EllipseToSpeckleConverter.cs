using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.Geometry;

[NameAndRankValue(nameof(RG.Ellipse), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class EllipseToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<RG.Ellipse, SOG.Ellipse>
{
  private readonly IRawConversion<RG.Plane, SOG.Plane> _planeConverter;
  private readonly IRawConversion<RG.Box, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public EllipseToSpeckleConverter(
    IRawConversion<RG.Plane, SOG.Plane> planeConverter,
    IRawConversion<RG.Box, SOG.Box> boxConverter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack
  )
  {
    _planeConverter = planeConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((RG.Ellipse)target);

  public SOG.Ellipse RawConvert(RG.Ellipse target)
  {
    var nurbsCurve = target.ToNurbsCurve();
    return new(
      _planeConverter.RawConvert(target.Plane),
      target.Radius1,
      target.Radius2,
      _contextStack.Current.SpeckleUnits
    )
    {
      domain = new SOP.Interval(0, 1),
      length = nurbsCurve.GetLength(),
      area = Math.PI * target.Radius1 * target.Radius2,
      bbox = _boxConverter.RawConvert(new RG.Box(nurbsCurve.GetBoundingBox(true)))
    };
  }
}
