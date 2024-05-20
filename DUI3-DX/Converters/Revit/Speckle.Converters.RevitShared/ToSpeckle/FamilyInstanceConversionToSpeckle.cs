using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.RevitShared.ToSpeckle;

// POC: bin for now? This is also a parent child relationship and may need a pattern for this
// so we don't end up with some god FamilyInstanceConversionToSpeckle converter
[NameAndRankValue(nameof(DB.FamilyInstance), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public sealed class FamilyInstanceConversionToSpeckle : BaseConversionToSpeckle<DB.FamilyInstance, Base>
{
  private readonly ITypedConverter<DB.Element, SOBR.RevitElement> _elementConverter;
  private readonly ITypedConverter<DB.FamilyInstance, SOBR.RevitBeam> _beamConversion;
  private readonly ITypedConverter<DB.FamilyInstance, SOBR.RevitColumn> _columnConversion;
  private readonly ITypedConverter<DB.FamilyInstance, SOBR.RevitBrace> _braceConversion;

  public FamilyInstanceConversionToSpeckle(
    ITypedConverter<DB.Element, SOBR.RevitElement> elementConverter,
    ITypedConverter<DB.FamilyInstance, SOBR.RevitBeam> beamConversion,
    ITypedConverter<DB.FamilyInstance, SOBR.RevitColumn> columnConversion,
    ITypedConverter<DB.FamilyInstance, SOBR.RevitBrace> braceConversion
  )
  {
    _elementConverter = elementConverter;
    _beamConversion = beamConversion;
    _columnConversion = columnConversion;
    _braceConversion = braceConversion;
  }

  public override Base Convert(DB.FamilyInstance target)
  {
    return target.StructuralType switch
    {
      DB.Structure.StructuralType.Beam => _beamConversion.Convert(target),
      DB.Structure.StructuralType.Column => _columnConversion.Convert(target),
      DB.Structure.StructuralType.Brace => _braceConversion.Convert(target),

      // POC: return generic element conversion or throw?
      //
      //throw new SpeckleConversionException(
      //  $"No conditional converters registered that could convert object of type {target.GetType()}"
      //);
      _ => _elementConverter.Convert(target)
    };
  }
}
