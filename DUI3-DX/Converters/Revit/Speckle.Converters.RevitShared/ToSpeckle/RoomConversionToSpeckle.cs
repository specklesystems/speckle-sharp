using Objects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Core.Models;
using DBA = Autodesk.Revit.DB.Architecture;
using SOBE = Objects.BuiltElements;

namespace Speckle.Converters.RevitShared.ToSpeckle;

[NameAndRankValue(nameof(DBA.Room), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class RoomConversionToSpeckle : BaseConversionToSpeckle<DBA.Room, SOBE.Room>
{
  private readonly DisplayValueExtractor _displayValueExtractor;
  private readonly ParameterObjectAssigner _parameterObjectAssigner;
  private readonly IRawConversion<DB.Level, SOBR.RevitLevel> _levelConverter;
  private readonly ParameterValueExtractor _parameterValueExtractor;
  private readonly IRawConversion<DB.Location, Base> _locationConverter;
  private readonly IRawConversion<IList<DB.BoundarySegment>, SOG.Polycurve> _boundarySegmentConverter;

  public RoomConversionToSpeckle(
    DisplayValueExtractor displayValueExtractor,
    ParameterObjectAssigner parameterObjectAssigner,
    IRawConversion<DB.Level, SOBR.RevitLevel> levelConverter,
    ParameterValueExtractor parameterValueExtractor,
    IRawConversion<DB.Location, Base> locationConverter,
    IRawConversion<IList<DB.BoundarySegment>, SOG.Polycurve> boundarySegmentConverter
  )
  {
    _displayValueExtractor = displayValueExtractor;
    _parameterObjectAssigner = parameterObjectAssigner;
    _levelConverter = levelConverter;
    _parameterValueExtractor = parameterValueExtractor;
    _locationConverter = locationConverter;
    _boundarySegmentConverter = boundarySegmentConverter;
  }

  public override SOBE.Room RawConvert(DBA.Room target)
  {
    var number = target.Number;
    var name = _parameterValueExtractor.GetValueAsString(target, DB.BuiltInParameter.ROOM_NAME);
    var area = _parameterValueExtractor.GetValueAsDouble(target, DB.BuiltInParameter.ROOM_AREA);

    var displayValue = _displayValueExtractor.GetDisplayValue(target);
    var basePoint = (SOG.Point)_locationConverter.RawConvert(target.Location);
    var level = _levelConverter.RawConvert(target.Level);

    var profiles = target
      .GetBoundarySegments(new DB.SpatialElementBoundaryOptions())
      .Select(c => (ICurve)_boundarySegmentConverter.RawConvert(c))
      .ToList();

    var outline = profiles.First();
    var voids = profiles.Skip(1).ToList();

    var speckleRoom = new SOBE.Room(name ?? "-", number, level, basePoint)
    {
      displayValue = displayValue,
      area = area ?? 0,
      outline = outline,
      voids = voids
    };

    _parameterObjectAssigner.AssignParametersToBase(target, speckleRoom);

    // POC: Removed dynamic property `phaseCreated` as it seems the info is included in the parameters already

    return speckleRoom;
  }
}
