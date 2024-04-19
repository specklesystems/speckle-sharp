using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.RevitShared.ToSpeckle;

// POC: bin for now? This is also a parent child relationship and may need a pattern for this
// so we don't end up with some god FamilyInstanceConversionToSpeckle converter
[NameAndRankValue(nameof(DB.FamilyInstance), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public sealed class FamilyInstanceConversionToSpeckle : BaseConversionToSpeckle<DB.FamilyInstance, Base>
{
  private readonly IRawConversion<DB.Element, SOBR.RevitElement> _elementConverter;
  private readonly IEnumerable<IConditionalToSpeckleConverter<DB.FamilyInstance>> _conditionalConverters;

  public FamilyInstanceConversionToSpeckle(
    IRawConversion<DB.Element, SOBR.RevitElement> elementConverter,
    IEnumerable<IConditionalToSpeckleConverter<DB.FamilyInstance>> conditionalConverters
  )
  {
    _elementConverter = elementConverter;
    _conditionalConverters = conditionalConverters;
  }

  public override Base RawConvert(DB.FamilyInstance target)
  {
    foreach (var conditionalConverter in _conditionalConverters)
    {
      if (conditionalConverter.CanConvert(target))
      {
        return conditionalConverter.ConvertToSpeckle(target);
      }
    }

    // POC: return generic element conversion or throw?
    //
    //throw new SpeckleConversionException(
    //  $"No conditional converters registered that could convert object of type {target.GetType()}"
    //);
    return _elementConverter.RawConvert(target);
  }
}
