using Speckle.Converters.Common.Objects;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Revit2023.ToSpeckle;

public class HermiteSplitToSpeckleConverter : ITypedConverter<IRevitHermiteSpline, SOG.Curve>
{
  private readonly ITypedConverter<IRevitNurbSpline, SOG.Curve> _splineConverter;
  private readonly IRevitNurbSplineUtils _revitNurbSplineUtils;

  public HermiteSplitToSpeckleConverter(
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
