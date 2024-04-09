using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.ToHost;

[NameAndRankValue(nameof(SOG.Point), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class SpecklePointToPointObjectConverter : ISpeckleObjectToHostConversion
{
  private readonly IRawConversion<SOG.Point, RG.Point3d> _pointConverter;

  public SpecklePointToPointObjectConverter(IRawConversion<SOG.Point, RG.Point3d> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public object Convert(Base target) => throw new NotImplementedException();
}
