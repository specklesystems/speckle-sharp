using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared.ToSpeckle;

// POC: bin for now? This is also a parent child relationship and may need a pattern for this
// so we don't end up with some god FamilyInstanceTopLevelConverterToSpeckle converter
[NameAndRankValue(nameof(IRevitFamilyInstance), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public sealed class FamilyInstanceTopLevelConverterToSpeckle
  : BaseTopLevelConverterToSpeckle<IRevitFamilyInstance, Base>
{
  private readonly ITypedConverter<IRevitElement, SOBR.RevitElement> _elementConverter;
  private readonly ITypedConverter<IRevitFamilyInstance, SOBR.RevitBeam> _beamConversion;
  private readonly ITypedConverter<IRevitFamilyInstance, SOBR.RevitColumn> _columnConversion;
  private readonly ITypedConverter<IRevitFamilyInstance, SOBR.RevitBrace> _braceConversion;

  public FamilyInstanceTopLevelConverterToSpeckle(
    ITypedConverter<IRevitElement, SOBR.RevitElement> elementConverter,
    ITypedConverter<IRevitFamilyInstance, SOBR.RevitBeam> beamConversion,
    ITypedConverter<IRevitFamilyInstance, SOBR.RevitColumn> columnConversion,
    ITypedConverter<IRevitFamilyInstance, SOBR.RevitBrace> braceConversion
  )
  {
    _elementConverter = elementConverter;
    _beamConversion = beamConversion;
    _columnConversion = columnConversion;
    _braceConversion = braceConversion;
  }

  public override Base Convert(IRevitFamilyInstance target)
  {
    return target.StructuralType switch
    {
      RevitStructuralType.Beam => _beamConversion.Convert(target),
      RevitStructuralType.Column => _columnConversion.Convert(target),
      RevitStructuralType.Brace => _braceConversion.Convert(target),

      // POC: return generic element conversion or throw?
      //
      //throw new SpeckleConversionException(
      //  $"No conditional converters registered that could convert object of type {target.GetType()}"
      //);
      _ => _elementConverter.Convert(target)
    };
  }
}
