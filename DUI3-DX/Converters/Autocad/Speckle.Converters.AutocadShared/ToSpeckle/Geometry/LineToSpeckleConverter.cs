using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToSpeckle.Geometry;

[NameAndRankValue(nameof(ADB.Line), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class LineToSpeckleConverter : IToSpeckleTopLevelConverter
{
  private readonly ITypedConverter<ADB.Line, SOG.Line> _lineConverter;

  public LineToSpeckleConverter(ITypedConverter<ADB.Line, SOG.Line> lineConverter)
  {
    _lineConverter = lineConverter;
  }

  public Base Convert(object target) => Convert((ADB.Line)target);

  public SOG.Line Convert(ADB.Line target) => _lineConverter.Convert(target);
}
