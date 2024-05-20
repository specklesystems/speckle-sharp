using Speckle.Converters.Common;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Converters.RevitShared.ToSpeckle;

// POC: needs review feels, BIG, feels like it could be broken down..
// i.e. GetParams(), GetGeom()? feels like it's doing too much
[NameAndRankValue(nameof(DBA.TopographySurface), 0)]
public class TopographyTopLevelConverterToSpeckle : BaseTopLevelConverterToSpeckle<DBA.TopographySurface, SOBR.RevitTopography>
{
  private readonly DisplayValueExtractor _displayValueExtractor;
  private readonly ParameterObjectAssigner _parameterObjectAssigner;

  public TopographyTopLevelConverterToSpeckle(
    DisplayValueExtractor displayValueExtractor,
    ParameterObjectAssigner parameterObjectAssigner
  )
  {
    _displayValueExtractor = displayValueExtractor;
    _parameterObjectAssigner = parameterObjectAssigner;
  }

  public override SOBR.RevitTopography Convert(DBA.TopographySurface target)
  {
    var speckleTopo = new SOBR.RevitTopography
    {
      displayValue = _displayValueExtractor.GetDisplayValue(target),
      elementId = target.Id.ToString()
    };

    // POC: shouldn't we just do this in the RevitConverter ?
    _parameterObjectAssigner.AssignParametersToBase(target, speckleTopo);

    return speckleTopo;
  }
}
