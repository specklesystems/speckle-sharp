using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Extensions;
using Speckle.Core.Models;

namespace Speckle.Converters.RevitShared.ToSpeckle;

// POC: bin for now? This is also a parent child relationship and may need a pattern for this
// so we don't end up with some god FamilyInstanceConversionToSpeckle converter
[NameAndRankValue(nameof(DB.FamilyInstance), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public sealed class FamilyInstanceConversionToSpeckle : BaseConversionToSpeckle<DB.FamilyInstance, Base>
{
  private readonly IRawConversion<DB.Element, SOBR.RevitElement> _elementConverter;
  private readonly IRawConversion<DB.FamilyInstance, SOBR.RevitBeam> _beamConversion;

  public FamilyInstanceConversionToSpeckle(
    IRawConversion<DB.Element, SOBR.RevitElement> elementConverter,
    IRawConversion<DB.FamilyInstance, SOBR.RevitBeam> beamConversion
  )
  {
    _elementConverter = elementConverter;
    _beamConversion = beamConversion;
  }

  public override Base RawConvert(DB.FamilyInstance target)
  {
    if (
      target.Category.GetBuiltInCategory() == DB.BuiltInCategory.OST_StructuralFraming
      && target.StructuralType == DB.Structure.StructuralType.Beam
    )
    {
      return _beamConversion.RawConvert(target);
    }

    // POC: return generic element conversion or throw?
    //
    //throw new SpeckleConversionException(
    //  $"No conditional converters registered that could convert object of type {target.GetType()}"
    //);
    return _elementConverter.RawConvert(target);
  }
}
