using Objects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Core.Models;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Revit2023.ToSpeckle;


[NameAndRankValue(nameof(IRevitRoom), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class RoomTopLevelConverterToSpeckle : BaseTopLevelConverterToSpeckle<IRevitRoom, SOBE.Room>
{
  private readonly IDisplayValueExtractor _displayValueExtractor;
  private readonly IParameterObjectAssigner _parameterObjectAssigner;
  private readonly ITypedConverter<IRevitLevel, SOBR.RevitLevel> _levelConverter;
  private readonly IParameterValueExtractor _parameterValueExtractor;
  private readonly ITypedConverter<IRevitLocation, Base> _locationConverter;
  private readonly ITypedConverter<IList<IRevitBoundarySegment>, SOG.Polycurve> _boundarySegmentConverter;

  public RoomTopLevelConverterToSpeckle(
    IDisplayValueExtractor displayValueExtractor,
    IParameterObjectAssigner parameterObjectAssigner,
    ITypedConverter<IRevitLevel, SOBR.RevitLevel> levelConverter,
    IParameterValueExtractor parameterValueExtractor,
    ITypedConverter<IRevitLocation, Base> locationConverter,
    ITypedConverter<IList<IRevitBoundarySegment>, SOG.Polycurve> boundarySegmentConverter
  )
  {
    _displayValueExtractor = displayValueExtractor;
    _parameterObjectAssigner = parameterObjectAssigner;
    _levelConverter = levelConverter;
    _parameterValueExtractor = parameterValueExtractor;
    _locationConverter = locationConverter;
    _boundarySegmentConverter = boundarySegmentConverter;
  }

  public override SOBE.Room Convert(IRevitRoom target)
  {
    var number = target.Number;
    var name = _parameterValueExtractor.GetValueAsString(target, RevitBuiltInParameter.ROOM_NAME);
    var area = _parameterValueExtractor.GetValueAsDouble(target, RevitBuiltInParameter.ROOM_AREA);

    var displayValue = _displayValueExtractor.GetDisplayValue(target);
    var basePoint = (SOG.Point)_locationConverter.Convert(target.Location);
    var level = _levelConverter.Convert(target.Level);

    var profiles = target
      .GetBoundarySegments()
      .Select(c => (ICurve)_boundarySegmentConverter.Convert(c.ToList()))
      .ToList();

    var outline = profiles.First();
    var voids = profiles.Skip(1).ToList();

    var speckleRoom = new SOBE.Room(name ?? "-", number, level, basePoint)
    {
      displayValue = displayValue,
      area = area,
      outline = outline,
      voids = voids
    };

    _parameterObjectAssigner.AssignParametersToBase(target, speckleRoom);

    // POC: Removed dynamic property `phaseCreated` as it seems the info is included in the parameters already

    return speckleRoom;
  }
}
