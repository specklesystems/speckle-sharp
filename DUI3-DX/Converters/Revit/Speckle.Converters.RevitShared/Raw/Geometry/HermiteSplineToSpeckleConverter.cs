using Speckle.Converters.Common.Objects;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class HermiteSplineToSpeckleConverter : ITypedConverter<IRevitHermiteSpline, SOG.Curve>
{
  private readonly ITypedConverter<IRevitNurbSpline, SOG.Curve> _splineConverter;
  private readonly IRevitNurbSplineUtils _revitNurbSplineUtils;

  public HermiteSplineToSpeckleConverter(
    ITypedConverter<IRevitNurbSpline, SOG.Curve> splineConverter,
    IRevitNurbSplineUtils revitNurbSplineUtils
  )
  {
    _splineConverter = splineConverter;
    _revitNurbSplineUtils = revitNurbSplineUtils;
  }

  public SOG.Curve Convert(IRevitHermiteSpline target)
  {
    var nurbs = _revitNurbSplineUtils.Create(target);
    return _splineConverter.Convert(nurbs);
  }
}
