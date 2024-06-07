using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Revit.Api;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared.ToSpeckle;

[NameAndRankValue(nameof(DB.Element), 0)]
public class ElementTopLevelConverterToSpeckleLegacy : BaseTopLevelConverterToSpeckle<DB.Element, SOBR.RevitElement>
{
  private readonly ITypedConverter<IRevitElement, SOBR.RevitElement> _typedConverter;

  public ElementTopLevelConverterToSpeckleLegacy(ITypedConverter<IRevitElement, SOBR.RevitElement> typedConverter)
  {
    _typedConverter = typedConverter;
  }

  public override SOBR.RevitElement Convert(DB.Element target) => _typedConverter.Convert(new ElementProxy(target));
}
